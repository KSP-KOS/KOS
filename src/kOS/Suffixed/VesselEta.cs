using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    public class VesselEta : Structure
    {
        private readonly SharedObjects shared;
        public VesselEta(SharedObjects shared )
        {
            this.shared = shared;
            InitializeSuffixEta();
        }

        private void InitializeSuffixEta()
        {
            AddSuffix("APOAPSIS" , new NoArgsSuffix<ScalarDoubleValue>(GetApoapsis));
            AddSuffix("PERIAPSIS" , new NoArgsSuffix<ScalarDoubleValue>(GetPeriapsis));
            AddSuffix("TRANSITION" , new NoArgsSuffix<ScalarDoubleValue>(GetTransition));
        }
        public ScalarDoubleValue GetApoapsis()
        {
            return shared.Vessel.orbit.timeToAp;            
        }
        
        public ScalarDoubleValue GetPeriapsis()
        {
            return shared.Vessel.orbit.timeToPe;
        }
        
        public ScalarDoubleValue GetTransition()
        {
            return shared.Vessel.orbit.EndUT - Planetarium.GetUniversalTime();
        }
        
        public override string ToString()
        {
            return string.Format("ETA: Apoapsis={0} Periapsis={1} Transition={2}", GetApoapsis(), GetPeriapsis(), GetTransition());
        }
    }
}