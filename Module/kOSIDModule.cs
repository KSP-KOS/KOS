using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace kOS.Module
{
    public class kOSIDModule : PartModule
    {
        [KSPField(isPersistant=true, guiName = "kOS Part ID", guiActive = true)]
        public string ID;
    }
}
