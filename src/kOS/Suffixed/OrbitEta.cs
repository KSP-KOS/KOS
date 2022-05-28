using System;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Utilities;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("OrbitEta")]
    public class OrbitEta : Structure
    {
        private readonly Orbit orbit;
        private readonly SharedObjects shared;

        public OrbitEta(Orbit orbit, SharedObjects shared)
        {
            this.orbit = orbit;
            this.shared = shared;
            InitializeSuffixEta();
        }

        private void InitializeSuffixEta()
        {
            AddSuffix("APOAPSIS", new NoArgsSuffix<ScalarValue>(GetApoapsis));
            AddSuffix("PERIAPSIS", new NoArgsSuffix<ScalarValue>(GetPeriapsis));
            AddSuffix("TRANSITION", new NoArgsSuffix<ScalarValue>(GetEndTransition));
            AddSuffix("NEXTNODE", new NoArgsSuffix<ScalarValue>(GetNextNode));
        }

        public ScalarValue GetApoapsis()
        {
            if (!IsClosedOrbit()) return float.MaxValue;
            return ObTToETA(orbit.period / 2, Planetarium.GetUniversalTime());
        }

        public ScalarValue GetPeriapsis()
        {
            return ObTToETA(0, Planetarium.GetUniversalTime());
        }

        public ScalarValue GetNextNode()
        {
            var vessel = shared.Vessel;
            if (vessel.patchedConicSolver == null || vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return float.MaxValue;
            if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return float.MaxValue;
            return vessel.patchedConicSolver.maneuverNodes[0].UT - Planetarium.GetUniversalTime();
        }

        private BooleanValue IsClosedOrbit()
        {
            return orbit.eccentricity < 1;
        }

        // Given an orbit time measured in seconds from (some) periapsis, get the time
        // until the next occurrence of that orbit position after the orbit's start time
        // (or after the current time, if the start time is 0).
        private double ObTToETA(double obt, double now)
        {
            double period = orbit.period;
            if (!IsClosedOrbit()) {
                // Hyperbolic orbits never repeat, so the math is simpler.
                return obt - orbit.ObTAtEpoch + (orbit.epoch - now);
            } else {
                double start = orbit.StartUT;
                // Orbits created via createorbit() have StartUT set to 0.
                if (start == 0) start = now;
                double eta = obt - orbit.ObTAtEpoch + (orbit.epoch - start);
                eta %= period;
                if (eta < 0) eta += period;
                return eta + (start - now);
            }
        }

        public ScalarValue GetEndTransition()
        {
            if (!HasEndTransition()) return float.MaxValue;
            return orbit.EndUT - Planetarium.GetUniversalTime();
        }

        private BooleanValue HasEndTransition()
        {
            return IsRealTransition(orbit.patchEndTransition);
        }

        private static BooleanValue IsRealTransition(Orbit.PatchTransitionType transition)
        {
            return transition != Orbit.PatchTransitionType.INITIAL
                && transition != Orbit.PatchTransitionType.FINAL;
        }

        public override string ToString()
        {
            return string.Format("ETA: Apoapsis={0} Periapsis={1} Transition={2}",
                IsClosedOrbit() ? GetApoapsis().ToString() : "N/A",
                GetPeriapsis(),
                HasEndTransition() ? GetEndTransition().ToString() : "N/A");
        }
    }
}