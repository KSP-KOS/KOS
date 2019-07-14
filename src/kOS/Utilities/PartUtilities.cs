using kOS.Safe.Encapsulation;
using UnityEngine;
using System;

namespace kOS.Utilities
{
    public static class PartUtilities
    {
        public static float CalculateCurrentMass(this Part part)
        {
            // rb mass is one physics tick behind.  Use part.GetPhysicslessChildMass() if the
            // delay becomes a significant problem, but this should be good 99% of the time.
            // Default to zero if the rigid body is not yet updated, or the part is physics-less
            return part.HasPhysics() && part.rb != null ? part.rb.mass : 0;
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
            // this will technically have an oportunity to return a negative wet mass
            // if the part is physics-less, but that option is intended for small part
            // to help with the physics calculation, not tanks of fuel.
            return part.CalculateCurrentMass() - part.resourceMass;
        }

        public static float GetWetMass(this Part part)
        {
            // See the note above regarding negative dry mass, the wet mass may net to
            // zero in the same case.  Again, highly unlikely.
            float mass = part.GetDryMass();

            for (int index = 0; index < part.Resources.Count; ++index)
            {
                PartResource partResource = part.Resources[index];
                mass += (float)partResource.maxAmount * partResource.info.density;
            }

            return mass;
        }

    }
}