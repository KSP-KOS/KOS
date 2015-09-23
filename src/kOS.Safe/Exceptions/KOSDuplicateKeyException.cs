namespace kOS.Safe.Exceptions
{
    public class KOSDuplicateKeyException : KOSException
    {
        private const string MSG = "The given key, ({0}), is already present in the case-{1} collection. Use :HASKEY for checking safety.";

        public KOSDuplicateKeyException(string key, bool caseSensitive) : base(string.Format(MSG, key, caseSensitive ? "sensitive" : "insensitive"))
        {

        }
    }
}