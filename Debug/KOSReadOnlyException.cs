using System;

namespace kOS.Debug
{
    public class KOSReadOnlyException : KOSException
    {
        public KOSReadOnlyException(String varName) : base (varName + " is read-only")
        {
        }
    }
}