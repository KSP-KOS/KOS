using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace kOS.Safe.Utilities
{
    /// <summary>
    /// kOS Utilities to help perform common Reflection operations
    /// </summary>
    public class ReflectUtil
    {
        /// <summary>
        /// Avoid using System.Reflection.Assembly.GetTypes() in a KSP mod that
        /// wants to walk over all the Assemblies getting all their Types, and instead call this.
        ///  
        /// Doing so fixes https://github.com/KSP-KOS/KOS/issues/2491.
        /// 
        /// If there was an exception during the loading of Assembly from its DLL, then the
        /// types that failed to load will simply be culled from this list rather than the
        /// default GetTypes() behavior of aborting with an exception.
        /// (That may mean it's a list of zero length if the entire DLL failed to load.)
        /// </summary>
        /// <param name="assembly"></param>
        public static Type[] GetLoadedTypes(Assembly assembly)
        {
            try
            {
                // If there are no Types that failed to load, then we can just return the normal result
                // of GetTypes().
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                // If our DLLs *or any another mod's DLL outside our control* has enough of a
                // version mismatch with KSP that it can't load, then calling that
                // Assembly's GetTypes() throws this exception.

                // But this exception doesn't actually abort GetTypes()' work.  It still
                // does build the full list.  It just stores that list
                // in the exception's 'Types' member instead of returning it.

                // BUT, that list will contain some nulls in it - placeholders for where
                // the missing Types would have appeared in the list had they been loaded.
                // Getting rid of those nulls should result in just the types that actually
                // are loaded, which is what we really want:
                return exception.Types.Where(t => t != null).ToArray();

                // There is no error message logged here because presumably failing to load
                // one of the classes should have resulted in that mod failing to work, with
                // plenty of other error messages that are "nearer to the cause" than this is.
            }
        }
    }
}
