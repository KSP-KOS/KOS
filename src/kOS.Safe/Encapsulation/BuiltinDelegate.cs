using System.Collections.Generic;
using kOS.Safe.Execution;
using kOS.Safe.Compilation;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// A callback reference to a built-in function, (one of the kinds derived from kOS.Function.FunctionBase).<br/>
    /// <br/>
    /// </summary>
    public class BuiltinDelegate : KOSDelegate
    {
        public string Name { get; set; }
        
        public BuiltinDelegate(ICpu cpu, string Name) :
            base(cpu)
        {
            this.Name = Name;
        }

        public BuiltinDelegate(BuiltinDelegate oldCopy) : base(oldCopy)
        {
            this.Name = oldCopy.Name;
        }
        
        public override KOSDelegate Clone()
        {
            return new BuiltinDelegate(this);
        }
        
        public override string ToString()
        {
            return string.Format("BuiltinDelegate(cpu={0}, Name={1},\n  {2})",
                                 cpu.ToString(), Name, base.ToString());
        }
        
        public override void PushUnderArgs()
        {
            // do nothing.
        }
        
        public override object Call()
        {
            int throwAway = OpcodeCall.StaticExecute(cpu, true, Name, true);
            // throwAway will be -1 for cases where it's a builtin function.
            // and the return value will be left atop the stack by StaticExecute.
            return new KOSPassThruReturn();
        }
    }
}
