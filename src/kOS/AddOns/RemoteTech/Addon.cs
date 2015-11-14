using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Safe.Encapsulation;

namespace kOS.AddOns.RemoteTech
{
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base ("RT", shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("DELAY", new OneArgsSuffix<double, VesselTarget>(RTGetDelay, "Get current Shortest Signal Delay for Vessel"));

            AddSuffix("KSCDELAY", new OneArgsSuffix<double, VesselTarget>(RTGetKSCDelay, "Get current KSC Signal Delay"));

            AddSuffix("HASCONNECTION", new OneArgsSuffix<bool, VesselTarget>(RTHasConnection, "True if ship has any connection"));

            AddSuffix("HASKSCCONNECTION", new OneArgsSuffix<bool, VesselTarget>(RTHasKSCConnection, "True if ship has connection to KSC"));

            AddSuffix("HASLOCALCONTROL", new OneArgsSuffix<bool, VesselTarget>(RTHasLocalControl, "True if ship has locacl control (i.e. a pilot in a command module)"));

            AddSuffix("GROUNDSTATIONS", new NoArgsSuffix<ListValue<string>>(RTGetGroundStations, "Get names of all ground stations"));

        }

        private static double RTGetDelay(VesselTarget tgtVessel)
        {
            double waitTotal = 0;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id) && tgtVessel.Vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = RemoteTechHook.Instance.GetShortestSignalDelay(tgtVessel.Vessel.id);
            }

            return waitTotal;
        }

        private static double RTGetKSCDelay(VesselTarget tgtVessel)
        {
            double waitTotal = 0;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id) && tgtVessel.Vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = RemoteTechHook.Instance.GetSignalDelayToKSC(tgtVessel.Vessel.id);
            }

            return waitTotal;
        }

        private static bool RTHasConnection(VesselTarget tgtVessel)
        {
            bool result = false;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = RemoteTechHook.Instance.HasAnyConnection(tgtVessel.Vessel.id);
            }

            return result;
        }

        private static bool RTHasLocalControl(VesselTarget tgtVessel)
        {
            bool result = false;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = RemoteTechHook.Instance.HasLocalControl(tgtVessel.Vessel.id);
            }

            return result;
        }

        private static bool RTHasKSCConnection(VesselTarget tgtVessel)
        {
            bool result = false;

            if (RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = RemoteTechHook.Instance.HasConnectionToKSC(tgtVessel.Vessel.id);
            }

            return result;
        }

        private static ListValue<string> RTGetGroundStations() {
            var groundStations = RemoteTechHook.Instance.GetGroundStations();

            return new ListValue<string>(groundStations);
        }

        public override bool Available()
        {
            return RemoteTechHook.IsAvailable();
        }

    }
}