using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.InterProcessor
{
    public class InterProcCommand
    {
        public virtual void Execute(SharedObjects shared) { }
    }
}
