using System;

namespace kOS.Debug
{
    public class KOSReadOnlyException : KOSException
    {
        public KOSReadOnlyException(string varName) : base (varName + " is read-only")
        {
        }
    }
}