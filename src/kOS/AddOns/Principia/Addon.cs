using kOS.Safe.Encapsulation;

namespace kOS.AddOns.Principia
{
    [kOSAddon("Principia")]
    [kOS.Safe.Utilities.KOSNomenclature("PrincipiaAddon")]
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base (shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            //AddSuffix("DELAY", new OneArgsSuffix<double, VesselTarget>(RTGetDelay, "Get current Shortest Signal Delay for Vessel"));
        }

        public override BooleanValue Available()
        {
            return PrincipiaWrapper.Wrapped();
        }
    }
}