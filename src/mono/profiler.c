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

/* Time counting */



struct _MonoProfiler {
        int ncalls;
};

/* called at the end of the program */
static void
sample_shutdown (MonoProfiler *prof)
{
        printf("exiting");
        g_print ("total number of calls: %d\n", prof->ncalls);
}

static void
sample_method_enter (MonoProfiler *prof, MonoMethod *method)
{
        char *signature = (char*) mono_signature_get_desc (mono_method_signature (method), TRUE);
        char *name = (char*) mono_method_get_name (method);
        if(signature != NULL && name != NULL) {
            printf("called: %s %s", name, signature);
        }   
        g_free (signature);
        prof->ncalls++;
}

static void
sample_method_leave (MonoProfiler *prof, MonoMethod *method)
{
}




/* The main entry point of profiler (called back from mono, defined in profile.h)*/
void
mono_profiler_startup (const char *desc)
{
        MonoProfiler *prof;

        DEBUG("initialize");
        //prof = g_new0 (MonoProfiler, 1);
        prof = (MonoProfiler *) malloc(sizeof(MonoProfiler)); 

        mono_profiler_install (prof, sample_shutdown);
        mono_profiler_install_enter_leave (sample_method_enter, sample_method_leave);
        mono_profiler_set_events (MONO_PROFILE_ENTER_LEAVE);
}

