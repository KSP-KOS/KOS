namespace kOS.Binding
{
    [KOSBinding("ksp")]
    public class RoverStats : IBinding
    {
        public  void BindTo(IBindingManager manager)
        {
            //manager.AddGetter("HEADING", delegate(CPU cpu) { return cpu.Vessel.vesselName; });

            //manager.AddSetter("VESSELNAME", delegate(CPU cpu, object value) { cpu.Vessel.vesselName = value.ToString(); });
        }

        public void Update(float time)
        {
        }
    }
}
