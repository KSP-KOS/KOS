namespace kOS.Binding
{
    [KOSBinding]
    public class TestBindings : IBinding
    {
        public void BindTo(IBindingManager manager)
        {
#if DEBUG
            manager.AddGetter("TEST1", cpu => 4);
            manager.AddSetter("TEST1", (cpu, val) => cpu.StdOut(val.ToString()));
#endif
        }

        public void Update(float time)
        {
        }
    }
}