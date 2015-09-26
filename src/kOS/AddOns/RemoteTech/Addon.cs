using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using System.Linq;

namespace kOS.AddOns.RemoteTech
{
    [kOS.Safe.Utilities.KOSNomenclature("RTAddon")]
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base ("RT", shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("DELAY", new OneArgsSuffix<ScalarValue, VesselTarget>(RTGetDelay, "Get current Shortest Signal Delay for Vessel"));
            AddSuffix("KSCDELAY", new OneArgsSuffix<ScalarValue, VesselTarget>(RTGetKSCDelay, "Get current KSC Signal Delay"));
            AddSuffix("HASCONNECTION", new OneArgsSuffix<BooleanValue, VesselTarget>(RTHasConnection, "True if ship has any connection"));
            AddSuffix("HASKSCCONNECTION", new OneArgsSuffix<BooleanValue, VesselTarget>(RTHasKSCConnection, "True if ship has connection to KSC"));
            AddSuffix("ANTENNAHASCONNECTION", new OneArgsSuffix<BooleanValue, PartValue>(RTAntennaHasConnection, "True if antenna has any connection"));
            AddSuffix("HASLOCALCONTROL", new OneArgsSuffix<BooleanValue, VesselTarget>(RTHasLocalControl, "True if ship has locacl control (i.e. a pilot in a command module)"));
            AddSuffix("GROUNDSTATIONS", new NoArgsSuffix<ListValue>(RTGetGroundStations, "Get names of all ground stations"));

        }

        private static ScalarValue RTGetDelay(VesselTarget tgtVessel)
        {
            double waitTotal = 0;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id) && tgtVessel.Vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = RemoteTechHook.Instance.GetShortestSignalDelay(tgtVessel.Vessel.id);
            }

            return waitTotal;
        }

        private static ScalarValue RTGetKSCDelay(VesselTarget tgtVessel)
        {
            double waitTotal = 0;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id) && tgtVessel.Vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = RemoteTechHook.Instance.GetSignalDelayToKSC(tgtVessel.Vessel.id);
            }

            return waitTotal;
        }

        private static BooleanValue RTAntennaHasConnection(PartValue part)
        {
            bool result = false;

            if (RemoteTechHook.IsAvailable(part.Part.vessel.id))
            {
                result = RemoteTechHook.Instance.AntennaHasConnection(part.Part);
            }

            return result;
        }

        private static BooleanValue RTHasConnection(VesselTarget tgtVessel)
        {
            bool result = false;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = RemoteTechHook.Instance.HasAnyConnection(tgtVessel.Vessel.id);
            }

            return result;
        }

        private static BooleanValue RTHasLocalControl(VesselTarget tgtVessel)
        {
            bool result = false;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = RemoteTechHook.Instance.HasLocalControl(tgtVessel.Vessel.id);
            }

            return result;
        }

        private static BooleanValue RTHasKSCConnection(VesselTarget tgtVessel)
        {
            bool result = false;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = RemoteTechHook.Instance.HasConnectionToKSC(tgtVessel.Vessel.id);
            }

            return result;
        }

        private static ListValue RTGetGroundStations() {
            var groundStations = RemoteTechHook.Instance.GetGroundStations();

            return new ListValue(groundStations.Select((s) => new StringValue(s)).Cast<Structure>());
        }

        public override BooleanValue Available()
        {
            return RemoteTechHook.IsAvailable();
        }

    }
}