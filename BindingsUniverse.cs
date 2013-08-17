using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS
{
    [kOSBinding("ksp")]
    public class BindingTimeWarp : Binding
    {
        public override void AddTo(BindingManager manager)
        {/*
            manager.AddGetter("WARP", delegate(CPU cpu) { return TimeWarp.fetch.current_rate_index; });
            manager.AddSetter("WARP", delegate(CPU cpu, object val)
            {
                int newRate;
                if (int.TryParse(val.ToString(), out newRate))
                {
                    TimeWarp.SetRate(newRate, false);
                }
            });*/
        }
    }
}
