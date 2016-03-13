using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Communication
{
    [kOS.Safe.Utilities.KOSNomenclature("Connection")]
    public abstract class Connection<S> : Structure where S : SharedObjects
    {
        protected S shared;

        public abstract bool Connected { get; }
        public abstract double Delay { get; }

        public Connection(S shared)
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

