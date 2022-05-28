using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("VesselAltitude")]
    public class VesselAlt : Structure
    {
        private readonly SharedObjects shared;
        public VesselAlt(SharedObjects shared )
        {
            this.shared = shared;
            InitializeSuffixAlt();
        }

        private void InitializeSuffixAlt()
        {
            AddSuffix("APOAPSIS", new NoArgsSuffix<ScalarValue>(GetApoapsis));
            AddSuffix("PERIAPSIS", new NoArgsSuffix<ScalarValue>(GetPeriapsis));
            AddSuffix("RADAR", new NoArgsSuffix<ScalarValue>(GetRadar));
        }
        
        public ScalarValue GetApoapsis()
        {
            return shared.Vessel.orbit.ApA;            
        }
        
        public ScalarValue GetPeriapsis()
        {
            return shared.Vessel.orbit.PeA;
        }
        
        public ScalarValue GetRadar()
        {
            double seaAlt = shared.Vessel.altitude;

            // Note, this is -1 when ground is too far away, which is why it needs the "> 0" checks you see below
            // to fallback on sea level altitude when it isn't working.
            double groundAlt = shared.Vessel.heightFromTerrain;
            if (shared.Vessel.mainBody.ocean)
            {
                return Convert.ToDouble(groundAlt > 0 ? Math.Min(groundAlt, seaAlt) : seaAlt);
            }
            else
            {
                return Convert.ToDouble(groundAlt > 0 ? groundAlt : seaAlt);
            }
        }

        public override string ToString()
        {
            return string.Format("ALT: Apoapsis={0} Periapsis={1} Radar={2}", GetApoapsis(), GetPeriapsis(), GetRadar());
        }
    }
}