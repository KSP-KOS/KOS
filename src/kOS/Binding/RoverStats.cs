using kOS.Safe.Binding;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class RoverStats : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            //manager.AddGetter("HEADING", delegate(CPU cpu) { return cpu.Vessel.vesselName; });

            //manager.AddSetter("VESSELNAME", delegate(CPU cpu, object value) { cpu.Vessel.vesselName = value.ToString(); });
        }
    }
}
