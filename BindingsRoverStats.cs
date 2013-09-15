using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace kOS
{
    [kOSBinding("ksp")]
    public class BindingsRoverStats : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            //manager.AddGetter("HEADING", delegate(CPU cpu) { return cpu.Vessel.vesselName; });

            //manager.AddSetter("VESSELNAME", delegate(CPU cpu, object value) { cpu.Vessel.vesselName = value.ToString(); });
        }
    }
}
