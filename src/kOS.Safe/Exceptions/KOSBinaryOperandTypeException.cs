using System;
using kOS.Safe.Compilation;

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
        public override string VerboseMessage { get{ return Message;} }

        public override string HelpURL { get{ return "";} }
        
        /// <summary>
        /// Describe the error in terms of the two operands and the verb/preposition
        /// being done with them.  For example:
        /// </summary>
        /// <param name="pair">the left and right objects of the preposition</param>
        /// <param name="verb">present-tense singular conjugation of the operation's verb, i.e "add"</param>
        /// <param name="preposition">preposition usually used with the verb, i.e you add "to", but divide "by".</param>
        public KOSBinaryOperandTypeException(OperandPair pair, string verb, string preposition) :
            base(String.Format(TERSE_MSG_FMT, verb, pair.Left.GetType().Name, preposition, pair.Right.GetType().Name))
        {
        }
    }
}
