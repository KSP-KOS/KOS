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

            AddSuffix("BODY", new Suffix<CelestialBody,string>(celestialBody, model => model.bodyName));
            AddSuffix("EXISTS", new Suffix<CelestialBody,bool>(celestialBody, model => model.atmosphere));
            AddSuffix("OXYGEN", new Suffix<CelestialBody,bool>(celestialBody, model => celestialBody.atmosphere && celestialBody.atmosphereContainsOxygen));
            AddSuffix("SCALE", new Suffix<CelestialBody,double>(celestialBody, model => celestialBody.atmosphere ? celestialBody.atmosphereScaleHeight : 0));
            AddSuffix("SEALEVELPRESSURE", new Suffix<CelestialBody,double>(celestialBody, model => celestialBody.atmosphere ? celestialBody.staticPressureASL : 0));
            AddSuffix("HEIGHT", new Suffix<CelestialBody,double>(celestialBody, model => celestialBody.atmosphere ? celestialBody.maxAtmosphereAltitude : 0));
        }

        public override string ToString()
        {
            return "BODYATMOSPHERE(\"" + celestialBody.bodyName + "\")";
        }
    }
}