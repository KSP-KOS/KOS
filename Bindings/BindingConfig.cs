using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;

namespace kOS.Bindings
{
    [kOSBinding("ksp")]
    public class BindingConfig : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;
            _shared.BindingMgr.AddGetter("CONFIG", delegate(CPU cpu) { return Config.GetInstance(); });
        }
    }
}
