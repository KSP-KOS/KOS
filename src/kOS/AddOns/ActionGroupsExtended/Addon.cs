using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using System;

namespace kOS.AddOns.ActionGroupsExtended
{
    public class Addon : kOS.Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base ("AGX", shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            //AddSuffix("DELAY", new OneArgsSuffix<double, VesselTarget>(RTGetDelay, "Get current Shortest Signal Delay for Vessel"));
        }

        public override bool Available()
        {
            return ActionGroupsExtendedAPI.Instance.Installed ();
        }
    }
}