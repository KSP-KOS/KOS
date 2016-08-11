using System;
using System.Collections.Generic;
using kOS.Safe.Execution;
using kOS.Safe.Compilation;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// A UserDelegate that actually never executes any user code.
    /// Instead, when OpcodeCall() is executed, it will merely
    /// consume all the args down to the arg bottom mark, and
    /// return a zero on the stack, without actually calling any
    /// user code at all.  This is meant as a way for user code
    /// to have a "null delegate" without really having one.
    /// 
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("EmptyDelegate")]
    public class DoNothingDelegate : UserDelegate
    {
        static private int sameHashForAllInstances =
            ("All EmptyDelegate Instances are Equal 0987654321.123456789").GetHashCode();

        public DoNothingDelegate(ICpu cpu) : base(cpu, cpu.GetCurrentContext(), -1, false)
        {
        }

        public override KOSDelegate Clone()
        {
            return new DoNothingDelegate(Cpu);
        }
        
        public override void PushUnderArgs()
        {
            // force it to do nothing.
        }
        
        public override Structure CallWithArgsPushedAlready()
        {
            Console.WriteLine("eraseme: EmptyDelegate.Call() starting.");
            Console.WriteLine(Cpu.DumpStack()); // eraseme

            // Throw away all args on the stack this might have been called with:
            while (Cpu.GetStackSize() > 0 && !(Cpu.PopStack() is KOSArgMarkerType))
            {
                // do nothing
            }
            
            return new ScalarIntValue(0); // dummy return of zero.
        }
        
        public override string ToString()
        {
            return string.Format("DoNothingDelegate");
        }
                
        public override bool Equals(object o) 
        {
            // all DoNothingDelegates are the same:
            if (o.GetType() == typeof(DoNothingDelegate))
                return true;
            else
                return false;
        }
        
        public override int GetHashCode()
        {
            return sameHashForAllInstances;
        }
    }
}
