using System;

namespace kOS.Safe.Exceptions
{
    public class KOSLowTechException : KOSException
    {
        public override string HelpURL
        {
            get { /*TODO:*/ return "TODO - need to write docs about this feature and put URL here"; }
        }

        private const string TERSE_MESSAGE_FMT = "You need a better {0} in order to {1}.";

        public override string VerboseMessage
        {
            get
            {
                return base.Message + "\n" +
                       "This requirement was added in order to make kOS thematically fit the new career paradigm first introduced by KSP 0.90.";
            }
        }

        /// <summary>
        /// A KOSException to be thrown whenever the tech level of either
        /// the kOS CPU part or an upgradable career building isn't good enough to allow
        /// the feature that the script is trying to use.
        /// <br/><br/>
        /// TODO: Replace this with a mere warning if and when warnings get implemented later.
        /// </summary>
        /// <param name="feature">string describing what kOS feature is being disallowed. Phrase it as a verb.</param>
        /// <param name="thingToUpgrade">string describing what thing is too low-tech to allow the feature.  Phrase it as a noun.</param>
        public KOSLowTechException(string feature, string thingToUpgrade) :
            base(String.Format(TERSE_MESSAGE_FMT, thingToUpgrade, feature))
        {
        }
    }
}