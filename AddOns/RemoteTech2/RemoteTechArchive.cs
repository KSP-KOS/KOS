using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Persistence;

namespace kOS.AddOns.RemoteTech2
{
    public class RemoteTechArchive : Archive
    {
        public override bool CheckRange(Vessel vessel)
        {
            if (vessel != null)
            {
                return RemoteTechHook.Instance.HasConnectionToKSC(vessel.id);
            }
            else
            {
                return false;
            }
        }
    }
}
