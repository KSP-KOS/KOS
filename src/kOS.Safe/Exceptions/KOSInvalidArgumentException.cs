namespace kOS.Safe.Exceptions
{
    public class KOSInvalidArgumentException : KOSException
    {
        private const string MSG = "While Invoking function {0}, argument {1} was invalid for the reason: {2}";

        public KOSInvalidArgumentException(string functionName, string argumentName, string reason) : base(string.Format(MSG, functionName, argumentName, reason))
        {
        }
    }
}
