using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Suffixed
{
    public class OrbitInfo : SpecialValue
    {
        Orbit orbitRef;

        public OrbitInfo(Orbit init)
        {
            this.orbitRef = init;
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "APOAPSIS") return orbitRef.ApA;
            else if (suffixName == "PERIAPSIS") return orbitRef.PeA;
            else if (suffixName == "BODY") return orbitRef.referenceBody.name;

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            if (orbitRef != null)
            {
                return orbitRef.referenceBody.name;
            }

            return "";
        }
    }
}
