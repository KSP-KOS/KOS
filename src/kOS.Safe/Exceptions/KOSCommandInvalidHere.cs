using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown when a command would normally be allowed and the parser
    /// thinks it's perfectly valid syntax, but there's some contextual
    /// semantic reason that it can't be used WHERE it's being used.
    /// Example:  Trying to PRESERVE when *not* in a trigger body.
    /// </summary>
    public class KOSCommandInvalidHere: KOSCompileException, IKOSException
    {
        private static string terseMsgFmt = "'{0}' command found {1}. It only works {2}.";
        
        public override string VerboseMessage { get { return VerbosePrefix; } set{} }
        
        protected string VerbosePrefix =
            "While most commands in kOS work anywhere you\n" +
            "put them, there are a few exceptions in which\n" +
            "a command is only meaningful in some limited\n" +
            "places of the code.  This is one of those cases.\n";

        // Just nothing by default:
        public override string HelpURL { get{ return "";} set{} }

        /// <summary>
        /// Describe the condition under which the invalidity is happening.
        /// </summary>
        /// <param name="command">string name of the invalid command</param>
        /// <param name="badPlace">describing where in code the it's not being allowed.
        /// Use a phrasing that starts with a preposition, i.e. "in a loop", "outside a loop"</param>
        /// <param name="goodPlace">describing what sort of code the it is meant to be used in instead.
        /// Use a phrasing that starts with a preposition, i.e. "in a loop", "outside a loop"</param>
        public KOSCommandInvalidHere(string command, string badPlace, string goodPlace) :
            base(String.Format(terseMsgFmt, command, badPlace, goodPlace))
        {
        }
    }
}
