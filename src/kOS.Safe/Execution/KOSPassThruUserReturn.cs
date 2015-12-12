namespace kOS.Safe.Execution
{
    /// <summary>
    /// This is a special "type" used as a dummy return type for
    /// KOS suffixes and built-ins to indicate that when executing
    /// them in OpcodeCall, their return value should not even exist
    /// on the stack at all.<br/>
    /// <br/>
    /// Normally, kOS puts a dummy return on the
    /// stack even for "void" suffixes, that will get popped off by an
    /// OpcodePop as the next instruction.  But in the case where the suffix
    /// is meant to wait for user code to finish executing in order to get
    /// the return value (such as the suffix KOSDelegate:CALL()), this makes
    /// a problem.<br/>
    /// <br/>
    /// The problem is that user code like a user function won't actually start
    /// executing until after the OpcodeGetMember or OpcodeGetMethod that was
    /// calling the suffix finishes its current opcode.  So if they push a return
    /// value onto the stack before they finish, then that return value gets in
    /// the way of the arguments being sent to the user function that hasn't
    /// had a chance to start yet.<br/>
    /// <br/>
    /// This must ONLY be ever used by suffixes that intend to jump the 
    /// instruction pointer to a subroutine as the next instruction, and expect
    /// the subroutine to put a return value on the stack instead of this suffix.<br/>
    /// <br/>
    /// (For example, when performing user delegate calls with :CALL).
    /// </summary>
    public class KOSPassThruUserReturn
    {
        public KOSPassThruUserReturn()
        {
        }
    }
}
