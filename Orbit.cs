namespace kOS
{
    public class OrbitInfo : SpecialValue
    {
        readonly Orbit orbitRef;

        public OrbitInfo(Orbit init)
        {
            orbitRef = init;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "APOAPSIS":
                    return orbitRef.ApA;
                case "PERIAPSIS":
                    return orbitRef.PeA;
                case "BODY":
                    return orbitRef.referenceBody.name;
                case "PERIOD":
                    return orbitRef.period;
                case "INCLINATION":
                    return orbitRef.inclination;
                case "ECCENTRICITY":
                    return orbitRef.eccentricity;
                case "SEMIMAJORAXIS":
                    return orbitRef.semiMajorAxis;
                case "SEMIMINORAXIS":
                    return orbitRef.semiMinorAxis;
                case "TRANSITION":
                    return orbitRef.patchEndTransition;
            }

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
