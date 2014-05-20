using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Binding
{
    public class Binding
    {
        protected SharedObjects _shared;

        public virtual void AddTo(SharedObjects shared) { }
        public virtual void Update() { }
    }
}
