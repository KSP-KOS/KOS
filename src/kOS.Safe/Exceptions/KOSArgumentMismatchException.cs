using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Indicates that a method or function was called with the 
    /// wrong number of arguments.
    /// </summary>
    public class KOSArgumentMismatchException : KOSException
    {
        public override string VerboseMessage
        {
            get { return BuildVerboseMessage(); }
        }

        public override string HelpURL
        {
            get { return string.Empty; }
        }
        
        private int expectedNum;
        private int actualNum;
        
        /// <summary>
        /// Describe an error in the number of arguments.
        /// </summary>
        /// <param name="expected">number of expected arguments</param>
        /// <param name="actual">number of actual arguments</param>
        /// <param name="message">optional message</param>
        public KOSArgumentMismatchException(int expected, int actual, string message = "" ) :
            base( BuildTerseMessage(expected,actual) + message )
        {
            expectedNum = expected;
            actualNum = actual;
        }
        
        private static string BuildTerseMessage(int expected, int actual)
        {
            return String.Format("Incorrect number of arguments.  Expected {0} argument{1}, but found {2}",
                                 (expected==0?"no":expected.ToString()), (expected==1?"":"s"), (actual==0?"none":actual.ToString())
                                );
        }
        
        private string BuildVerboseMessage()
        {
            return
                Message + "\n" +
                "The number of arguments being passed into a function call is not correct.\n";
        }
    }
}