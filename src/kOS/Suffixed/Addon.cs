using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;

namespace kOS.Suffixed
{
    /// <summary>
    /// A generic addon description class for use in AddonList
    /// Addons must inherit from this one to implement functions
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Addon")]
    public abstract class Addon : Structure
    {
        protected readonly SharedObjects shared;
        
        protected Addon(SharedObjects shared)
        {
            this.shared = shared;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("AVAILABLE", new Suffix<BooleanValue>(Available));
        }

        public abstract BooleanValue Available ();
       
        public override string ToString()
        {
            return string.Format("Addon({0})", Safe.Utilities.KOSNomenclature.GetKOSName(this.GetType()));
        }
    }
}