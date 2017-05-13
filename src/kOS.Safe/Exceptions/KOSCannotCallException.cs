using System;

namespace kOS.Safe.Exceptions
{
    public class KOSCannotCallException : KOSException
    {
        private static string messageString = "An attempt was made to call NoDelegate, which can't be called.";
        
        public KOSCannotCallException() :base(messageString)
        {
        }
    }
}