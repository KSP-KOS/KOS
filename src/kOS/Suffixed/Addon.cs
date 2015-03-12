using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;

namespace kOS.Suffixed
{
    public class Addon : Structure
    {
        private readonly string addonName;

        public Addon(string name)
        {
            addonName = name;

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("AVAILABLE", new Suffix<Boolean>(Available));

            if (addonName == "RT")
            {
                AddSuffix("DELAY", new OneArgsSuffix<double, VesselTarget>(RTGetDelay, "Get current Shortest Signal Delay for Vessel"));

                AddSuffix("KSCDELAY", new OneArgsSuffix<double, VesselTarget>(RTGetKSCDelay, "Get current KSC Signal Delay"));

                AddSuffix("HASCONNECTION", new OneArgsSuffix<bool, VesselTarget>(RTHasConnection, "True if ship has any connection"));

                AddSuffix("HASKSCCONNECTION", new OneArgsSuffix<bool, VesselTarget>(RTHasKSCConnection, "True if ship has connection to KSC"));
            }
        }

        private static double RTGetDelay(VesselTarget tgtVessel)
        {
            double waitTotal = 0;

            if (AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.Vessel.id) && tgtVessel.Vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = AddOns.RemoteTech.RemoteTechHook.Instance.GetShortestSignalDelay(tgtVessel.Vessel.id);
            }

            return waitTotal;
        }

        private static double RTGetKSCDelay(VesselTarget tgtVessel)
        {
            double waitTotal = 0;

            if (AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.Vessel.id) && tgtVessel.Vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = AddOns.RemoteTech.RemoteTechHook.Instance.GetSignalDelayToKSC(tgtVessel.Vessel.id);
            }

            return waitTotal;
        }

        private static bool RTHasConnection(VesselTarget tgtVessel)
        {
            bool result = false;

            if (AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = AddOns.RemoteTech.RemoteTechHook.Instance.HasAnyConnection(tgtVessel.Vessel.id);
            }

            return result;
        }

        private static bool RTHasKSCConnection(VesselTarget tgtVessel)
        {
            bool result = false;

            if (AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = AddOns.RemoteTech.RemoteTechHook.Instance.HasConnectionToKSC(tgtVessel.Vessel.id);
            }

            return result;
        }

        public Boolean Available()
        {
            if (addonName == "AGX")
            {
                return AddOns.ActionGroupsExtended.ActionGroupsExtendedAPI.Instance.Installed();
            }

            if (addonName == "KAC")
            {
                return AddOns.KerbalAlarmClock.KACWrapper.APIReady;
            }

            if (addonName == "RT")
            {
                return AddOns.RemoteTech.RemoteTechHook.IsAvailable();
            }
            return false;
        }

        public override string ToString()
        {
            return string.Format("{0} Addon", base.ToString());
        }
    }
}