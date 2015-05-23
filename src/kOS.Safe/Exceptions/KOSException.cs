using System;

namespace kOS.Safe.Exceptions
{
    public class KOSException : Exception
    {
        public KOSException(string message):base(message)
        {
        }

        public virtual string VerboseMessage
        {
            get { return base.Message; }
        }

        public virtual string HelpURL
        {
            get { return string.Empty; }
        }
    }
}