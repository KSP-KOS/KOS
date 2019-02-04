namespace kOS.Safe.Execution
{
    /// <summary>
    /// The context record that gets pushed onto the stack to store what you need to
    /// know to return from a subroutine.
    /// </summary>
    public class SubroutineContext
    {
        /// <summary>In the case where this block context is for a subroutine that needs
        /// to jump back to the calling location, this stores what the calling location was.
        /// It is the instruction pointer that this subroutine call came from, and therefore
        /// should be returned to when it's done.</summary>
        public int CameFromInstPtr {get; private set;}

        /// <summary>
        /// Most normal subroutine calls don't escalate the priority, but
        /// some will. i.e The calls that are triggered by some kind of interrupt will raise the CPU's
        /// priority level.  When popping the subroutine context to return from this subroutine, this
        /// is the priority to de-escalate back down to, that the machine was in prior to this call.
        /// </summary>
        public InterruptPriority CameFromPriority;

        /// <summary>
        /// If this context record is from a trigger call the kOS CPU inserted, then
        /// this acts as an effective Unique ID for which trigger it was.
        /// </summary>
        public TriggerInfo Trigger {get; private set;}

        /// <summary>
        /// True if this context record represents a trigger call that was inserted
        /// by the kOS CPU itself rather than from an explicit call in the user's code.
        /// </summary>
        public bool IsTrigger {get { return (Trigger != null); }}

        /// <summary>
        /// True if this context record represents a trigger call that has been queued up to get
        /// executed but then got cancelled in the meantime by some other part of the 
        /// system.  If this is true, then the opcode OpcodeIsCancelled will push a True on the stack
        /// and the trigger can know it shouldn't fire.
        /// There is no automated enforcement of this rule in the CPU.  To pay attention to this feature,
        /// a trigger would have to be written with opcodes at the top which call OpcodeTestCancelled and
        /// pay attention to what it says to return prematurely if it's true.
        /// </summary>
        /// <value><c>true</c> if this instance is cancelled; otherwise, <c>false</c>.</value>
        public bool IsCancelled { get; private set;}

        /// <summary>Make a new Subroutine Context, with all the required data.</summary>
        /// <param name="cameFromInstPtr">which instruction did it come from (should it return to when this subroutine is over)</param>
        public SubroutineContext(int cameFromInstPtr, TriggerInfo trigger =  null)
        {
            CameFromInstPtr = cameFromInstPtr;
            Trigger = trigger;
            IsCancelled = false;
        }

        /// <summary>
        /// Call this to tell the subroutine to cancel itself if it's already on the call stack
        /// but hasn't executed yet (such as happens when the CPU inserts subroutine calls for
        /// each trigger at the start of a physics tick).  Note that a subroutine has to actually
        /// be written with opcodes that care about this for it to work.  The CPU does not enforce
        /// that a subroutine call will chose to cancel itself when you do this.  For this to have
        /// any effect, the subroutine has to start with opcodes that pay attention to this and
        /// obey it.  See OpcodeTestCancelled.
        /// </summary>
        /// <returns><c>true</c> if this instance cancel ; otherwise, <c>false</c>.</returns>
        public void Cancel()
        {
            IsCancelled = true;
        }
        
        public override string ToString()
        {
            return string.Format("SubroutineContext: {{CameFromInstPtr {0}, {1}TriggerPointer {2}}}", CameFromInstPtr, (IsCancelled ? "(cancelled) " : ""), Trigger);
        }
    }
}
