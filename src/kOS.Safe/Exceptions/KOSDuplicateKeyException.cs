namespace kOS.Safe.Exceptions
{
    public class KOSDuplicateKeyException : KOSException
    {
        private const string MSG = "The given key {0} is already present, use CONTAINS for checking safety. This is a case {1} collection";

        public KOSDuplicateKeyException(string key, bool caseSensitive) : base(string.Format(MSG, key, caseSensitive ? "insensitive" : "sensitive"))
        {

        }
    }
}