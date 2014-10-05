namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown whenever KOS compiler encounters something it does not like.
    /// This is to be distinguished from errors that occur while code is
    /// actually running.  This exception, and exceptions derived from
    /// it, might be handled differently because they are expected to
    /// occur *prior* to actually letting the CPU start executing the
    /// program's opcodes.
    /// </summary>
    public class KOSCompileException: KOSException
    {
        // Just default the Verbose message to return the terse message:
        public override string VerboseMessage { get{return base.Message;} }

        // Just nothing by default:
        public override string HelpURL { get{ return "";} }

        public KOSCompileException(string message) : base(message)
        {
        }
    }
}
