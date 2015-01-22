#include <stdio.h>
#include <stdlib.h>
#include <mono/metadata/profiler.h>
#include <glib.h>
/*
 *  * Bare bones profiler. Compile with:
 *  * gcc -shared -o mono-profiler-sample.so sample.c `pkg-config --cflags --libs mono`
 *  * Install the binary where the dynamic loader can find it.
 *  * Then run mono with:
 *  * mono --profile=sample your_application.exe
 *  */

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
        printf("called: %s %s", mono_method_get_name (method), signature);
        g_free(signature);
        prof->ncalls++;
}

static void
sample_method_leave (MonoProfiler *prof, MonoMethod *method)
{
}

/* the entry point */
void
mono_profiler_startup (const char *desc)
{
        MonoProfiler *prof;
        g_print("initialize");
        //prof = g_new0 (MonoProfiler, 1);
        prof = (MonoProfiler *) malloc(sizeof(MonoProfiler)); 

        mono_profiler_install (prof, sample_shutdown);
        mono_profiler_install_enter_leave (sample_method_enter, sample_method_leave);
        mono_profiler_set_events (MONO_PROFILE_ENTER_LEAVE);
}

