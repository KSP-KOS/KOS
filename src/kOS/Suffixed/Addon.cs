using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;

namespace kOS.Suffixed
{
    /// <summary>
    /// A generic addon description class for use in AddonList
    /// Addons must inherit from this one to implement functions
    /// </summary>
    public abstract class Addon : Structure
    {
        protected readonly string addonName;
        protected readonly SharedObjects shared;

        protected Addon(string name, SharedObjects shared)
        {
            addonName = name;
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
            return string.Format("{0} Addon, name = " + addonName, base.ToString());
        }
    }
}