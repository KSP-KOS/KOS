using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Atmosphere")]
    public class BodyAtmosphere : Structure
    {
        private readonly CelestialBody celestialBody;
        private readonly SharedObjects shared;

        public BodyAtmosphere(CelestialBody celestialBody, SharedObjects shared)
        {
            this.celestialBody = celestialBody;
            this.shared = shared;

            AddSuffix("BODY", new Suffix<StringValue>(()=> celestialBody.bodyName));
            AddSuffix("EXISTS", new Suffix<BooleanValue>(()=> celestialBody.atmosphere));
            AddSuffix("OXYGEN", new Suffix<BooleanValue>(()=> celestialBody.atmosphere && celestialBody.atmosphereContainsOxygen));
            AddSuffix("SEALEVELPRESSURE", new Suffix<ScalarValue>(()=> celestialBody.atmosphere ? celestialBody.atmospherePressureSeaLevel * ConstantValue.KpaToAtm : 0));
            AddSuffix("HEIGHT", new Suffix<ScalarValue>(()=> celestialBody.atmosphere ? celestialBody.atmosphereDepth : 0));
            AddSuffix("ALTITUDEPRESSURE", new OneArgsSuffix<ScalarValue, ScalarValue>((alt)=> celestialBody.GetPressure(alt) * ConstantValue.KpaToAtm));
            AddSuffix("MOLARMASS", new Suffix<ScalarValue>(() => celestialBody.atmosphereMolarMass));
            AddSuffix(new string[] { "ADIABATICINDEX", "ADBIDX" }, new Suffix<ScalarValue>(() => celestialBody.atmosphereAdiabaticIndex));
            AddSuffix(new string[] { "ALTITUDETEMPERATURE", "ALTTEMP" }, new OneArgsSuffix<ScalarValue, ScalarValue>((alt) => celestialBody.GetTemperature(alt)));
        }

        public override string ToString()
        {
            return "BODYATMOSPHERE(\"" + celestialBody.bodyName + "\")";
        }
    }
}