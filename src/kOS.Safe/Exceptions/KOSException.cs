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

    public class KOSDuplicateKeyException : KOSException
    {
        private const string MSG = "The given key {0} is already present, use CONTAINS for checking safety. This is a case {1} collection";

        public KOSDuplicateKeyException(string key, bool caseSensitive) : base(string.Format(MSG, key, caseSensitive ? "insensitive" : "sensitive"))
        {

        }
    }
}