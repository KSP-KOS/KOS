using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed
{
    public class BodyAtmosphere : Structure
    {
        private readonly CelestialBody celestialBody;

        public BodyAtmosphere(CelestialBody celestialBody)
        {
            this.celestialBody = celestialBody;

            AddSuffix("BODY", new Suffix<string>(()=> celestialBody.bodyName));
            AddSuffix("EXISTS", new Suffix<bool>(()=> celestialBody.atmosphere));
            AddSuffix("OXYGEN", new Suffix<bool>(()=> celestialBody.atmosphere && celestialBody.atmosphereContainsOxygen));
            AddSuffix("SEALEVELPRESSURE", new Suffix<double>(()=> celestialBody.atmosphere ? celestialBody.atmospherePressureSeaLevel : 0));
            AddSuffix("HEIGHT", new Suffix<double>(()=> celestialBody.atmosphere ? celestialBody.atmosphereDepth : 0));

            AddSuffix("SCALE", new Suffix<double>(() => { throw new KOSAtmosphereDeprecationException("0.17.2","SCALE","<None>",string.Empty); }));
        }

        public override string ToString()
        {
            return "BODYATMOSPHERE(\"" + celestialBody.bodyName + "\")";
        }
    }
}