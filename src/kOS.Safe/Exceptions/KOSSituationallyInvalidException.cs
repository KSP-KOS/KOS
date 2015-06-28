namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// To be thrown whenever a command cannot be used or is going to
    /// give bogus data due to the KSP GAME situation it's being called from
    /// being invalid.<br/>
    /// This is to be distinguished from various compile
    /// exceptions about commands being disallowed in certain parts of the
    /// code.<br/>
    /// This is for runtime situations where it's not kOS that prevents
    /// things from working, but KSP itself, according to the game
    /// rules.<br/>
    /// Examples of the sorts of things where it might be
    /// appropriate to throw this include:<br/>
    ///  - Trying to use time warp (not phys warp) when in atmosphere.<br/>
    ///  - Trying to deploy a parachute that is broken.<br/>
    ///  - Trying to see the manuever nodes of a vessel that is not the active vessel. (KSP only
    /// attaches a patchedConicSolver to the current ActiveVessel.  It's null for all others.)
    /// </summary>
    public class KOSSituationallyInvalidException : KOSException
    {

        public override string VerboseMessage { get { return Message; } }

        public KOSSituationallyInvalidException(string msg) :
            base(msg)
        {
        }
    }
}