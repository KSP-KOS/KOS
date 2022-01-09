using System;
using System.Collections.Generic;
using kOS.Safe.Execution;
using kOS.Safe.Exceptions;
using kOS.Safe.Compilation;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// A UserDelegate that cannot actually ever executesany user code.
    /// Instead, when OpcodeCall() is executed for it, it will merely
    /// crash with an exception.  The idea is to give scripts something
    /// they can use to "unset" a callback hook.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("NoDelegate")]
    public class NoDelegate : UserDelegate
    {
        static private int sameHashForAllInstances =
            ("All EmptyDelegate Instances are Equal 0987654321.123456789").GetHashCode();

        public NoDelegate(ICpu cpu) : base(cpu, (cpu == null ? null : cpu.GetCurrentContext()), -1, false)
        {
        }

        private static NoDelegate instance;
        public static NoDelegate Instance
        {
            get { if (instance == null) instance = new NoDelegate(null); return instance; }
        }

        public override KOSDelegate Clone()
        {
            return new NoDelegate(Cpu);
        }
        
        public override void PushUnderArgs()
        {
            // force it to do nothing.
        }
        
        public override Structure CallWithArgsPushedAlready()
        {
            throw new KOSCannotCallException();
        }
                
        public override bool Equals(object o) 
        {
            // all DoNothingDelegates are the same:
            if (o.GetType() == typeof(NoDelegate))
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
