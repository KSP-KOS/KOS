using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("Separator")]
    public class SeparatorValue : DecouplerValue
    {
        private readonly ModuleDecouplerBase module;

        /// <summary>
        /// Do not call! VesselTarget.ConstructPart uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal SeparatorValue(SharedObjects shared, global::Part part, PartValue parent, DecouplerValue separator, ModuleDecouplerBase module)
            : base(shared, part, parent, separator)
        {
            this.module = module;
            RegisterInitializer(DockingInitializeSuffixes);
        }

        private void DockingInitializeSuffixes()
        {
            AddSuffix("EJECTIONFORCE", new Suffix<ScalarValue>(() => module.ejectionForce));
            AddSuffix("ISDECOUPLED", new Suffix<BooleanValue>(() => module.isDecoupled));
            AddSuffix("STAGED", new Suffix<BooleanValue>(() => module.staged));
        }
    }
}