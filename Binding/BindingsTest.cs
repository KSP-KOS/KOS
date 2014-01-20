using TimeSpan = kOS.Value.TimeSpan;

namespace kOS.Binding
{
    [kOSBinding("ksp", "testTerm")]
    public class BindingsTest : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            manager.AddGetter("TEST:RADAR", delegate(CPU cpu)
            {
                return new TimeSpan(cpu.SessionTime);
            }); 
        }

        public override void Update(float time)
        {
            base.Update(time);
        }
    }
}

