using System;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Exceptions
{
    public class KOSInvalidPathException : KOSException
    {
        private string pathString;

        public KOSInvalidPathException(string message, string pathString) : base(message + " (" + pathString + ")")
        {
            this.pathString = pathString;
        }

        public new string VerboseMessage
        {
            get { return base.Message + ": '" + pathString + "'. This error occurred while trying to access a kOS Volume"; }
        }
    }
}

