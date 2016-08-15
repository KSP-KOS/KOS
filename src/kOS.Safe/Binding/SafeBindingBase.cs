using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe;

namespace kOS.Safe.Binding
{
    public abstract class SafeBindingBase
    {
        public abstract void AddTo(SafeSharedObjects shared);

        public virtual void Update()
        {
        }
    }
}