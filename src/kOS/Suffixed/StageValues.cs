using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using kOS.Module;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Stage")]
    public class StageValues : Structure
    {
        private readonly SharedObjects shared;
        private HashSet<global::Part> partHash = new HashSet<global::Part>();
        private PartSet partSet;
        private double lastRefresh = 0;
        private ListValue<ActiveResourceValue> resList;
        private Lexicon resLex;

        public StageValues(SharedObjects shared)
        {
            this.shared = shared;
            partSet = new PartSet(partHash);

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NUMBER", new Suffix<ScalarValue>(() => StageManager.CurrentStage));
            AddSuffix("READY", new Suffix<BooleanValue>(() => shared.Vessel.isActiveVessel && StageManager.CanSeparate));
            AddSuffix("RESOURCES", new Suffix<ListValue<ActiveResourceValue>>(GetResourceManifest));
            AddSuffix("RESOURCESLEX", new Suffix<Lexicon>(GetResourceDictionary));
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

        public override ISuffixResult GetSuffix(string suffixName)
        {
            string fixedName;
            if (!Utils.IsResource(suffixName, out fixedName))
            {
                return base.GetSuffix(suffixName);
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

            return Math.Round(total, 2);
        }

        public void CreatePartSet()
        {
            double refresh = Planetarium.GetUniversalTime();
            if (lastRefresh >= refresh)
                return;
            lastRefresh = refresh;

            // The following replicates the logic in KSP.UI.Screens.ResourceDisplay.CreateResourceList
            // We're creating the set every time because it doesn't pay attention to the various events
            // that would tell us that the old partset is no longer valid.
            partHash.Clear();
            int vstgComp = shared.Vessel.currentStage - 2;
            var parts = shared.Vessel.Parts;
            global::Part part;
            for (int i = parts.Count - 1; i >= 0; --i)
            {
                part = parts[i];
                if (part.State == PartStates.ACTIVE)
                {
                    foreach (var crossPart in part.crossfeedPartSet.GetParts())
                    {
                        if (crossPart.inverseStage > vstgComp) partHash.Add(crossPart);
                    }
                }
            }
            partSet.RebuildInPlace();
        }

        public override string ToString()
        {
            return string.Format("{0} Stage", base.ToString());
        }
    }
}
