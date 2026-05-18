using kOS.Safe.Execution;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// A UserDelegate that cannot actually ever execute any user code.
    /// The idea is to give scripts something they can use to "unset" a callback hook.
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

        public override Structure CallPassingArgs(params Structure[] args)
        {
            return new KOSPassThruReturn();
        }
        
        public override string ToString()
        {
            return string.Format("DoNothingDelegate");
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
