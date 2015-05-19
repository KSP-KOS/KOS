using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Describe an error in the number of arguments.
        /// </summary>
        /// <param name="expected">number of expected arguments</param>
        /// <param name="actual">number of actual arguments</param>
        /// <param name="message">optional message</param>
        public KOSArgumentMismatchException(int expected, int actual, string message = "") :
            this(new[] { expected }, actual, message)
        {
        }

        /// <summary>
        /// Describe an error in the number of arguments.
        /// </summary>
        /// <param name="expected">a list of the expected arguments</param>
        /// <param name="actual">number of actual arguments</param>
        /// <param name="message">optional message</param>
        public KOSArgumentMismatchException(IList<int> expected, int actual, string message = "") :
            base(string.Format("{0} {1}", BuildTerseMessage(expected, actual), message))
        {
        }

        /// <summary>
        /// Describe an error in the number of arguments, without knowing the number of arguments
        /// </summary>
        /// <param name="message">optional message</param>
        public KOSArgumentMismatchException(string message = "") :
            base(string.Format("{0} {1}", BuildTerseMessage(), message))
        {
        }

        private static string BuildTerseMessage(IList<int> expected, int actual)
        {
            var expectedDisplay = (expected.Any() ? String.Join(", ", new List<int>(expected).ConvertAll(i => i.ToString()).ToArray()) : "no");
            var pluralDecorator = (expected.Count() == 1 ? "" : "s");
            var actualArgs = (actual == 0 ? "none" : actual.ToString());

            return string.Format("Incorrect number of arguments.  Expected {0} argument{1}, but found {2}.",
                                 expectedDisplay, pluralDecorator, actualArgs);
        }

        private static string BuildTerseMessage()
        {
            return String.Format("Number of arguments passed to the function didn't match the number of DECLARE PARAMETERs encountered.");
        }

        private string BuildVerboseMessage()
        {
            return
                Message + "\n" +
                "The number of arguments being passed into a function call is not correct.\n";
        }
    }
}