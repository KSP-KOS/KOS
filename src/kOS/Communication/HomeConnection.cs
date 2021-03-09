using kOS.Safe.Communication;
using kOS.Safe.Encapsulation;
using kOS.Suffixed;
using kOS.AddOns.ArchiveMainframe;

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
            if (Mainframe.instance == null)
                throw new Safe.Exceptions.KOSException("KSC Mainframe not responding.");
            double sentAt = Planetarium.GetUniversalTime();
            VesselTarget sender = null;
            if (!(shared is SharedMainframeObjects))
                sender = VesselTarget.CreateOrGetExisting(shared);
            Mainframe.instance.messageQueue.Push(Message.Create(content, sentAt, sentAt, sender, "archive"));
            return true;
        }
    }
}