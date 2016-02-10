using kOS.Safe.Encapsulation;

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
    /// This must ONLY be ever used by suffixes that intend to let some other
    /// part of their call push a return value onto the cpu stack rather than
    /// having it be returned by the suffix call itself.<br/>
    /// <br/>
    /// (For example, when performing user delegate calls with :CALL).
    /// </summary>
    public class KOSPassThruReturn : Structure
        
        // This is derived from Structure ONLY because it is 
        // a thing the :CALL() suffix can temporarily return
        // on the stack, and suffixes have been changed so the
        // thing they return MUST now be derived from Structure.
        // Making this be derived from Structure was the easiest
        // way to keep the CALL() suffix working.  This is a dummy
        // placeholder anyway, so the fact that it's derived from
        // Suffix doesn't mean much, other than allowing it to
        // be the value of DelegateSuffixResult.value.
    {
        public KOSPassThruReturn()
        {
        }
    }
}
