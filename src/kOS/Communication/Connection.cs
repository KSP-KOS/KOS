using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Communication
{
    [kOS.Safe.Utilities.KOSNomenclature("Connection")]
    public abstract class Connection : Structure
    {
        public static int Infinity = -1;

        protected SharedObjects shared;

        public abstract bool Connected { get; }
        public abstract double Delay { get; }

        public Connection(SharedObjects shared)
        {
            this.shared = shared;

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ISCONNECTED", new Suffix<BooleanValue>(() => Connected));
            AddSuffix("DELAY", new Suffix<ScalarValue>(() => Delay));
            AddSuffix("SENDMESSAGE", new OneArgsSuffix<BooleanValue, Structure>(SendMessage));
            AddSuffix("DESTINATION", new NoArgsSuffix<Structure>(Destination));
        }

        protected abstract Structure Destination();
        protected abstract BooleanValue SendMessage(Structure content);
    }
}

