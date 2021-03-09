using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Binding;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using kOS.Utilities;
using kOS.Module;
using kOS.Communication;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.AddOns.ArchiveMainframe
{
    [Binding("archive")]
    public class ArchiveMissionSettings : kOS.Binding.Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            var mainframeShared = shared as SharedMainframeObjects;
            if (mainframeShared != null) {
                shared.BindingMgr.AddGetter("SHIP", () => mainframeShared.ArchiveShip);
                shared.BindingMgr.AddGetter("CORE", () => mainframeShared.ArchiveCore);
                shared.BindingMgr.AddGetter("TIME", () => new TimeStamp(Planetarium.GetUniversalTime()));
            }
        }
    }
}
