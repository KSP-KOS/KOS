using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;


namespace kOS.Suffixed
{
    public class AddonList : Structure
    {
        private readonly SharedObjects shared;
        private readonly kOS.AddOns.KerbalAlarmClock.Addon kacAddon;
        private readonly kOS.AddOns.RemoteTech.Addon rtAddon;
        private readonly kOS.AddOns.ActionGroupsExtended.Addon agxAddon;

        public AddonList(SharedObjects shared)
        {
            this.shared = shared;
            kacAddon = new kOS.AddOns.KerbalAlarmClock.Addon(shared);
            rtAddon = new kOS.AddOns.RemoteTech.Addon(shared);
            agxAddon = new kOS.AddOns.ActionGroupsExtended.Addon(shared);

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("KAC", new Suffix<Addon>(() => kacAddon));
            AddSuffix("RT", new Suffix<Addon>(() => rtAddon));
            AddSuffix("AGX", new Suffix<Addon>(() => agxAddon));
        }

        public override string ToString()
        {
            return string.Format("{0} AddonList", base.ToString());
        }
    }
}