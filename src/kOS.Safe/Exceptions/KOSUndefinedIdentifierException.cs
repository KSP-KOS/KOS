using System;

namespace kOS.Safe.Exceptions
{
    public class KOSUndefinedIdentifierException : KOSException
    {
        private const string TERSE_MSG_FMT = "Undefined Variable Name '{0}'. {1}";
 
        /// <summary> 
        /// Throw when there's a user-land identifier that hasn't been defined.
        /// </summary>
        /// <param name="identifier">identifier that had the problem</param>
        /// <param name="message">optional further information</param>
        public KOSUndefinedIdentifierException(string identifier, string message) :
            base(string.Format(TERSE_MSG_FMT, identifier, message))
        {
        }
    }
}
