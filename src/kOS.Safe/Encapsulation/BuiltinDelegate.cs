using kOS.Safe.Compilation;
using kOS.Safe.Execution;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// A callback reference to a built-in function, (one of the kinds derived from kOS.Function.FunctionBase).<br/>
    /// <br/>
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("BuiltinDelegate")]
    public class BuiltinDelegate : KOSDelegate
    {
        public string Name { get; set; }
        
        public BuiltinDelegate(ICpu cpu, string name) :
            base(cpu)
        {
            Name = name;
        }

        public BuiltinDelegate(BuiltinDelegate oldCopy) : base(oldCopy)
        {
            Name = oldCopy.Name;
        }
        
        public override KOSDelegate Clone()
        {
            return new BuiltinDelegate(this);
        }

        public override bool IsDead()
        {
            return false; // builtins cannot be dead.
        }
        
        /*
        public override string ToString()
        {
            return string.Format("BuiltinDelegate(cpu={0}, Name={1},\n  {2})",
                                 Cpu, Name, base.ToString());
        }
        */
        
        public override void PushUnderArgs()
        {
            // do nothing.
        }
        
        public override Structure CallWithArgsPushedAlready()
        {
            int throwAway = OpcodeCall.StaticExecute(Cpu, true, Name, true);
            // throwAway will be -1 for cases where it's a builtin function.
            // and the return value will be left atop the stack by StaticExecute.
            return new KOSPassThruReturn();
        }
    }
}
