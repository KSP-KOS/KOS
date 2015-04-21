using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown when user tries to call a function/suffix that is reliant on the availability of a certain addon, 
    /// like RemoteTech, Kerbal Alarm Clock or AGX.
    /// </summary>
    public class KOSUnavailableAddonException : KOSException
    {
        private const string TERSE_MSG_FMT = "'{0}' command requires {1} addon to be available.";

        protected string VerbosePrefix =
            "It seems you tried calling a function or suffix \n" +
            "that relies on the availability of a certain mod installed.\n" +
            "You can check the availability of addons via 'addons' global, \n" +
            "for example 'addons:rt:available' returns true if RemoteTech is present.\n";

        // Just nothing by default:
        public override string HelpURL { get { return ""; } }
        public override string VerboseMessage { get { return VerbosePrefix; } }

        /// <summary>
        /// Describe the condition under which the invalidity is happening.
        /// </summary>
        /// <param name="command">string name of the invalid command</param>
        /// <param name="addonRequired">describing which addon/mod is required</param>
        public KOSUnavailableAddonException(string command, string addonRequired) :
            base(String.Format(TERSE_MSG_FMT, command, addonRequired))
        {
        }
    }
}
