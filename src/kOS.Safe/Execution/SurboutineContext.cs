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
        /// If this context record is from a trigger call the kOS CPU inserted, then
        /// this acts as an effective Unique ID for which trigger it was.
        /// </summary>
        public TriggerInfo Trigger {get; private set;}

        /// <summary>
        /// True if this context record represents a trigger call that was inserted
        /// by the kOS CPU itself rather than from an explicit call in the user's code.
        /// </summary>
        public bool IsTrigger {get { return (Trigger != null); }}

        /// <summary>Make a new Subroutine Context, with all the required data.</summary>
        /// <param name="cameFromInstPtr">Sets the ComeFromIP field</param>
        public SubroutineContext(int cameFromInstPtr, TriggerInfo trigger =  null)
        {
            CameFromInstPtr = cameFromInstPtr;
            Trigger = trigger;
        }
        
        public override string ToString()
        {
            return string.Format("SubroutineContext: {{CameFromInstPtr {0}, TriggerPointer {1}}}", CameFromInstPtr, Trigger);
        }
    }
}
