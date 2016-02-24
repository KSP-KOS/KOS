namespace kOS.Safe.Exceptions
{
    public class KOSWrongCPUVesselException : KOSException
    {
        public KOSWrongCPUVesselException()
            : base("Access to requested suffix or structure is limited to the vessel on which this processor is mounted.")
        {
        }

        public KOSWrongCPUVesselException(string suffix)
            : base(string.Format("Access to {0} is limited to the vessel on which this processor is mounted.", suffix))
        {
        }

        public override string VerboseMessage
        {
            get
            {
                return "The suffix or structure you requested is only accessible from the current " +
                       "CPU vessel.  Common examples include trying to access the CONTROL suffix " +
                       "on another vessel, or the DOEVENT suffix on a part from another vessel.  " +
                       "Make sure you are not caching the value prior to a staging or docking event.";
            }
        }
    }
}