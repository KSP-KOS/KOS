using System;

namespace kOS.Safe.Exceptions
{
    public class KOSException : Exception
    {
        public KOSException() // a default constructor is needed for how KOSCompileException works
        {
        }

        public KOSException(string message) : base(message)
        {
        }

        public KOSException(string message, params object[] args) : base(string.Format(message, args))
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
