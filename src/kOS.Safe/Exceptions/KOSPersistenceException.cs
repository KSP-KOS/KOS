using System;

namespace kOS.Safe.Exceptions
{
    public class KOSPersistenceException : Exception
    {
        public KOSPersistenceException(string message) : base(message)
        {
        }

        public KOSPersistenceException(string message, Exception cause) : base(message, cause)
        {
        }

        public string VerboseMessage
        {
            get { return base.Message + "\n" + "This error occurred while trying to load or save from a kOS Volume"; }
        }

        public string HelpURL
        {
            get { return string.Empty; }
        }
    }
}
