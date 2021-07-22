using kOS.Safe;
using kOS.Safe.Binding;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Binding
{
    [AssemblyWalk(AttributeType = typeof(BindingAttribute), InherritedType = typeof(SafeBindingBase), StaticRegisterMethod = "RegisterMethod")]
    public class BindingManager : BaseBindingManager
    {
        private FlightControlManager flightControl;

        public BindingManager(SafeSharedObjects shared) : base(shared)
        {
        }

        protected override void LoadInstanceWithABinding(SafeBindingBase instanceWithABinding)
        {
            var manager = instanceWithABinding as FlightControlManager;
            if (manager != null)
            {
                flightControl = manager;
            }
        }

        public override void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (flightControl != null)
            {
                flightControl.ToggleFlyByWire(paramName, enabled);
            }
        }

        public override void SelectAutopilotMode(string autopilotMode)
        {
            if (flightControl != null)
            {
                flightControl.SelectAutopilotMode(autopilotMode);
            }
        }
    }
}