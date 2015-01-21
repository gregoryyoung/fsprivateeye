#include <stdio.h>
#include <stdlib.h>
#include <mono/metadata/profiler.h>
#include <glib.h>

/*
 * Profiling backend for privateeye in mono. Uses the mono profiler api and
 * writes to a named pipe that gets processed by the privateeye backend
 * in the host process.
 * TODO add more comments and stuff
 *  */

#define DEBUG_PROFILER 1
#if (DEBUG_PROFILER)
#define DEBUG(m) printf ("%s\n", m)
#else
#define DEBUG(m)
#endif

#define CHECK_PROFILER_ENABLED() do {\
    if (! profiler->profiler_enabled)\
        return;\
} while (0)



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
        0x53,                       /* push   %ebx */
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



/* MonoProfiler Opaque struct */
struct _MonoProfiler {
    int ncalls;
    gboolean profiler_enabled;
};

static void
enable_profiler (MonoProfiler *profiler) {
    profiler->profiler_enabled = TRUE;
}

static void
disable_profiler (MonoProfiler *profiler) {
    profiler->profiler_enabled = FALSE;
}

static char *
get_method_name(MonoMethod *method) {
    MonoClass *klass = mono_method_get_class (method);
    char *signature = (char*) mono_signature_get_desc (mono_method_signature (method), TRUE);
    if(signature == NULL) return NULL; 
    char *name = g_strdup_printf ("%s.%s:%s (%s)", mono_class_get_namespace (klass), mono_class_get_name (klass), mono_method_get_name (method), signature);
    g_free (signature);
    return name; 
}


/**********************************************************************
 *
 * Profiler callbacks
 *
 **********************************************************************/

static void
sample_shutdown (MonoProfiler *profiler)
{
    CHECK_PROFILER_ENABLED();
    DEBUG("exiting\n");
    printf("number of calls is %d\n", profiler->ncalls);
}

static void
sample_method_enter (MonoProfiler *profiler, MonoMethod *method)
{
    CHECK_PROFILER_ENABLED();
    DEBUG(get_method_name (method));
    profiler->ncalls++;
}

static void
sample_method_leave (MonoProfiler *profiler, MonoMethod *method)
{
    CHECK_PROFILER_ENABLED();
}

/* The main entry point of profiler (called back from mono, defined in profile.h)*/
void
mono_profiler_startup (const char *desc)
{
    MonoProfiler *prof;

    DEBUG("initialize");
    prof = (MonoProfiler *) malloc(sizeof(MonoProfiler)); 
    prof->profiler_enabled = TRUE;

    mono_profiler_install (prof, sample_shutdown);
    mono_profiler_install_enter_leave (sample_method_enter, sample_method_leave);
    mono_profiler_set_events (MONO_PROFILE_ENTER_LEAVE);
    enable_profiler (prof);
}

