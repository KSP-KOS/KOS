using System;

namespace kOS.Safe.Exceptions
{
    public class KOSPatchesDeprecationException : KOSDeprecationException
    {
        protected static string OldUsage { get{return "orbit's :PATCHES suffix";}}
        protected static string NewUsage { get{return "the :PATCHES suffix of Vessel, or Orbit's :NEXTPATCH and :HASNEXTPATCH suffixes";}}
        protected static string Version { get{return "v0.15";}}
        protected static string Url { get{return "TODO for v0.15 - Go back and fill in after docs are updated";}}
        
        public KOSPatchesDeprecationException() : 
            base(Version, OldUsage, NewUsage, Url)
        {
        }

        public override string VerboseMessage
        {
            get
            {
                return
                    base.Message + "\n" +
                    "In the past, you could get the list of patches\n" +
                    "of a vessel by saying SHIP:OBT:PATCHES, but the\n" +
                    "list of all patches was moved up one level to\n" +
                    "the vessel itself and you can do this instead:\n" +
                    "SHIP:PATCHES\n" +
                    "\n" +
                    "If you want to treat the orbit patches like a\n" +
                    "linked list, you can alternatively use the \n" +
                    ":NEXTPATCH suffix of an Orbit to get the next\n" +
                    "patch, and then :HASNEXTPATCH will tell you\n" +
                    "when there is no next patch.\n" +
                    "\n" +
                    "For example, these are two ways to get the same\n" +
                    "thing:\n" +
                    "  SHIP:OBT:NEXTPATCH:NEXTPATCH:NEXTPATCH\n" +
                    "  SHIP:PATCHES[3]\n" +
                    "And these are also two ways to get the the same\n" +
                    "thing:\n" +
                    "  SHIP:OBT\n" +
                    "  SHIP:PATCHES[0]\n";
            }
        }

        public override string HelpURL
        {
            get { return Url; }
        }
    }
}