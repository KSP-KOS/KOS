namespace kOS.Binding
{
    [KOSBinding("ksp")]
    public class RoverStats : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            //manager.AddGetter("HEADING", delegate(CPU cpu) { return cpu.Vessel.vesselName; });

            //manager.AddSetter("VESSELNAME", delegate(CPU cpu, object value) { cpu.Vessel.vesselName = value.ToString(); });
        }
    }
}
