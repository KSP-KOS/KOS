using System.Text;

namespace kOS.Safe.Exceptions
{
    public class KOSStackOverflowException : KOSException
    {
        private const string formatMsg = "%s Stack Overflow";
        private const string verboseMsg = "This is usually caused by excessive recursion in the script.";
       
        public KOSStackOverflowException(string whatStack) : base(string.Format(formatMsg, whatStack))
        {
        }
            
        public override string VerboseMessage
        {
            get { return string.Format("%s %s", base.Message, verboseMsg); }
        }
    }
}

