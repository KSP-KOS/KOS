using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Compilation.KS
{
    class Context
    {
        public LockCollection Locks;
        public TriggerCollection Triggers;
        public int LabelIndex;
        public int InstructionId;

        public Context()
        {
            Locks = new LockCollection();
            Triggers = new TriggerCollection();
            LabelIndex = 0;
            InstructionId = 0;
        }
    }
}
