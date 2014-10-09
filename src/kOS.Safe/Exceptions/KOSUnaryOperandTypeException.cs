using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Indicates that some sort of unary expression operation was performed
    /// on an illegal data type, for example trying to "not" a string.
    /// </summary>
    public class KOSUnaryOperandTypeException: KOSException
    {
        private const string TERSE_MSG_FMT = "Cannot {0} {1}";

        // for now just put a placeholder in using the terse message as the verbose one:
        public override string VerboseMessage { get{ return base.Message;} }

        public override string HelpURL { get{ return "";} }
        
        /// <summary>
        /// Describe the error in terms of the two operands and the verb/preposition
        /// being done with them.  For example:
        /// </summary>
        /// <param name="verb">present-tense singular conjugation of the operation's verb, i.e "negate"</param>
        /// <param name="operand">operand object being worked on</param>
        public KOSUnaryOperandTypeException(string verb, object operand) :
            base(String.Format(TERSE_MSG_FMT, verb, operand.GetType().Name))
        {
        }
    }
}
