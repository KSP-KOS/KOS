using kOS.Safe.Communication;
using kOS.Safe.Encapsulation;

namespace kOS.Communication
{
    [Safe.Utilities.KOSNomenclature("Connection", KOSToCSharp = false)]
    public class ControlConnection : Connection
    {
        private SharedObjects shared;

        public ControlConnection(SharedObjects sharedObj)
        {
            shared = sharedObj;
        }

        public override bool Connected
        {
            get
            {
                return ConnectivityManager.HasControl(shared.Vessel);
            }
        }

        public override double Delay
        {
            get
            {
                return ConnectivityManager.GetDelayToControl(shared.Vessel);
            }
        }

        protected override Structure Destination()
        {
            return new StringValue("CONTROL");
        }

        protected override BooleanValue SendMessage(Structure content)
        {
            throw new Safe.Exceptions.KOSException("kOS does not support sending messages to the control source");
        }
    }
}