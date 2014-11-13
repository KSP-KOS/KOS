using kOS.Safe.Exceptions;

namespace kOS.Safe.Persistence
{
    public class KOSFileException : KOSException
    {
        public KOSFileException(string message)
            : base(message)
        {
        }
    }
}