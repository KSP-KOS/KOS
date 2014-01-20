namespace kOS.Binding
{
    [KOSBinding]
    public class TestBindings : Binding
    {
        public override void AddTo(BindingManager manager)
        {
#if DEBUG
            manager.AddGetter("TEST1", cpu => 4);
            manager.AddSetter("TEST1", (cpu, val) => cpu.StdOut(val.ToString()));
#endif                  
        }
    }
}