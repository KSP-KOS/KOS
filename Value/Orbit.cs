namespace kOS.Value
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
                case "PATCHES":
                    return BuildPatchList();
            }

            return base.GetSuffix(suffixName);
        }

        private object BuildPatchList()
        {
            var list = new ListValue();
            var orbit = orbitRef;
            while (orbit.nextPatch != null)
            {
                list.Add(new OrbitInfo(orbit));
            }
            return list;
        }

        public override string ToString()
        {
            return orbitRef != null ? orbitRef.referenceBody.name : "";
        }
    }
}
