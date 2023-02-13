using System;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Exceptions
{
    public class KOSYouShouldNeverSeeThisException : KOSException
    {
        public KOSYouShouldNeverSeeThisException(string message) : base("This is an error end users should never see. If you see this please report it on the kOS github:\r\n    " + message)
        {
        }
    }
}