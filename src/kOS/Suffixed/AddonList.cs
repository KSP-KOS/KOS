using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;


namespace kOS.Suffixed
{
    [Safe.Utilities.KOSNomenclature("Addons")]
    public class AddonList : Structure
    {
        private readonly SharedObjects shared;

        public AddonList(SharedObjects shared)
        {
            this.shared = shared;

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            foreach (string id in shared.AddonManager.AllAddons.Keys)
            {
                AddSuffix(id, new Suffix<Addon>(() => shared.AddonManager.AllAddons[id]));
            }
            AddSuffix("AVAILABLE", new OneArgsSuffix<BooleanValue, StringValue>(CheckAddonAvailable));
            AddSuffix("HASADDON", new OneArgsSuffix<BooleanValue, StringValue>(CheckHasAddon));
        }

        public override string ToString()
        {
            return string.Format("{0} AddonList", base.ToString());
        }

        public BooleanValue CheckAddonAvailable(StringValue value)
        {
            if (!string.IsNullOrEmpty(value) && shared.AddonManager.AllAddons.ContainsKey(value))
            {
                return shared.AddonManager.AllAddons[value].Available();
            }
            return BooleanValue.False;
        }

        public BooleanValue CheckHasAddon(StringValue value)
        {
            if (!string.IsNullOrEmpty(value) && shared.AddonManager.AllAddons.ContainsKey(value))
            {
                return BooleanValue.True;
            }
            return BooleanValue.False;
        }
    }
}