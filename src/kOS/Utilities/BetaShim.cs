using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Utilities
{
    public static class BetaShim
    {
        public static uint uid(this Part part)
        {
            return (uint)part.GetHashCode();
        }

        public static string ConstructID(this Part part)
        {
            return part.partInfo.name + part.gameObject.GetInstanceID();
        }
    }
}
