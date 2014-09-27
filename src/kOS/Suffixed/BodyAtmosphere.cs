using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class BodyAtmosphere : Structure
    {
        private readonly CelestialBody b;

        public BodyAtmosphere(CelestialBody b)
        {
            this.b = b;

            AddSuffix("BODY", new Suffix<CelestialBody,string>(b, model => model.bodyName));
            AddSuffix("EXISTS", new Suffix<CelestialBody,bool>(b, model => model.atmosphere));
            AddSuffix("OXYGEN", new Suffix<CelestialBody,bool>(b, model => b.atmosphere && b.atmosphereContainsOxygen));
            AddSuffix("SCALE", new Suffix<CelestialBody,double>(b, model => b.atmosphere ? b.atmosphereScaleHeight : 0));
            AddSuffix("SEALEVELPRESSURE", new Suffix<CelestialBody,double>(b, model => b.atmosphere ? b.staticPressureASL : 0));
            AddSuffix("HEIGHT", new Suffix<CelestialBody,double>(b, model => b.atmosphere ? b.maxAtmosphereAltitude : 0));
        }

        public override string ToString()
        {
            return "BODYATMOSPHERE(\"" + b.bodyName + "\")";
        }
    }
}