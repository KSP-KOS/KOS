using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using UnityEngine;

namespace kOS.Suffixed
{
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
            AddSuffix("APOAPSIS", new NoArgsSuffix<ScalarDoubleValue>(GetApoapsis));
            AddSuffix("PERIAPSIS", new NoArgsSuffix<ScalarDoubleValue>(GetPeriapsis));
            AddSuffix("RADAR", new NoArgsSuffix<ScalarDoubleValue>(GetRadar));
        }
        
        public ScalarDoubleValue GetApoapsis()
        {
            return shared.Vessel.orbit.ApA;            
        }
        
        public ScalarDoubleValue GetPeriapsis()
        {
            return shared.Vessel.orbit.PeA;
        }
        
        public ScalarDoubleValue GetRadar()
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