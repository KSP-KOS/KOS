using System;

namespace kOS.Safe.Execution
{
    /// <summary>
    /// The priority of the current mode of code, for deciding what
    /// kinds of interrupting triggers are allowed to break into the
    /// code right now.  The rule of thumb is that an interrupt must be of a
    /// higher level than the current level to be allowed to activate now.
    /// Interrupts of a lower level must wait until the interrupt level drops
    /// down again.
    /// </summary>
    public enum InterruptPriority : int
    {
        // Some space in between the numbers so it's possible to
        // add more fine-grain levels later if we feel like it:

        /// <summary>Use this value when you want to signal that
        /// you want the CPU priority to remain whatever it was with no
        /// escalation or de-escalation.</summary>
        NoChange = -999,
        /// <summary>Not in an interrupt at all (in mainline code)</summary>
        Normal = 0,
        /// <summary>A one-shot callback such as is common with GUI code</summary>
        CallbackOnce = 10,
        /// <summary>A user-made trigger that tends to keep getting scheduled to happen
        /// again as soon as the previous call to it is completed.</summary>
        Recurring = 20,
        /// <summary>These are recurring triggers too, but they absolutely MUST always fire
        /// because they are used for the coooked controls like LOCK THROTTLLE and
        /// LOCK STEERING</summary>
        RecurringControl = 30
    }
}

