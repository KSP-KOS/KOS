using kOS.Safe.Communication;
using kOS.Safe.Encapsulation;

namespace kOS.Communication
{
    [Safe.Utilities.KOSNomenclature("Connection", KOSToCSharp = false)]
    public class HomeConnection : Connection
    {
        private SharedObjects shared;

        public HomeConnection(SharedObjects sharedObj)
        {
            shared = sharedObj;
        }

        public override bool Connected
        {
            get
            {
                return ConnectivityManager.HasConnectionToHome(shared.Vessel);
            }
        }

        public override double Delay
        {
            get
            {
                return ConnectivityManager.GetDelayToHome(shared.Vessel);
            }
        }

        protected override Structure Destination()
        {
            return new StringValue("HOME");
        }

        protected override BooleanValue SendMessage(Structure content)
        {
            throw new Safe.Exceptions.KOSException("kOS does not support sending messages to HOME (KSC in Stock)");
        }
    }
}