using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Atmosphere")]
    public class BodyAtmosphere : Structure
    {
        private readonly CelestialBody celestialBody;

        public BodyAtmosphere(CelestialBody celestialBody)
        {
            this.celestialBody = celestialBody;

            AddSuffix("BODY", new Suffix<StringValue>(()=> celestialBody.bodyName));
            AddSuffix("EXISTS", new Suffix<BooleanValue>(()=> celestialBody.atmosphere));
            AddSuffix("OXYGEN", new Suffix<BooleanValue>(()=> celestialBody.atmosphere && celestialBody.atmosphereContainsOxygen));
            AddSuffix("SEALEVELPRESSURE", new Suffix<ScalarValue>(()=> celestialBody.atmosphere ? celestialBody.atmospherePressureSeaLevel : 0));
            AddSuffix("HEIGHT", new Suffix<ScalarValue>(()=> celestialBody.atmosphere ? celestialBody.atmosphereDepth : 0));

            AddSuffix("SCALE", new Suffix<ScalarValue>(() => { throw new KOSAtmosphereDeprecationException("0.17.2","SCALE","<None>",string.Empty); }));
        }

        public override string ToString()
        {
            return "BODYATMOSPHERE(\"" + celestialBody.bodyName + "\")";
        }
    }
}