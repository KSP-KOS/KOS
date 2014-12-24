using kOS.Persistence;
using kOS.Safe.Persistence;

namespace kOS.AddOns.RemoteTech2
{
    public class RemoteTechArchive : Archive
    {
        public bool CheckRange(Vessel vessel)
        {
            // return true if RemoteTech reports a connection to KSC, or if the vessel is currently 
            // in "PRELAUNCH" situation, which converts to int 2
            return vessel != null && (RemoteTechHook.Instance.HasConnectionToKSC(vessel.id) || (int)vessel.situation == 2);
        }
    }
}
