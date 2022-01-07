using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using kOS.Module;
using kOS.Safe.Utilities;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Stage")]
    public class StageValues : Structure
    {
        private readonly SharedObjects shared;
        private HashSet<global::Part> partHash = new HashSet<global::Part>();
        private PartSet partSet;
        private ListValue<ActiveResourceValue> resList;
        private Lexicon resLex;

        // Set by VesselTarget (from InvalidateParts)
        internal bool stale = true;

        /// <summary>
        /// Do not call! VesselTarget.StageValues uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal StageValues(SharedObjects shared)
        {
			// Do not try to construct VesselTarget here, it is called from VesselTarget's constructor!
			// Would need special logic if VesselTarget is needed here			

            this.shared = shared;
            partSet = new PartSet(partHash);

            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            // TODO: TEST IF THIS IS BROKEN WHEN CPU VESSEL != ACTIVE VESSEL
            // Some of these values come from StageManager, which is part of the UI and
            // only refers to the "active vessel", while others come from shared.Vessel and
            // thus refer to the CPU vessel.  Those aren't always the same thing:

            AddSuffix("NUMBER", new Suffix<ScalarValue>(() => StageManager.CurrentStage));
            AddSuffix("READY", new Suffix<BooleanValue>(() => shared.Vessel.isActiveVessel && StageManager.CanSeparate));
            AddSuffix("RESOURCES", new Suffix<ListValue<ActiveResourceValue>>(GetResourceManifest));
            AddSuffix("RESOURCESLEX", new Suffix<Lexicon>(GetResourceDictionary));
            AddSuffix(new string[] { "NEXTDECOUPLER", "NEXTSEPARATOR" }, new Suffix<Structure>(() => shared.VesselTarget.NextDecoupler ?? (Structure)StringValue.None));
            AddSuffix("DELTAV", new Suffix<DeltaVCalc>(() => new DeltaVCalc(shared, shared.Vessel.VesselDeltaV.GetStage(shared.Vessel.currentStage))));
        }

        private ListValue<ActiveResourceValue> GetResourceManifest()
        {
            if (resList != null) return resList;
            resList = new ListValue<ActiveResourceValue>();
            CreatePartSet();
            var defs = PartResourceLibrary.Instance.resourceDefinitions;
            foreach (var def in defs)
            {
                resList.Add(new ActiveResourceValue(def, shared, this, partSet));
            }

            return resList;
        }

        private Lexicon GetResourceDictionary()
        {
            if (resLex != null) return resLex;
            resLex = new Lexicon();
            CreatePartSet();
            var defs = PartResourceLibrary.Instance.resourceDefinitions;
            foreach (var def in defs)
            {
                resLex.Add(new StringValue(def.name), new ActiveResourceValue(def, shared, this, partSet));
            }

            return resLex;
        }

        public override ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
        {
            string fixedName;
            if (!Utils.IsResource(suffixName, out fixedName))
            {
                return base.GetSuffix(suffixName, failOkay);
            }

            double resourceAmount = GetResourceOfCurrentStage(fixedName);
            return new SuffixResult(ScalarValue.Create(resourceAmount));
        }

        private double GetResourceOfCurrentStage(string resourceName)
        {
            PartResourceDefinition resourceDef = PartResourceLibrary.Instance.resourceDefinitions[resourceName];

            double total = 0;
            double capacity = 0;

            if (resourceDef == null)
            {
                throw new KOSInvalidArgumentException("STAGE", resourceName, "The resource definition could not be found");
            }

            CreatePartSet();

            partSet.GetConnectedResourceTotals(resourceDef.id, out total, out capacity, true);

            return total;
        }

        public void CreatePartSet()
        {
            if (!stale)
                return;

            // We're creating the set every time because it doesn't pay attention to the various events
            // that would tell us that the old partset is no longer valid.
            partHash.Clear();

            var vesselTarget = shared.VesselTarget;
            var nextDecoupler = vesselTarget.NextDecoupler?.Part.inverseStage ?? -1;

            // Find all relevant parts that are to be separated by next decoupler/separator/dock
            foreach (var part in shared.Vessel.Parts)
            {
                // Engines only
                if (part.State != PartStates.ACTIVE)
                    continue;
                // All tanks accessible to this engine ...
                foreach (var crossPart in part.crossfeedPartSet.GetParts())
                {
                    // ... that are to be separated by next decoupler
                    if (vesselTarget[crossPart].DecoupledIn >= nextDecoupler)
                        partHash.Add(crossPart);
                }
            }
            partSet.RebuildInPlace();
            stale = false;

            SafeHouse.Logger.Log("StageValues: nd={0}, parts={1}", nextDecoupler, partHash.Count);
        }

        public override string ToString()
        {
            return string.Format("{0} Stage", base.ToString());
        }
    }
}
