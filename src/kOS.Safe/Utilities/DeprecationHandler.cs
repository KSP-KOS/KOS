using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Utilities
{
    public static class DeprecationHandler
    {
        private const string DeprecationMessage =
            "WARNING: Usage of {0} is being deprecated.\nUse {1} instead.\n"+
            "At kOS version {2} this usage will be removed.\n"+
            "To disable these warnings do \"config:deprecatedwarnings off.\"";
        
        public static void DeprecatedUsage(SafeSharedObjects shared, string oldUsage, string newUsage, VersionInfo completeDeprecationVersion)
        {
            if (SafeHouse.Version.SystemVersion >= completeDeprecationVersion.SystemVersion)
            {
                throw new KOSObsoletionException(completeDeprecationVersion.ToString(), oldUsage, newUsage, null);
            }
            if (SafeHouse.Config.DeprecatedWarnings)
            {
                shared.Screen.Print(string.Format(DeprecationMessage, oldUsage, newUsage, completeDeprecationVersion), true);
            }
        }
    }
}
