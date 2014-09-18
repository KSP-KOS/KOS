using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class BodyAtmosphere : Structure
    {
        public BodyAtmosphere(CelestialBody b)
        {
            BodyName = b.bodyName;

            Exists = b.atmosphere;

            Scale = Exists ? b.atmosphereScaleHeight : 0;
            Height = Exists ? b.maxAtmosphereAltitude : 0;
            Oxygen = Exists && b.atmosphereContainsOxygen;
            SeaLevelPressure = Exists ? b.staticPressureASL : 0;
        }

        protected double SeaLevelPressure { get; set; }
        protected string BodyName { get; set; }
        protected double Scale { get; set; }
        protected float Height { get; set; }
        protected bool Exists { get; set; }

        public bool Oxygen { get; set; }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "BODY":
                    return BodyName;
                case "EXISTS":
                    return Exists;
                case "HASOXYGEN":
                    return Oxygen;
                case "SCALE":
                    return Scale;
                case "SEALEVELPRESSURE":
                    return SeaLevelPressure;
                case "HEIGHT":
                    return Height;
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return "BODYATMOSPHERE(\"" + BodyName + "\")";
        }
    }
}