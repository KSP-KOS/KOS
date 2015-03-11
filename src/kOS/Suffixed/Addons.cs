using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class AddonList : Structure
    {
        private Addon KACAddon;
        private Addon RTAddon;
        private Addon AGXAddon;

        public AddonList()
        {
            KACAddon = new Addon ("KAC");
            RTAddon = new Addon ("RT");
            AGXAddon = new Addon ("AGX");

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("KAC", new Suffix<Addon>(() => KACAddon));
            AddSuffix("RT", new Suffix<Addon>(() => RTAddon));
            AddSuffix("AGX", new Suffix<Addon>(() => AGXAddon));
        }

        public override string ToString()
        {
            return string.Format("{0} AddonList", base.ToString());
        }
    }

    public class Addon : Structure
    {
        public string addonName = "";

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
                AddSuffix("GETDELAY", new OneArgsSuffix<double, VesselTarget>(RTGetDelay, "Get current Shortest Signal Delay for Vessel"));

                AddSuffix("GETKSCDELAY", new OneArgsSuffix<double, VesselTarget>(RTGetKSCDelay, "Get current KSC Signal Delay"));

                AddSuffix("HASCONNECTION", new OneArgsSuffix<bool, VesselTarget>(RTHasConnection, "True if ship has any connection"));

                AddSuffix("HASCONNECTION", new OneArgsSuffix<bool, VesselTarget>(RTHasConnection, "True if ship has connection to KSC"));
            }
        }

        private static double RTGetDelay(VesselTarget tgtVessel)
        {
            double waitTotal = 0;

            if (kOS.AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.Vessel.id) && tgtVessel.Vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = kOS.AddOns.RemoteTech.RemoteTechHook.Instance.GetShortestSignalDelay(tgtVessel.Vessel.id);
            }

            return waitTotal;
        }

        private static double RTGetKSCDelay(VesselTarget tgtVessel)
        {
            double waitTotal = 0;

            if (kOS.AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.Vessel.id) && tgtVessel.Vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = kOS.AddOns.RemoteTech.RemoteTechHook.Instance.GetSignalDelayToKSC(tgtVessel.Vessel.id);
            }

            return waitTotal;
        }

        private static bool RTHasConnection(VesselTarget tgtVessel)
        {
            bool result = false;

            if (kOS.AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = kOS.AddOns.RemoteTech.RemoteTechHook.Instance.HasAnyConnection(tgtVessel.Vessel.id);
            }

            return result;
        }

        private static bool RTHasKSCConnection(VesselTarget tgtVessel)
        {
            bool result = false;

            if (kOS.AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.Vessel.id))
            {
                result = kOS.AddOns.RemoteTech.RemoteTechHook.Instance.HasConnectionToKSC(tgtVessel.Vessel.id);
            }

            return result;
        }

        public Boolean Available()
        {
            if (addonName == "AGX")
            {
                return kOS.AddOns.ActionGroupsExtended.ActionGroupsExtendedAPI.Instance.Installed ();
            }

            if (addonName == "KAC")
            {
                return kOS.AddOns.KAC.KACWrapper.APIReady;
            }

            if (addonName == "RT")
            {
                return kOS.AddOns.RemoteTech.RemoteTechHook.IsAvailable ();
            }
            return false;
        }

        public override string ToString()
        {
            return string.Format("{0} Addon", base.ToString());
        }
    }
}
