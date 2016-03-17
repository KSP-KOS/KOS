using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe;

namespace kOS.Safe.Binding
{
    public abstract class SafeBinding
    {
        public abstract void AddTo(SharedObjects shared);

        public virtual void Update()
        {
        }
    }
}