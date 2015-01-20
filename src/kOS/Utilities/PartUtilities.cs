using System;

namespace kOS.Utilities
{
    public static class PartUtilities
    {
        public static float CalculateCurrentMass(this Part part)
        {
            return part.HasPhysics() ? part.mass + part.GetResourceMass() : 0;
        }

        public static bool HasPhysics(this Part part)
        {
            switch (part.physicalSignificance)
            {
                case Part.PhysicalSignificance.FULL:
                    return true;
                case Part.PhysicalSignificance.NONE:
                    return false;
                default:
                    throw new NotImplementedException("Unknown Part physics type: " + part.physicalSignificance);
            }
        }

        public static float GetDryMass(this Part part)
        {
            return !part.HasPhysics() ? 0 : part.mass;
        }

        public static float GetWetMass(this Part part)
        {
            if (!part.HasPhysics()) return 0;

            float mass = part.mass;

            for (int index = 0; index < part.Resources.Count; ++index)
            {
                PartResource partResource = part.Resources[index];
                mass += (float)partResource.maxAmount * partResource.info.density;
            }

            return mass;
        }
    }
}