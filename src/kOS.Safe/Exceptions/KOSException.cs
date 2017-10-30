using System;

namespace kOS.Safe.Exceptions
{
    public class KOSException : Exception
    {
        public KOSException(string message):base(message)
        {
        }
        
        public KOSException() // a default constructor is needed for how KOSCompileException works
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