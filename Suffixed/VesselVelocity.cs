using kOS.Utilities;

namespace kOS.Suffixed
{
    public class VesselVelocity : SpecialValue
    {
        private readonly Vector orbitVelocity;
        private readonly Vector surfaceVelocity;
        private readonly float velocityHeading;

        public VesselVelocity(Vessel v)
        {
            orbitVelocity = new Vector(v.obt_velocity);
            surfaceVelocity = new Vector(v.srf_velocity);
            velocityHeading = VesselUtils.GetVelocityHeading(v);
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "ORBIT":
                    return orbitVelocity;
                case "SURFACE":
                    return surfaceVelocity;
                case "SURFACEHEADING":
                    //TODO: I created this one for debugging purposes only, at some point I'll make a function to transform vectors to headings in a more eloquent way
                    return velocityHeading;
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return orbitVelocity.ToString();
        }
    }
}