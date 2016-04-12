using System;

namespace kOS.Safe.Exceptions
{
    public class KOSDeprecationException : KOSException
    {
        protected static string TerseMessageFmt = "As of kOS {0}, {1} is obsolete and has been replaced with {2}";

        public KOSDeprecationException(string version, string oldUsage,string newUsage, string url) :
            base( String.Format(TerseMessageFmt, version, oldUsage, newUsage) )
        {
        }
    }
}