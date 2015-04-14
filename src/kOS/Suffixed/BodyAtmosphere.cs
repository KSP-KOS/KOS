using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

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
            AddSuffix("SCALE", new Suffix<double>(()=> celestialBody.atmosphere ? celestialBody.atmospherePressureCurve.maxTime : 0));
            AddSuffix("SEALEVELPRESSURE", new Suffix<double>(()=> celestialBody.atmosphere ? celestialBody.atmospherePressureSeaLevel : 0));
            AddSuffix("HEIGHT", new Suffix<double>(()=> celestialBody.atmosphere ? celestialBody.atmosphereDepth : 0));
        }

        public override string ToString()
        {
            return "BODYATMOSPHERE(\"" + celestialBody.bodyName + "\")";
        }
    }
}