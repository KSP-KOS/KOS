namespace kOS.Suffixed
{
    public class OrbitInfo : SpecialValue
    {
        private readonly Orbit orbitRef;
        private readonly Vessel vesselRef;

        public OrbitInfo(Orbit init, Vessel vesselRef)
        {
            orbitRef = init;
            this.vesselRef = vesselRef;
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
                    return new BodyTarget(orbitRef.referenceBody, vesselRef);
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
            var list = new MixedListValue();
            var orbit = orbitRef;
            while (orbit.nextPatch != null)
            {
                list.Add(new OrbitInfo(orbit, vesselRef));
            }
            return list;
        }

        public override string ToString()
        {
            return orbitRef != null ? orbitRef.referenceBody.name : "";
        }
    }
}
