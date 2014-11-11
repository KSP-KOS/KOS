using kOS.Persistence;

namespace kOS.AddOns.RemoteTech2
{
    public class RemoteTechArchive : Archive
    {
        public override bool CheckRange(Vessel vessel)
        {
            return vessel != null && RemoteTechHook.Instance.HasConnectionToKSC(vessel.id);
        }
    }
}
