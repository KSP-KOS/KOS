using System.Collections.Generic;
using kOS.Safe.Execution;
using kOS.Safe.Compilation;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// A callback reference to a user-land function, implemented in kRISC code<br/>
    /// <br/>
    /// (As opposed to being a C# delegate, implemented in C# code).<br/>
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("UserDelegate")]
    public class UserDelegate : KOSDelegate, IUserDelegate
    {
        public IProgramContext ProgContext {get; private set;}
        
        /// <summary>
        /// The entry point to jump to.  Note if it's ever a negative number, then you should not
        /// call this UserDelegate and instead should treat it as a dummy null delegate.
        /// </summary>
        public int EntryPoint {get; private set;}

        public List<VariableScope> Closure {get; private set;}
        
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
            else
                Closure = new List<VariableScope>(); // make sure it exists as an empty list so we don't have to have 'if null' checks everwywhere.
        }

        public UserDelegate(UserDelegate oldCopy) : base(oldCopy)
        {
            ProgContext = oldCopy.ProgContext;
            EntryPoint = oldCopy.EntryPoint;
            Closure = oldCopy.Closure;
        }
        
        public override KOSDelegate Clone()
        {
            return new UserDelegate(this);
        }

        private void CaptureClosure()
        {
            Closure = Cpu.GetCurrentClosure();
        }
        
        public override string ToString()
        {
            return string.Format("UserDelegate(cpu={0}, entryPoint={1}, Closure={2},\n   {3})",
                                 Cpu, EntryPoint, Closure, base.ToString());
        }
        
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
            // Going to do an indirect call of myself, and indirect calls need
            // to have the delegate underneath the args.  That's how
            // OpcodeCall.StaticExecute() expects to see it.
            Cpu.PushStack(this);
        }

        public override Structure CallWithArgsPushedAlready()
        {
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
        /// it directly from that).
        /// </summary>
        public TriggerInfo TriggerNextUpdate(params Structure[] args)
        {
            return Cpu.AddTrigger(this, args);
        }
    }
}
