#include <stdio.h>
#include <stdlib.h>
#include <glib.h>
#include <assert.h>
#include <string.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/time.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/debug-helpers.h>

/*
 * Profiling backend for privateeye in mono. Uses the mono profiler api and
 * writes to a named pipe that gets processed by the privateeye backend
 * in the host process.
 * TODO add more comments and stuff
 *  */

static char *START_OUTPUT_METHOD = "BA91E1230BF74A17AB35D3879E65D032";
static char *END_OUTPUT_METHOD = "D574BADA0B0547DA891E57551C978192";
static char *FORCE_FLUSH_PROFILER_METHOD = "DE259A95ABCA4803A7731665B38DB33A";
static char *ENTER_MATRYOSHKA_METHOD = "AC4A98BC81E94DADB71D1FABA30E0703";
static char *LEAVE_MATRYOSHKA_METHOD = "BB8F606F50BD474293A734159ABA1D23";
static int METHOD_LENGTH = 32;
static char *FIFO_PREXIX = "PRIVATE_EYE";

static char EVENT_ALLOCATION = 'A';
static char EVENT_LEAVE = 'L';
static char EVENT_ENTER = 'E';
static char DEFINITION_TYPE = 'T';
static char DEFINITION_METHOD = 'M';

#define DEBUG TRUE 

/* Time counting */
#define MONO_PROFILER_GET_CURRENT_TIME(t) {\
        struct timeval current_time;\
        gettimeofday (&current_time, NULL);\
        (t) = (((guint64)current_time.tv_sec) * 1000000) + current_time.tv_usec;\
} while (0)

static gboolean use_fast_timer = FALSE;

#if (defined(__i386__) || defined(__x86_64__)) && ! defined(HOST_WIN32)

#if defined(__i386__)
static const guchar cpuid_impl [] = {
        0x55,                       /* push   %ebp */
        0x89, 0xe5,                 /* mov    %esp,%ebp */
        0x53                       /* push   %ebx */
        0x8b, 0x45, 0x08,           /* mov    0x8(%ebp),%eax */
        0x0f, 0xa2,                 /* cpuid   */
        0x50,                       /* push   %eax */
        0x8b, 0x45, 0x10,           /* mov    0x10(%ebp),%eax */
        0x89, 0x18,                 /* mov    %ebx,(%eax) */
        0x8b, 0x45, 0x14,           /* mov    0x14(%ebp),%eax */
        0x89, 0x08,                 /* mov    %ecx,(%eax) */
        0x8b, 0x45, 0x18,           /* mov    0x18(%ebp),%eax */
        0x89, 0x10,                 /* mov    %edx,(%eax) */
        0x58,                       /* pop    %eax */
        0x8b, 0x55, 0x0c,           /* mov    0xc(%ebp),%edx */
        0x89, 0x02,                 /* mov    %eax,(%edx) */
        0x5b,                       /* pop    %ebx */
        0xc9,                       /* leave   */
        0xc3,                       /* ret     */
};

typedef void (*CpuidFunc) (int id, int* p_eax, int* p_ebx, int* p_ecx, int* p_edx);

static int 
cpuid (int id, int* p_eax, int* p_ebx, int* p_ecx, int* p_edx) {
    int have_cpuid = 0;
#ifndef _MSC_VER
        __asm__  __volatile__ (
            "pushfl\n"
            "popl %%eax\n"
            "movl %%eax, %%edx\n"
            "xorl $0x200000, %%eax\n"
            "pushl %%eax\n"
            "popfl\n"
            "pushfl\n"
            "popl %%eax\n"
            "xorl %%edx, %%eax\n"
            "andl $0x200000, %%eax\n"
            "movl %%eax, %0"
            : "=r" (have_cpuid)
            :
            : "%eax", "%edx"
      );
#else
      __asm {
            pushfd
            pop eax
            mov edx, eax
            xor eax, 0x200000
            push eax
            popfd
            pushfd
            pop eax
            xor eax, edx
            and eax, 0x200000
            mov have_cpuid, eax
     }
#endif
     if (have_cpuid) {
         CpuidFunc func = (CpuidFunc) cpuid_impl;
         func (id, p_eax, p_ebx, p_ecx, p_edx);
         /*
          *       * We use this approach because of issues with gcc and pic code, see:
          *       * http://gcc.gnu.org/cgi-bin/gnatsweb.pl?cmd=view%20audit-trail&database=gcc&pr=7329
          *       __asm__ __volatile__ ("cpuid"
          *       : "=a" (*p_eax), "=b" (*p_ebx), "=c" (*p_ecx), "=d" (*p_edx)
          *       : "a" (id));
          **/
          return 1;
      }
   return 0;
}

static void detect_fast_timer (void) {
    int p_eax, p_ebx, p_ecx, p_edx;
            
    if (cpuid (0x1, &p_eax, &p_ebx, &p_ecx, &p_edx)) {
        if (p_edx & 0x10) {
            use_fast_timer = TRUE;
        } else {
            use_fast_timer = FALSE;
        }
    } else {
        use_fast_timer = FALSE;
    }
}
#endif

#if defined(__x86_64__)
static void detect_fast_timer (void) {
    guint32 op = 0x1;
    guint32 eax,ebx,ecx,edx;
    __asm__ __volatile__ ("cpuid" : "=a"(eax), "=b"(ebx), "=c"(ecx), "=d"(edx) : "a"(op));
    if (edx & 0x10) {
        use_fast_timer = TRUE;
    } else {
        use_fast_timer = FALSE;
    }
}
#endif

static __inline__ guint64 rdtsc(void) {
        guint32 hi, lo;
        __asm__ __volatile__ ("rdtsc" : "=a"(lo), "=d"(hi));
        return ((guint64) lo) | (((guint64) hi) << 32);
}
#define MONO_PROFILER_GET_CURRENT_COUNTER(c) {\
    if (use_fast_timer) {\
        (c) = rdtsc ();\
    } else {\
        MONO_PROFILER_GET_CURRENT_TIME ((c));\
    }\
} while (0)
#else
static void detect_fast_timer (void) {
        use_fast_timer = FALSE;
}
#define MONO_PROFILER_GET_CURRENT_COUNTER(c) MONO_PROFILER_GET_CURRENT_TIME ((c))
#endif

#ifndef HOST_WIN32
#define LOCK_PROFILER() do { pthread_mutex_lock (&(profiler->mutex));} while (0)
#define UNLOCK_PROFILER() do { pthread_mutex_unlock (&(profiler->mutex));} while (0)

#define CHECK_PROFILER_ENABLED() do {\
    if (! profiler->enabled)\
        return;\
} while (0)

#define INITIALIZE_PROFILER_MUTEX() pthread_mutex_init (&(profiler->mutex), NULL)
#define DELETE_PROFILER_MUTEX() pthread_mutex_destroy (&(profiler->mutex))

static pthread_key_t pthread_profiler_key;
static pthread_once_t profiler_pthread_once = PTHREAD_ONCE_INIT;
static void
make_pthread_profiler_key (void) {
        (void) pthread_key_create (&pthread_profiler_key, NULL);
}
#define LOOKUP_PROFILER_THREAD_DATA() ((ProfilerPerThreadData*) pthread_getspecific (pthread_profiler_key))
#define SET_PROFILER_THREAD_DATA(x) (void) pthread_setspecific (pthread_profiler_key, (x))
#define ALLOCATE_PROFILER_THREAD_DATA() (void) pthread_once (&profiler_pthread_once, make_pthread_profiler_key)
#define FREE_PROFILER_THREAD_DATA() (void) pthread_key_delete (pthread_profiler_key)
#define CURRENT_THREAD_ID() (gsize) pthread_self ()
#else
//TODO handle windows specific? like anyone would actually use that!
#endif

#define GET_PROFILER_THREAD_DATA(data) do {\
        ProfilerPerThreadData *_result = LOOKUP_PROFILER_THREAD_DATA ();\
        if (!_result) {\
                    printf("thread data not found, creating\n");\
                    _result = g_new (ProfilerPerThreadData, 1);\
                    _result->thread_id = CURRENT_THREAD_ID ();\
                    _result->is_matryoshka = FALSE;\
                    SET_PROFILER_THREAD_DATA (_result);\
                }\
        (data) = _result;\
} while (0)

#define LEAVE_IF_MATRYOSHKA() if(thread_data->is_matryoshka > 0) return;

#define DEBUG_PRINTF(fmt, ...) \
    do { if (DEBUG) fprintf(stderr, "PROFILER: %s:%d:%s(): " fmt, __FILE__, \
            __LINE__, __func__, __VA_ARGS__); } while (0)

typedef struct _ProfilerPerThreadData {
    guint32 thread_id;
    gboolean is_matryoshka;
} ProfilerPerThreadData;

typedef struct _MethodIdMappingElement {
    char *name;
    guint32 id;
    MonoMethod *method;
} MethodIdMappingElement;

typedef struct _ClassIdMappingElement {
    char *name;
    guint32 id;
    MonoClass *klass;
} ClassIdMappingElement;

struct _MonoProfiler {
    pthread_mutex_t mutex;
    int ncalls;
    int allocations;
    gboolean enabled;
    GHashTable *cached_methods;
    GHashTable *cached_types;
    FILE *fd;
};

static char *
get_method_name(MonoMethod *method) {
    MonoClass *klass = mono_method_get_class (method);
    MonoMethodSignature *sig = mono_method_signature (method);
    char *signature = (char *) mono_signature_get_desc (sig, TRUE);
    void *returntype = mono_signature_get_return_type(sig);
    char *tname = mono_type_get_name(returntype);
    char *name = g_strdup_printf ("%s %s.%s:%s (%s)", 
                                 tname, 
                                 mono_class_get_namespace (klass), 
                                 mono_class_get_name (klass), 
                                 mono_method_get_name (method), 
                                 signature);
    g_free (signature);
    g_free (tname);
    return name; 
}


static char * get_fifo_name() {
    return "";    
}

static void 
open_fifo_for_output (MonoProfiler *profiler, char *pipe_name) {
    FILE *pipe;
    int fifo_fd = open(pipe_name, O_WRONLY);
    if (!fifo_fd) printf("error from os open\n");
    FILE *fp = fdopen(fifo_fd, "w");
    if(!fp) {
        printf ("Could not open pipe, exiting\n");
        exit (1);
    }
    profiler->fd = fp;
}

static void
open_file_for_output (MonoProfiler *profiler, char *name) {
    FILE *fd;

    fd = fopen (name, "w+");
    if(!fd) {
        perror ("error opening file, exiting");
        exit (1);
    }
    profiler->fd = (FILE *) fd; 
}

static void 
close_fifo (MonoProfiler *profiler) {
    if(!fclose (profiler->fd)) {
        perror ("couldn't close pipe");
        exit(1);
    }
}

static void
flush_buffer (MonoProfiler *profiler) {
    assert (profiler != NULL);
    assert (profiler->fd > 0);
    fflush (profiler->fd);
}

static void 
write_event (MonoProfiler *profiler, char event_type, guint32 thread_id, guint64 counter, void *identifier) {
    fprintf (profiler->fd, "%c,%llu,%lu,%p\n", event_type, counter ,thread_id, identifier);
    flush_buffer (profiler);
}

static void 
write_definition (MonoProfiler *profiler, char event_type, void *identifier, char *name) {
    fprintf (profiler->fd, "%c,%p,%s\n", event_type, identifier, name);
}

static void
method_id_mapping_element_destroy (gpointer element) {
    MethodIdMappingElement *e = (MethodIdMappingElement*) element;
    if (e->name)
        g_free (e->name);
    g_free (element);
}

static void
class_id_mapping_element_destroy (gpointer element) {
    ClassIdMappingElement *e = (ClassIdMappingElement*) element;
    if (e->name)
         g_free (e->name);
    g_free (element);
}

static char *
get_method_name_with_cache(MonoProfiler *profiler, MonoMethod *method) 
{
    LOCK_PROFILER();
    MethodIdMappingElement *cached = g_hash_table_lookup (profiler->cached_methods, (gconstpointer) method);
    if (cached != NULL) {
        UNLOCK_PROFILER();
        return cached->name;
    }
    MethodIdMappingElement *result = g_new (MethodIdMappingElement, 1);
    char *signature = get_method_name (method);
    result->name = signature;            
    result->method = method;
    g_hash_table_insert (profiler->cached_methods, method, result);
    DEBUG_PRINTF ("Created new METHOD mapping element \"%s\" (%p)\n", result->name, method);
    write_definition (profiler, DEFINITION_METHOD, method, result->name);
    UNLOCK_PROFILER();
    return result->name;
}


static char *
get_type_name_with_cache(MonoProfiler *profiler, MonoClass *klass) 
{
    LOCK_PROFILER();
    ClassIdMappingElement *cached = g_hash_table_lookup (profiler->cached_types, (gconstpointer) klass);
    if (cached != NULL) {
        UNLOCK_PROFILER();
        return cached->name;
    }
    ClassIdMappingElement *result = g_new (ClassIdMappingElement, 1);
    result->name = g_strdup_printf("%s.%s", mono_class_get_namespace(klass), mono_class_get_name(klass));            
    result->klass = klass;
    g_hash_table_insert (profiler->cached_types, klass, result);
    DEBUG_PRINTF ("Created new CLASS mapping element \"%s\" (%p)\n", result->name, klass);
    write_definition (profiler, DEFINITION_TYPE, klass, result->name);
    UNLOCK_PROFILER();
    return result->name;
}


static MonoProfiler *
init_mono_profiler () {
    MonoProfiler *ret;
    ret = (MonoProfiler *) malloc(sizeof(MonoProfiler)); 
    ret->enabled = FALSE;
    
    pthread_mutex_init (&(ret->mutex), NULL);
    return ret;
}

static void
enable_profiler (MonoProfiler *profiler) {
    open_fifo_for_output (profiler, "/tmp/test");
    profiler->enabled = TRUE;
}

static void 
disable_profiler (MonoProfiler *profiler) {
    if(profiler->fd)
        close_fifo (profiler);
    profiler->enabled = FALSE;
}

static void 
try_parse_method_as_command (MonoProfiler *profiler, MonoMethod *method, ProfilerPerThreadData *thread_data) {
    const char *name = mono_method_get_name (method);
    if(strncmp(name, START_OUTPUT_METHOD, METHOD_LENGTH) == 0) {
        DEBUG_PRINTF ("enabling profiler%d\n", thread_data->thread_id);
        enable_profiler (profiler); 
        return;
    } 

    if(strncmp(name, ENTER_MATRYOSHKA_METHOD, METHOD_LENGTH) == 0) {
        DEBUG_PRINTF("enabling matryoshka %d\n", thread_data->thread_id);
        thread_data->is_matryoshka++;
        return;
    } 
    
    if(strncmp(name, LEAVE_MATRYOSHKA_METHOD, METHOD_LENGTH) == 0) {
        printf ("disabling matryoshka\n");
        if (thread_data->is_matryoshka == 0) {
            printf ("critical sections don't match.\n");
            exit (3);
        }
        thread_data->is_matryoshka--;
        return;
    } 
}

/**********************************************************************
 *
 * Profiler callbacks
 *
 **********************************************************************/

static void
pe_shutdown (MonoProfiler *profiler) {
    LOCK_PROFILER();
    mono_profiler_set_events (0);
    disable_profiler (profiler);
    printf("number of calls is %d\n", profiler->ncalls);
    printf("number of allocations is %d\n", profiler->allocations);
    UNLOCK_PROFILER();
}

static void
pe_method_enter (MonoProfiler *profiler, MonoMethod *method) {
    ProfilerPerThreadData *thread_data;
    guint64 counter = 0;

    GET_PROFILER_THREAD_DATA (thread_data);
    try_parse_method_as_command(profiler, method, thread_data);
    LEAVE_IF_MATRYOSHKA();
    CHECK_PROFILER_ENABLED();
    MONO_PROFILER_GET_CURRENT_COUNTER (counter);
    char *name = get_method_name_with_cache(profiler, method);
    DEBUG_PRINTF ("enter %s\n", name);
    write_event(profiler, EVENT_ENTER, thread_data->thread_id, counter, method);
    profiler->ncalls++;
}

static void
pe_method_leave (MonoProfiler *profiler, MonoMethod *method) {
    ProfilerPerThreadData *thread_data;
    guint64 counter;

    GET_PROFILER_THREAD_DATA (thread_data);
    LEAVE_IF_MATRYOSHKA();
    CHECK_PROFILER_ENABLED();
    MONO_PROFILER_GET_CURRENT_COUNTER (counter);
    char *name = get_method_name_with_cache(profiler, method);
    //DEBUG_PRINTF ("leave %s\n", name);
    write_event(profiler, EVENT_LEAVE, thread_data->thread_id, counter, method);
}

static void
pe_object_allocated (MonoProfiler *profiler, MonoObject *obj, MonoClass *klass) {
    ProfilerPerThreadData *thread_data;
    guint64 counter;

    GET_PROFILER_THREAD_DATA (thread_data);
    LEAVE_IF_MATRYOSHKA();
    CHECK_PROFILER_ENABLED();
    MONO_PROFILER_GET_CURRENT_COUNTER (counter);
    
    guint size = mono_object_get_size (obj);
    char *type_name = get_type_name_with_cache (profiler, klass);
    write_event(profiler, EVENT_ALLOCATION, thread_data->thread_id, counter, klass);
    profiler->allocations++;
}

static void
pe_method_jit_result (MonoProfiler *profiler, MonoMethod *method, MonoJitInfo* jinfo, int result) {
    CHECK_PROFILER_ENABLED();
    if (result == MONO_PROFILE_OK) {
        char *name = get_method_name_with_cache (profiler, method);
        int code_size = mono_jit_info_get_code_size (jinfo);
        if(name == NULL) return;
    }
}

static void
pe_throw_callback (MonoProfiler *profiler, MonoObject *object) {
    CHECK_PROFILER_ENABLED();
}

static void 
pe_clause_callback (MonoProfiler *profiler, MonoMethod *method, int clause_type, int clause_num) {
    //fprintf(profiler->fd, "clause call back\n");
    CHECK_PROFILER_ENABLED();
}

static void
pe_exc_method_leave (MonoProfiler *profiler, MonoMethod *method) {
    //printf("method leave due to exception.\n");
    guint64 counter;
    CHECK_PROFILER_ENABLED();
    MONO_PROFILER_GET_CURRENT_COUNTER (counter);
    char *name = get_method_name_with_cache(profiler, method);
}

/* The main entry point of profiler (called back from mono, defined in profile.h)*/
void
mono_profiler_startup (const char *desc) {
    MonoProfiler *profiler;

    detect_fast_timer ();

    ALLOCATE_PROFILER_THREAD_DATA();
     
    profiler = (MonoProfiler *) malloc(sizeof(MonoProfiler)); 

    profiler->cached_methods = g_hash_table_new_full (g_direct_hash, NULL, NULL, method_id_mapping_element_destroy);
    profiler->cached_types = g_hash_table_new_full (g_direct_hash, NULL, NULL, class_id_mapping_element_destroy);
    profiler->enabled = FALSE;
    INITIALIZE_PROFILER_MUTEX();
    mono_profiler_install (profiler, pe_shutdown);
    mono_profiler_install_enter_leave (pe_method_enter, pe_method_leave);
    mono_profiler_install_exception (pe_throw_callback, pe_exc_method_leave, pe_clause_callback);
    mono_profiler_install_allocation (pe_object_allocated);
    mono_profiler_install_jit_end (pe_method_jit_result);

    mono_profiler_set_events (MONO_PROFILE_ENTER_LEAVE | 
                              MONO_PROFILE_JIT_COMPILATION | 
                              MONO_PROFILE_ALLOCATIONS | 
                              MONO_PROFILE_EXCEPTIONS);
}

