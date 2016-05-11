using System;

namespace kOS.Safe.Exceptions
{
    public class KOSCommunicationException : KOSException
    {
        public KOSCommunicationException(string message) : base(message)
        {
        }
    }
}

