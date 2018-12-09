using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Safe.Exceptions
{
    public class KOSModifyReadonly : KOSException
    {
        public KOSModifyReadonly() : base("The collection is read-only. Suffixes like :CLEAR or :ADD are not allowed.")
        {
        }
    }
}
