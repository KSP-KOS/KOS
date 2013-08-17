using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    public class kOSIDModule : PartModule
    {
        [KSPField(isPersistant=false, guiName = "kOS Part ID", guiActive = true)]
        public string ID;

        public kOSIDModule(string initID)
        {
            this.ID = initID;
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

        }

        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("************ ONLOAD");
            ID = node.GetValue("kosID");
        }

        public override void OnSave(ConfigNode node)
        {
            Debug.Log("************ ONSAVE");
            node.SetValue("kosID", ID);
        }
    }
}
