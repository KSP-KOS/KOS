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
        }
        private void InitializeSuffixes()
        {
            AddSuffix("AVAILABLE", new Suffix<Boolean>(Available));

            if (addonName == "RT")
            {
                AddSuffix("GETDELAY", new OneArgsSuffix<double, Vessel>(RTGetDelay, "Get current Shortest Signal Delay for Vessel"));

                AddSuffix("GETKSCDELAY", new OneArgsSuffix<double, Vessel>(RTGetKSCDelay, "Get current KSC Signal Delay"));

                AddSuffix("HASCONNECTION", new OneArgsSuffix<bool, Vessel>(RTHasConnection, "True if ship has any connection"));

                AddSuffix("HASCONNECTION", new OneArgsSuffix<bool, Vessel>(RTHasConnection, "True if ship has connection to KSC"));
            }
        }

        private static double RTGetDelay(Vessel tgtVessel)
        {
            double waitTotal = 0;

            if (kOS.AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.id) && tgtVessel.GetVesselCrew().Count == 0)
            {
                waitTotal = kOS.AddOns.RemoteTech.RemoteTechHook.Instance.GetShortestSignalDelay(tgtVessel.id);
            }

            return waitTotal;
        }

        private static double RTGetKSCDelay(Vessel tgtVessel)
        {
            double waitTotal = 0;

            if (kOS.AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.id) && tgtVessel.GetVesselCrew().Count == 0)
            {
                waitTotal = kOS.AddOns.RemoteTech.RemoteTechHook.Instance.GetSignalDelayToKSC(tgtVessel.id);
            }

            return waitTotal;
        }

        private static bool RTHasConnection(Vessel tgtVessel)
        {
            bool result = false;

            if (kOS.AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.id) && tgtVessel.GetVesselCrew().Count == 0)
            {
                result = kOS.AddOns.RemoteTech.RemoteTechHook.Instance.HasAnyConnection(tgtVessel.id);
            }

            return result;
        }

        private static bool RTHasKSCConnection(Vessel tgtVessel)
        {
            bool result = false;

            if (kOS.AddOns.RemoteTech.RemoteTechHook.IsAvailable(tgtVessel.id) && tgtVessel.GetVesselCrew().Count == 0)
            {
                result = kOS.AddOns.RemoteTech.RemoteTechHook.Instance.HasConnectionToKSC(tgtVessel.id);
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
