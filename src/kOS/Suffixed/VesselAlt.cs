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
            return Convert.ToDouble(
                shared.Vessel.heightFromTerrain > 0 ?
                    Mathf.Min(shared.Vessel.heightFromTerrain, (float)shared.Vessel.altitude) :
                    (float)shared.Vessel.altitude);
        }

        public override string ToString()
        {
            return string.Format("ALT: Apoapsis={0} Periapsis={1} Radar={2}", GetApoapsis(), GetPeriapsis(), GetRadar());
        }
    }
}