using TimeSpan = kOS.Value.TimeSpan;

namespace kOS.Binding
{
    [KOSBinding("ksp", "testTerm")]
    public class BindingsTest : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            manager.AddGetter("TEST:RADAR", cpu => new TimeSpan(cpu.SessionTime)); 
        }
    }
}

