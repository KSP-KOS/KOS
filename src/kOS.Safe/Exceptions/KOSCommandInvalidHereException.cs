using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown when a command would normally be allowed and the parser
    /// thinks it's perfectly valid syntax, but there's some contextual
    /// semantic reason that it can't be used WHERE it's being used.
    /// Example:  Trying to PRESERVE when *not* in a trigger body.
    /// </summary>
    public class KOSCommandInvalidHereException : KOSCompileException
    {
        private const string TERSE_MSG_FMT = "'{0}' command found {1}. It only works {2}.";

        protected string VerbosePrefix =
            "While most commands in kOS work anywhere you\n" +
            "put them, there are a few exceptions in which\n" +
            "a command is only meaningful in some limited\n" +
            "places of the code.  This is one of those cases.\n";

        // Just nothing by default:
        public override string HelpURL { get { return ""; } }

        public override string VerboseMessage { get { return VerbosePrefix; } }

        /// <summary>
        /// Describe the condition under which the invalidity is happening.
        /// </summary>
        /// <param name="line">current line num in script where the problem was</param>
        /// <param name="col">current col num in script where the problem was</param>
        /// <param name="command">string name of the invalid command</param>
        /// <param name="badPlace">describing where in code the it's not being allowed.
        /// Use a phrasing that starts with a preposition, i.e. "in a loop", "outside a loop"</param>
        /// <param name="goodPlace">describing what sort of code the it is meant to be used in instead.
        /// Use a phrasing that starts with a preposition, i.e. "in a loop", "outside a loop"</param>
        public KOSCommandInvalidHereException(int line, int col, string command, string badPlace, string goodPlace) :
            base(line, col, string.Format(TERSE_MSG_FMT, command, badPlace, goodPlace))
        {
        }
    }
}