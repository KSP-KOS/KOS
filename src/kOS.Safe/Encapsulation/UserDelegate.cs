using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using kOS.Safe.Compilation;
using System.Collections.Generic;
using System;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// A callback reference to a user-land function, implemented in kRISC code<br/>
    /// <br/>
    /// (As opposed to being a C# delegate, implemented in C# code).<br/>
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("UserDelegate")]
    public class UserDelegate : KOSDelegate, IUserDelegate, IPopContextNotifyee
    {
        private WeakReference weakProgContext;
        public IProgramContext ProgContext
        {
            get { return (IProgramContext)weakProgContext.Target; }
            private set { weakProgContext = new WeakReference(value); }
        }
        
        /// <summary>
        /// The entry point to jump to.  Note if it's ever a negative number, then you should not
        /// call this UserDelegate and instead should treat it as a dummy null delegate.
        /// </summary>
        public int EntryPoint {get; private set;}

        private List<VariableScope> closure;
        public List<VariableScope> Closure
        {
            get
            {
                CheckForDead(false);
                return closure;
            }
            private set
            {
                if (!CheckForDead(false))
                    closure = value;
            }
        }

        /// <summary>
        /// It is possible for a UserDelegate to continue existing after the
        /// program that contained it died.  (i.e. if you passed it to a
        /// part of the system as a callback hook)  In this case the UserDelegate
        /// shouldn't be called ever again.
        /// </summary>
        /// <returns><c>true</c> if this instance is dead (its Program context is GC'ed,
        /// or hasn't GC'ed YET but should soon because the CPU has moved on to a new
        /// program context); otherwise, <c>false</c>.</returns>
        public override bool IsDead()
        {
            return (weakProgContext != null) && // If this is still null then we got called during the constructor and this doesn't count yet.
                (
                    (Cpu == null) ||
                    (ProgContext == null) ||
                    (!weakProgContext.IsAlive) ||
                    (weakProgContext.Target == null) ||
                    (((IProgramContext)weakProgContext.Target).ContextId != Cpu.GetCurrentContext().ContextId)
                );
        }


        // Making Closure a weak reference is messy because it's a list.  Also, there
        // are times when it's correct for the closure to be the last thing left that's
        // keeping a variable alive, so we can't use weak references for it.  Instead we check it
        // regularly to see if it *should* be cleared out.  Essentially it should get cleared
        // any time the program context is stale.
        // Once the program context is stale, you can't call the UserDelegate anyway.
        // (The opcodes at the target instruction pointer have been cleared and possibly
        // replaced with something else so it would be a catastrophic bug to try).
        // The variables in the closure can be safely orphaned for GC'ing at that point.
        private bool CheckForDead(bool throwException)
        {
            bool dead = IsDead();
            if (dead)
            {
                DeadCleanup();
                if (throwException)
                    throw new KOSInvalidDelegateContextException("one program run", "another");
            }
            return dead;
        }

        /// <summary>
        /// Cleans up the references I hold (to allow GC to  happen on them) because I
        /// know I can never be called again at this point.
        /// (Note, this is called because of Cpu.AddPopContextNotifyee()).
        /// </summary>
        public bool OnPopContext(IProgramContext context)
        {
            // Just in case we ever add more contexts later than just interpreter
            // and program, we want to be sure we don't invoke the cleanup code
            // unless it's *our* program context (the one this UserDelegate is from)
            // that's being removed.  That's the reason for these checks here:
            if (weakProgContext.IsAlive)
            {
                if (weakProgContext.Target == context)
                {
                    DeadCleanup();
                    return false;
                }
                return true; // act like this never fired off.  It's not for our program context.
            }
            // If we already orphaned our program context for some other reason (I don't
            // know what that would be), then at least clean up what's still left:
            DeadCleanup();
            return false;
        }

        /// <summary>
        /// Call when this UserDelegate is confirmed to be dead, so it frees everything
        /// up and doesn't prevent things from garbage collecting:
        /// </summary>
        private void DeadCleanup()
        {
            if (weakProgContext != null)
                weakProgContext.Target = null;
            if (closure != null)
                closure.Clear();
        }

        /// <summary>
        /// Make a new UserDelegate given the current state of the CPU and its stack, and
        /// the entry point location of the function to call.
        /// </summary>
        /// <param name="cpu">the CPU on which this program is running.</param>
        /// <param name="context">The IProgramContext in which the entryPoint is stored.  Entry point 27 in the interpreter is not the same as entrypoint 27 in program context.</param>
        /// <param name="entryPoint">instruction address where OpcodeCall should jump to to call the function.</param>
        /// <param name="useClosure">If true, then a snapshot of the current scoping stack, and thus a persistent ref to its variables,
        ///   will be kept in the delegate so it can be called later as a callback with closure.  Set to false if the
        ///   function is only getting called instantly using whatever the scope is at the time of the call.</param>
        public UserDelegate(ICpu cpu, IProgramContext context, int entryPoint, bool useClosure) :
            base(cpu)
        {
            ProgContext = context;
            EntryPoint = entryPoint;
            if (useClosure)
                CaptureClosure();
            if (Cpu != null)
                Cpu.AddPopContextNotifyee(this);
        }

        public UserDelegate(UserDelegate oldCopy) : base(oldCopy)
        {
            ProgContext = oldCopy.ProgContext;
            EntryPoint = oldCopy.EntryPoint;
            Closure = oldCopy.Closure;
            if (Cpu != null)
                Cpu.AddPopContextNotifyee(this);
        }
        
        public override KOSDelegate Clone()
        {
            return new UserDelegate(this);
        }

        private void CaptureClosure()
        {
            if (Cpu != null)
                Closure = Cpu.GetCurrentClosure();
            else
                Closure = new List<VariableScope>(); // make sure it exists as an empty list so we don't have to have 'if null' checks everwywhere.
        }
        
        /*
        public override string ToString()
        {
            return string.Format("UserDelegate(cpu={0}, entryPoint={1}, Closure={2},\n   {3})",
                (Cpu == null ? "(No CPU)" : Cpu.ToString()), EntryPoint, Closure, base.ToString());
        }
        */
        
        public override bool Equals(object obj)
        {
            UserDelegate other = obj as UserDelegate;
            if (other == null)
                return false;
            return object.Equals(this.ProgContext, other.ProgContext) && this.EntryPoint == other.EntryPoint && object.Equals(this.Closure, other.Closure);
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            unchecked {
                if (ProgContext != null)
                    hashCode += 1000000007 * ProgContext.GetHashCode();
                hashCode += 1000000009 * EntryPoint.GetHashCode();
                if (Closure != null)
                    hashCode += 1000000021 * Closure.GetHashCode();
            }
            return hashCode;
        }

        public override void PushUnderArgs()
        {
            if (Cpu == null)
                throw new KOSCannotCallException();
            CheckForDead(true);
            // Going to do an indirect call of myself, and indirect calls need
            // to have the delegate underneath the args.  That's how
            // OpcodeCall.StaticExecute() expects to see it.
            Cpu.PushArgumentStack(this);
        }

        public override Structure CallWithArgsPushedAlready()
        {
            if (Cpu == null)
                throw new KOSCannotCallException();
            CheckForDead(true);
            int absoluteJumpTo = OpcodeCall.StaticExecute(Cpu, false, "", true);
            if (absoluteJumpTo >= 0)
                Cpu.InstructionPointer = absoluteJumpTo - 1; // -1 because it increments by 1 automatically between instructions.
            
            // Remember this is just a special flag telling OpcodeCall to never place
            // this suffix's C# delegate return value on the stack.  It's like saying
            // "even more void that void", because normally even a void suffix gets a
            // dummy return value.  This says to not even do that - just offload the
            // responsibility for pushing a return value onto the user code that is
            // about to be jumped into.
            return new KOSPassThruReturn();
        }

        /// <summary>
        /// A convienience shortcut to do a Cpu.AddTrigger for this UserDelegate.  See
        /// Cpu.AddTrigger to see what this is for.  This is useful for cases where you
        /// want to do an AddTrigger() but don't have access to the Shared.Cpu with which to
        /// do so (the UserDelegate knows which Cpu it was created with so it can get to
        /// it directly from that).  For the sake of preventing auto-repeating triggers
        /// from starving mainline code, the CPU's check for 'has mainline code been reached'
        /// will count the code in this trigger as qualifying as mainline code.  This should
        /// be used to trigger single-event one-shot callbacks, not for callbacks you
        /// expect to infinitely respawn every time they finish.  If you plan to infinitely
        /// re-execute a callback every time it finishes, you should probably do so using
        /// TriggerOnFutureUpdate() instead, to be "nice" to the rest of the code.
        /// </summary>
        public TriggerInfo TriggerOnNextOpcode(InterruptPriority priority, params Structure[] args)
        {
            if (CheckForDead(false))
                return null;
            return Cpu.AddTrigger(this, priority, Cpu.NextTriggerInstanceId, true, args);
        }

        /// <summary>
        /// Similar to TriggerOnNextOpcode(), except this won't trigger until the
        /// next KOSFixedUpdate in which the callstack is free of other similar
        /// future-update triggers like this one.  This is to be 'nice' to
        /// other kerboscript code and prevent these types of triggers from
        /// using 100% of the CPU time.  This should be used in
        /// cases where you intend to make a repeating callback by scheduling
        /// a new call as soon as you detect the previous one is done.  (like
        /// VectorRenderer's UPDATEVEC does for example).  It can also be used for
        /// one-shots as well, if you think it's okay for the one-shot to wait until
        /// at least the next update boundary to execute.
        /// </summary>
        public TriggerInfo TriggerOnFutureUpdate(InterruptPriority priority, params Structure[] args)
        {
            if (CheckForDead(false))
                return null;
            return Cpu.AddTrigger(this, priority, Cpu.NextTriggerInstanceId, false, args);
        }
    }
}
