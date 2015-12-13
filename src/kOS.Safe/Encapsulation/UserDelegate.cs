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
    public class UserDelegate : KOSDelegate, IUserDelegate
    {
        public IProgramContext ProgContext {get; private set;}
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
            Closure = cpu.GetCurrentClosure();
        }
        
        public override string ToString()
        {
            return string.Format("UserDelegate(cpu={0}, entryPoint={1}, Closure={2},\n   {3})",
                                 cpu.ToString(), EntryPoint.ToString(), Closure.ToString(), base.ToString());
        }
        
        public override void PushUnderArgs()
        {
            // Going to do an indirect call of myself, and indirect calls need
            // to have the delegate underneath the args.  That's how
            // OpcodeCall.StaticExecute() expects to see it.
            cpu.PushStack(this);
        }
        
        public override object Call()
        {
            int absoluteJumpTo = OpcodeCall.StaticExecute(cpu, false, "", true);
            if (absoluteJumpTo >= 0)
                cpu.InstructionPointer = absoluteJumpTo - 1; // -1 because it increments by 1 automatically between instructions.
            
            // Remember this is just a special flag telling OpcodeCall to never place
            // this suffix's C# delegate return value on the stack.  It's like saying
            // "even more void that void", because normally even a void suffix gets a
            // dummy return value.  This says to not even do that - just offload the
            // responsibility for pushing a return value onto the user code that is
            // about to be jumped into.
            return new KOSPassThruReturn();
        }
    }
}
