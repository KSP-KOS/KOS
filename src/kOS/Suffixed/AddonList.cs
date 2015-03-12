using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class AddonList : Structure
    {
        private Addon kacAddon;
        private Addon rtAddon;
        private Addon agxAddon;

        public AddonList()
        {
            kacAddon = new Addon ("KAC");
            rtAddon = new Addon ("RT");
            agxAddon = new Addon ("AGX");

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
