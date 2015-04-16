using System.Collections.Generic;

namespace kOS.Safe.Execution
{
    /// <summary>
    /// A callback reference to a user-land function, implemented in kRISC code<br/>
    /// <br/>
    /// (As opposed to being a C# delegate, implemented in C# code).<br/>
    /// </summary>
    public class UserDelegate : IUserDelegate
    {
        public int EntryPoint {get; private set;}
        private readonly ICpu cpu;
        public List<VariableScope> Closure {get; private set;}
        
        /// <summary>
        /// Make a new UserDelegate given the current state of the CPU and its stack, and
        /// the entry point location of the function to call.
        /// </summary>
        /// <param name="cpu">the CPU on which this program is running.</param>
        /// <param name="entryPoint">instruction address where OpcodeCall should jump to to call the function.</param>
        /// <param name="useClosure">If true, then a snapshot of the current scoping stack, and thus a persistent ref to its variables,
        ///   will be kept in the delegate so it can be called later as a callback with closure.  Set to false if the
        ///   function is only getting called instantly using whatever the scope is at the time of the call.</param>
        public UserDelegate(ICpu cpu, int entryPoint, bool useClosure)
        {
            this.cpu = cpu;
            EntryPoint = entryPoint;
            if (useClosure)
                CaptureClosure();
            else
                Closure = new List<VariableScope>(); // make sure it exists as an empty list so we don't have to have 'if null' checks everwywhere.
        }

        private void CaptureClosure()
        {
            Closure = cpu.GetCurrentClosure();
        }
        
        public override string ToString()
        {
            return "UserDelegate( cpu=" + cpu.ToString() + ", entryPoint=" + EntryPoint.ToString() + ", Closure=" + Closure.ToString();
        }

    }
}
