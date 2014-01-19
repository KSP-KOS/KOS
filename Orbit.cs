using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
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
            if (suffixName == "PERIAPSIS") return orbitRef.PeA;
            if (suffixName == "BODY") return orbitRef.referenceBody.name;
            if (suffixName == "PERIOD") return orbitRef.period;
            if (suffixName == "INCLINATION") return orbitRef.inclination;
            if (suffixName == "ECCENTRICITY") return orbitRef.eccentricity;
            if (suffixName == "SEMIMAJORAXIS") return orbitRef.semiMajorAxis;
            if (suffixName == "SEMIMINORAXIS") return orbitRef.semiMinorAxis;
            if (suffixName == "TRANSITION") return orbitRef.patchEndTransition;

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
