namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown when you attempt to make a function call like:<br/>
    ///   identifier().
    /// and the identifier doesn't resolve to an invokable thing.
    /// (not a user function, suffix, or built-in function).
   /// </summary>
    public class KOSNotInvokableException : KOSException
    {
        private const string TERSE_MSG_FMT = "Attempted to make a function call on a non-invokable object:\n   {0}";
        public object objAttempted;

        public KOSNotInvokableException(object objAttempted) :
            base(string.Format(TERSE_MSG_FMT,objAttempted.ToString()))
        {
            this.objAttempted = objAttempted;
        }

        public override string VerboseMessage
        {
            get { return Message; }
        }

    }
}