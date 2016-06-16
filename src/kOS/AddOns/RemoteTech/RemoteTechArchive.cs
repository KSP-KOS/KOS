using kOS.Safe.Persistence;

namespace kOS.AddOns.RemoteTech
{
    [kOS.Safe.Utilities.KOSNomenclature("RTArchive")]
    public class RemoteTechArchive : Archive
    {
        public RemoteTechArchive(string archiveFolder) : base(archiveFolder)
        {

        }

        public bool CheckRange(Vessel vessel)
        {
            if (vessel == null)
            {
                return false;
            }
            // return true if RemoteTech reports a connection to KSC, or if the vessel is currently in "PRELAUNCH" situation
            return RemoteTechHook.Instance.HasConnectionToKSC(vessel.id) || vessel.situation == Vessel.Situations.PRELAUNCH;
        }
    }
}
