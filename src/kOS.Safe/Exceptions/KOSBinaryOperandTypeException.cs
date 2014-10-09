using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Indicates that some sort of binary expression operation was performed
    /// on illegal data types, for example trying to multiply strings.
    /// </summary>
    public class KOSBinaryOperandTypeException: KOSException
    {
        private const string TERSE_MSG_FMT = "Cannot {0} {1} {2} {3}";

        // for now just put a placeholder in using the terse message as the verbose one:
        public override string VerboseMessage { get{ return base.Message;} }

        public override string HelpURL { get{ return "";} }
        
        /// <summary>
        /// Describe the error in terms of the two operands and the verb/preposition
        /// being done with them.  For example:
        /// </summary>
        /// <param name="leftSide">operand object on the left side of the preposition</param>
        /// <param name="verb">present-tense singular conjugation of the operation's verb, i.e "add"</param>
        /// <param name="preposition">preposition usually used with the verb, i.e you add "to", but divide "by".</param>
        /// <param name="rightSide">operand object on the right side of the preposition</param>
        public KOSBinaryOperandTypeException(object leftSide, string verb, string preposition, object rightSide) :
            base(String.Format(TERSE_MSG_FMT, verb, leftSide.GetType().Name, preposition, rightSide.GetType().Name))
        {
        }
    }
}
