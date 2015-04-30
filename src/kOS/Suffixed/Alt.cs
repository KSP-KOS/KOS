using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using System;
using UnityEngine;
using Math = kOS.Safe.Utilities.Math;

namespace kOS.Suffixed
{
    public class Alt : Structure
    {
        private SharedObjects shared;
        public Alt(SharedObjects shared )
        {
            this.shared = shared;
            InitializeSuffixAlt();
        }

        private void InitializeSuffixAlt()
        {
            AddSuffix("APOAPSIS", new NoArgsSuffix<double>(GetApoapais));
            AddSuffix("PERIAPSIS", new NoArgsSuffix<double>(GetPeriapsis));
            AddSuffix("RADAR", new NoArgsSuffix<double>(GetRadar));
        }
        
        public double GetApoapais()
        {
            return shared.Vessel.orbit.ApA;            
        }
        
        public double GetPeriapsis()
        {
            return shared.Vessel.orbit.PeA;
        }
        
        public double GetRadar()
        {
            return Convert.ToDouble(
                shared.Vessel.heightFromTerrain > 0 ?
                    Mathf.Min(shared.Vessel.heightFromTerrain, (float)shared.Vessel.altitude) :
                    (float)shared.Vessel.altitude);
        }

        public override string ToString()
        {
            return string.Format("ALT: Apoapsis={0} Periapsis={0} Radar={0}", GetApoapais(), GetPeriapsis(), GetRadar());
        }
    }
}