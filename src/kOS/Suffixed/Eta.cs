using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using System;
using UnityEngine;
using Math = kOS.Safe.Utilities.Math;

namespace kOS.Suffixed
{
    public class Eta : Structure
    {
        private SharedObjects shared;
        public Eta(SharedObjects shared )
        {
            this.shared = shared;
            InitializeSuffixEta();
        }

        private void InitializeSuffixEta()
        {
            AddSuffix("APOAPSIS" , new NoArgsSuffix<double>(GetApoapsis));
            AddSuffix("PERIAPSIS" , new NoArgsSuffix<double>(GetPeriapsis));
            AddSuffix("TRANSITION" , new NoArgsSuffix<double>(GetTransition));
        }
        public double GetApoapsis()
        {
            return shared.Vessel.orbit.timeToAp;            
        }
        
        public double GetPeriapsis()
        {
            return shared.Vessel.orbit.timeToPe;
        }
        
        public double GetTransition()
        {
            return shared.Vessel.orbit.EndUT - Planetarium.GetUniversalTime();
        }
        
        public override string ToString()
        {
            return string.Format("ETA: Apoapsis={0} Periapsis={0} Transition={0}", GetApoapsis(), GetPeriapsis(), GetTransition());
        }
    }
}