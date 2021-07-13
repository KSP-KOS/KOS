using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed.PartModuleField;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("RCS")]
    public class RCSValue : PartValue
    {
        private readonly ModuleRCS module;

        private static FieldInfo deadbandField = typeof(ModuleRCS).GetField("EPSILON", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Accesses the private KSPField inside stock ModuleRCS that causes it to have a 5% deadband.  This
        /// appears to exist for the purpose of making stock SAS not waste monoprop maintaining
        /// direction.
        /// <para />
        /// Because it is private, changing it is at the users' own risk.
        /// <para />
        /// We will protect all access to it through checks to see if it's null (it will be null if the
        /// field stops existing in a future KSP release.)
        /// </summary>
        private float Deadband
        {
            get { return (float)(deadbandField == null ? 0.05f : deadbandField.GetValue(module)); }
            set { if (deadbandField != null) deadbandField.SetValue(module, value); }
        }

        /// <summary>
        /// Do not call! VesselTarget.ConstructPart uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal RCSValue(SharedObjects shared, global::Part part, PartValue parent, DecouplerValue decoupler, ModuleRCS module)
            : base(shared, part, parent, decoupler)
        {
            this.module = module;
            RegisterInitializer(RCSInitializeSuffixes);
        }
        private void RCSInitializeSuffixes()
        {
            // Tweakables
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => module.rcsEnabled, value => module.rcsEnabled = value));
            AddSuffix("YAWENABLED", new SetSuffix<BooleanValue>(() => module.enableYaw, value => module.enableYaw = value));
            AddSuffix("PITCHENABLED", new SetSuffix<BooleanValue>(() => module.enablePitch, value => module.enablePitch = value));
            AddSuffix("ROLLENABLED", new SetSuffix<BooleanValue>(() => module.enableRoll, value => module.enableRoll = value));
            AddSuffix("STARBOARDENABLED", new SetSuffix<BooleanValue>(() => module.enableX, value => module.enableX = value));
            AddSuffix("TOPENABLED", new SetSuffix<BooleanValue>(() => module.enableY, value => module.enableY = value));
            AddSuffix("FOREENABLED", new SetSuffix<BooleanValue>(() => module.enableZ, value => module.enableZ = value));
            AddSuffix("FOREBYTHROTTLE", new SetSuffix<BooleanValue>(() => module.useThrottle, value => module.useThrottle = value));
            AddSuffix("FULLTHRUST", new SetSuffix<BooleanValue>(() => module.fullThrust, value => module.fullThrust = value));
            AddSuffix("THRUSTLIMIT", new ClampSetSuffix<ScalarValue>(() => module.thrustPercentage, value => module.thrustPercentage = value, 0f, 100f, 0f, "thrust limit percentage for this rcs thruster"));
            AddSuffix("DEADBAND", new ClampSetSuffix<ScalarValue>(() => (float)Deadband, value => Deadband = value, 0f, 1f, 0f, "deadband for this rcs thruster"));
            // Performance metrics
            AddSuffix("AVAILABLETHRUST", new Suffix<ScalarValue>(() => module.GetThrust(useThrustLimit: true)));
            AddSuffix("AVAILABLETHRUSTAT",  new OneArgsSuffix<ScalarValue, ScalarValue>((ScalarValue atmPressure) => module.GetThrust(atmPressure, useThrustLimit: true)));
            AddSuffix("MAXTHRUST", new Suffix<ScalarValue>(() => module.GetThrust()));
            AddSuffix("MAXFUELFLOW", new Suffix<ScalarValue>(GetMaxFuelFlow));
            AddSuffix("MAXMASSFLOW", new Suffix<ScalarValue>(() => module.maxFuelFlow));
            AddSuffix("ISP", new Suffix<ScalarValue>(() => module.realISP));
            AddSuffix(new[] { "VISP", "VACUUMISP" }, new Suffix<ScalarValue>(() => module.GetIsp(0)));
            AddSuffix(new[] { "SLISP", "SEALEVELISP" }, new Suffix<ScalarValue>(() => module.GetIsp(1)));
            AddSuffix("FLAMEOUT", new Suffix<BooleanValue>(() => module.flameout));
            AddSuffix("ISPAT", new OneArgsSuffix<ScalarValue, ScalarValue>((ScalarValue atmPressure) => module.GetIsp(atmPressure)));
            AddSuffix("MAXTHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>((ScalarValue atmPressure) => module.GetThrust(atmPressure)));
            AddSuffix("THRUSTVECTORS", new Suffix<ListValue>(GetThrustVectors));
            AddSuffix("CONSUMEDRESOURCES", new Suffix<Lexicon>(GetConsumedResources, "A Lexicon of all resources consumed by this rcs thruster"));
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            var vessel = VesselTarget.CreateOrGetExisting(sharedObj);
            foreach (var part in parts)
            {
                foreach (var module in part.Modules)
                {
                    if (module is ModuleRCS)
                    {
                        toReturn.Add(vessel[part]);
                        // Only add each part once
                        break;
                    }
                }
            }
            return toReturn;
        }
        
        // Unfortunately mixtureDensity is unreliable so there isn't a quick way to get this.
        public ScalarValue GetMaxFuelFlow()
        {
            double mixtureDensity = 0;
            foreach (Propellant p in module.propellants)
            {
                mixtureDensity += p.resourceDef.density * p.ratio;
            }
            SafeHouse.Logger.Log(string.Format("Calculated density: {0} reported: {1} [{2}]", mixtureDensity, module.mixtureDensity, 1.0 / module.mixtureDensityRecip));

            return module.maxFuelFlow / mixtureDensity;
        }

        public ListValue GetThrustVectors()
        {
            var toReturn = new ListValue();
            foreach (Transform t in module.thrusterTransforms)
            {
                if (t.gameObject.activeInHierarchy)
                {
                    // RCS thrusts along up. Possibly useZAxis property means it thrusts along forward?
                    toReturn.Add(new Vector(t.up));
                }
            }
            return toReturn;
        }

        public Lexicon GetConsumedResources()
        {
            var resources = new Lexicon();
            foreach (Propellant p in module.propellants)
            {
                resources.Add(new StringValue(p.displayName), new ConsumedResourceValueRCS(p, Shared));
            }

            return resources;
        }
    }

    public static class ModuleRCSExtensions
    {
        /// <summary>
        /// Get engine thrust
        /// </summary>
        /// <param name="rcs">The rcs thruster (can be null - returns zero in that case)</param>
        /// <param name="atmPressure">
        ///   Atmospheric pressure (defaults to pressure at current location if omitted/null,
        ///   1.0 means Earth/Kerbin sea level, 0.0 is vacuum)</param>
        /// <returns>The thrust</returns>
        public static float GetThrust(this ModuleRCS rcs, double? atmPressure = null, bool useThrustLimit = false)
        {
            if (rcs == null)
                return 0f;
            float throttle = 1.0f;
            if (useThrustLimit)
                throttle = throttle * rcs.thrustPercentage / 100.0f;
            // thrust is fuel flow rate times isp times g
            // Assume min fuel flow is 0 as it's not exposed.
            return (float)(rcs.maxFuelFlow * throttle * GetIsp(rcs, atmPressure) * rcs.G);
        }
        /// <summary>
        /// Get engine ISP
        /// </summary>
        /// <param name="rcs">The rcs thruster (can be null - returns zero in that case)</param>
        /// <param name="atmPressure">
        ///   Atmospheric pressure (defaults to pressure at current location if omitted/null,
        ///   1.0 means Earth/Kerbin sea level, 0.0 is vacuum)</param>
        /// <returns></returns>
        public static float GetIsp(this ModuleRCS rcs, double? atmPressure = null)
        {
            return rcs == null ? 0f : rcs.atmosphereCurve.Evaluate(Mathf.Max(0f, (float)(atmPressure ?? rcs.part.staticPressureAtm)));
        }
    }
}
