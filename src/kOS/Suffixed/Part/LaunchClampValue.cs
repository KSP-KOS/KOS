using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("LaunchClamp")]
    public class LaunchClampValue : DecouplerValue
    {
        /// <summary>
        /// Do not call! VesselTarget.ConstructPart uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal LaunchClampValue(SharedObjects shared, global::Part part, PartValue parent, DecouplerValue decoupler)
            : base(shared, part, parent, decoupler)
        {
        }

    }
}
