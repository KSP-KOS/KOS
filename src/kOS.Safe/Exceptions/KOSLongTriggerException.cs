using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown when CONFIG:IPU is exceeded in a trigger.
    /// </summary>
    public class KOSLongTriggerException: KOSException
    {
        private const string TERSE_MSG_FMT = "Ran more than {0} instructions in trigger bodies, exceeding CONFIG:IPU";

        public override string VerboseMessage
        {
            get
            {
                return
                    "* (\"Trigger\" means a WHEN or ON or LOCK command.)\n" +
                    "\n"+
                    "All the trigger code blocks being executed must\n" +
                    "complete within one update tick of KSP.  If the\n" +
                    "total instructions run by triggers exceeds the\n" +
                    "CONFIG:IPU setting, then you get this error.\n" +
                    "\n" +
                    "TO FIX THIS PROBLEM, TRY ONE OR MORE OF THESE:\n" +
                    " - Find a way to move some of your logic out of\n" +
                    " the triggers and into the mainline code instead.\n" +
                    " - Redesign your triggers to use less code.\n" +
                    " - Make CONFIG:IPU value bigger if you have a fast\n" +
                    "computer that can handle the load.\n" +
                    " - If your trigger body was meant to be a loop, \n" +
                    " consider using the PRESERVE keyword instead\n" +
                    " to make it run one iteration per Update.\n";
            }
        }
        
        public override string HelpURL
        {
            get{ return "https://ksp-kos.github.io/KOS_DOC/summary_topics/CPU_hardware/index.html#TRIGGERS"; }
        }

        public KOSLongTriggerException(int numInstructions) :
            base( String.Format(TERSE_MSG_FMT,numInstructions) )
                 {
                 }
        }
}
