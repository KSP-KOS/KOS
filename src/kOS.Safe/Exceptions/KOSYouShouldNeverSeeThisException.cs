using System;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation
{
    public class KOSYouShouldNeverSeeThisException : KOSException
    {
        public KOSYouShouldNeverSeeThisException(string message) : base("This is an error endusers should never see, if you see this please report it to the kOS devs " + message)
        {
        }
    }
}