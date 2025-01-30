using kOS.Module;
using kOS.Safe.Encapsulation;
using kOS.Suffixed.Part;
using kOS.Suffixed.PartModuleField;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace kOS.Suffixed
{
    partial class VesselTarget
    {
        public StageValues StageValues { get; private set; }

        //TODO: share these between all CPUs (or maybe have one VesselTarget instance?)
        //TODO: create single list of parts and _slices_ for `children`
        //..... [root, child1, child2, ..., part1-1, part1-2, ..., part2-1, ... heap ;)
        private PartValue rootPart;
        private DecouplerValue nextDecoupler;
        private List<PartValue> allParts;
        private List<DockingPortValue> dockingPorts;
        private List<DecouplerValue> decouplers;
        private Dictionary<global::Part, PartValue> partCache;

        private void InvalidateParts()
        {
            StageValues.stale = true;

            rootPart = null;
            nextDecoupler = null;
            allParts = null;
            dockingPorts = null;
            decouplers = null;
            partCache = null;
        }
        public PartValue Root
        {
            get
            {
                if (allParts == null)
                    ConstructParts();
                return rootPart;
            }
        }
        public DecouplerValue NextDecoupler
        {
            get
            {
                if (allParts == null)
                    ConstructParts();
                return nextDecoupler;
            }
        }
        public List<PartValue> Parts
        {
            get
            {
                if (allParts == null)
                    ConstructParts();
                return allParts;
            }
        }
        public List<DockingPortValue> DockingPorts
        {
            get
            {
                if (dockingPorts == null)
                    ConstructParts();
                return dockingPorts;
            }
        }
        public List<DecouplerValue> Decouplers
        {
            get
            {
                if (decouplers == null)
                    ConstructParts();
                return decouplers;
            }
        }
        public PartValue this[global::Part part]
        {
            get
            {
                if (allParts == null)
                    ConstructParts();
                if (partCache.ContainsKey(part)) {
                    return partCache[part];
                }
                // returning null is very dangerous and must be guarded in other kOS code,
                // because kOS itself has no ability to return a null value
                return null;
            }
        }
        private void ConstructParts()
        {
            rootPart = null;
            nextDecoupler = null;
            allParts = new List<PartValue>();
            dockingPorts = new List<DockingPortValue>();
            decouplers = new List<DecouplerValue>();
            partCache = new Dictionary<global::Part, PartValue>();

            ConstructPart(Vessel.rootPart, null, null);
        }
        private void ConstructPart(global::Part part, PartValue parent, DecouplerValue decoupler)
        {
            if (part.State == PartStates.DEAD || part.transform == null)
                return;

            // Modules can be in any order, so to enforce some sort of priority for parts which are multiple types,
            // gather all potential modules and then select from those valid.
            IEngineStatus engine = null;
            ModuleRCS rcs = null;
            DecouplerValue separator = null;
            ModuleEnviroSensor sensor = null;

            foreach (var module in part.Modules)
            {
                if (module is IEngineStatus)
                {
                    engine = module as IEngineStatus;
                }
                else if (module is ModuleRCS)
                {
                    rcs = module as ModuleRCS;
                }
                else if (module is IStageSeparator)
                {
                    var dock = module as ModuleDockingNode;
                    if (dock != null)
                    {
                        var port = new DockingPortValue(Shared, part, parent, decoupler, dock);
                        separator = port;
                        dockingPorts.Add(port);
                        //if (!module.StagingEnabled())
                        //    continue;
                        decoupler = port;
                        decouplers.Add(decoupler);
                    }
                    // ignore anything with staging disabled and continue the search
                    // this can e.g. be heat shield or some sensor with integrated decoupler
                    else
                    {
                        DecouplerValue port;
                        if (module is LaunchClamp)
                            port = new LaunchClampValue(Shared, part, parent, decoupler);
                        else if (module is ModuleDecouple || module is ModuleAnchoredDecoupler)
                            port = new SeparatorValue(Shared, part, parent, decoupler, module as ModuleDecouplerBase);
                        else // ModuleServiceModule ?
                            continue; // rather continue the search
                        separator = port;
                        //if (!module.StagingEnabled())
                        //    continue;
                        decoupler = port;
                        decouplers.Add(decoupler);
                    }
                    // ignore leftover decouplers
                    if (decoupler == null || decoupler.Part.inverseStage >= StageManager.CurrentStage)
                        continue;
                    // check if we just created closer decoupler (see StageValues.CreatePartSet)
                    if (nextDecoupler == null || decoupler.Part.inverseStage > nextDecoupler.Part.inverseStage)
                        nextDecoupler = decoupler;
                }
                else if (module is ModuleEnviroSensor)
                {
                    sensor = module as ModuleEnviroSensor;
                }
            }

            // Select part value in priority order
            PartValue self;
            if (engine != null)
                self = new EngineValue(Shared, part, parent, decoupler);
            else if (rcs != null)
                self = new RCSValue(Shared, part, parent, decoupler, rcs);
            else if (separator != null)
                self = separator;
            else if (sensor != null)
                self = new SensorValue(Shared, part, parent, decoupler, sensor);
            else
                self = new PartValue(Shared, part, parent, decoupler);

            if (rootPart == null)
                rootPart = self;
            partCache[part] = self;
            allParts.Add(self);
            foreach (var child in part.children)
                ConstructPart(child, self, decoupler);
        }

        private ListValue GetPartsDubbed(StringValue searchTerm)
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetPartsDubbed(searchTerm);
        }

        private ListValue GetPartsDubbedPattern(StringValue searchPattern)
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetPartsDubbedPattern(searchPattern);
        }

        private ListValue GetPartsNamed(StringValue partName)
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetPartsNamed(partName);
        }

        private ListValue GetPartsNamedPattern(StringValue partNamePattern)
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetPartsNamedPattern(partNamePattern);
        }

        private ListValue GetPartsTitled(StringValue partTitle)
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetPartsTitled(partTitle);
        }

        private ListValue GetPartsTitledPattern(StringValue partTitlePattern)
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetPartsTitledPattern(partTitlePattern);
        }

        private ListValue GetPartsTagged(StringValue tagName)
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetPartsTagged(tagName);
        }

        private ListValue GetPartsTaggedPattern(StringValue tagPattern)
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetPartsTaggedPattern(tagPattern);
        }

        /// <summary>
        /// Get all the parts which have at least SOME non-default name:
        /// </summary>
        /// <returns></returns>
        private ListValue GetAllTaggedParts()
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetAllTaggedParts();
        }

        private ListValue GetModulesNamed(StringValue modName)
        {
            return PartValueFactory.Construct(Vessel.rootPart, Shared).GetModulesNamed(modName);
        }

        private ListValue GetPartsInGroup(StringValue groupName)
        {
            var matchGroup = KSPActionGroup.None;
            string upperName = groupName.ToUpper();

            // TODO: later refactor:  put this in a Dictionary lookup instead, and then share it
            // by both this code and the code in ActionGroup.cs:
            if (upperName == "SAS") { matchGroup = KSPActionGroup.SAS; }
            if (upperName == "GEAR") { matchGroup = KSPActionGroup.Gear; }
            if (upperName == "LIGHTS") { matchGroup = KSPActionGroup.Light; }
            if (upperName == "BRAKES") { matchGroup = KSPActionGroup.Brakes; }
            if (upperName == "RCS") { matchGroup = KSPActionGroup.RCS; }
            if (upperName == "ABORT") { matchGroup = KSPActionGroup.Abort; }
            if (upperName == "AG1") { matchGroup = KSPActionGroup.Custom01; }
            if (upperName == "AG2") { matchGroup = KSPActionGroup.Custom02; }
            if (upperName == "AG3") { matchGroup = KSPActionGroup.Custom03; }
            if (upperName == "AG4") { matchGroup = KSPActionGroup.Custom04; }
            if (upperName == "AG5") { matchGroup = KSPActionGroup.Custom05; }
            if (upperName == "AG6") { matchGroup = KSPActionGroup.Custom06; }
            if (upperName == "AG7") { matchGroup = KSPActionGroup.Custom07; }
            if (upperName == "AG8") { matchGroup = KSPActionGroup.Custom08; }
            if (upperName == "AG9") { matchGroup = KSPActionGroup.Custom09; }
            if (upperName == "AG10") { matchGroup = KSPActionGroup.Custom10; }

            ListValue kScriptParts = new ListValue();
            if (matchGroup == KSPActionGroup.None) return kScriptParts;

            foreach (global::Part p in Vessel.parts)
            {
                // See if any of the parts' actions are this action group:
                bool hasPartAction = p.Actions.Any(a => a.actionGroup.Equals(matchGroup));
                if (hasPartAction)
                {
                    kScriptParts.Add(PartValueFactory.Construct(p, Shared));
                    continue;
                }

                var modules = p.Modules.Cast<PartModule>();
                bool hasModuleAction = modules.Any(pm => pm.Actions.Any(a => a.actionGroup.Equals(matchGroup)));
                if (hasModuleAction)
                {
                    kScriptParts.Add(PartValueFactory.Construct(p, Shared));
                }
            }
            return kScriptParts;
        }

        private ListValue GetModulesInGroup(StringValue groupName)
        {
            var matchGroup = KSPActionGroup.None;
            string upperName = groupName.ToUpper();

            // TODO: later refactor:  put this in a Dictionary lookup instead, and then share it
            // by both this code and the code in ActionGroup.cs:
            if (upperName == "SAS") { matchGroup = KSPActionGroup.SAS; }
            if (upperName == "GEAR") { matchGroup = KSPActionGroup.Gear; }
            if (upperName == "LIGHTS") { matchGroup = KSPActionGroup.Light; }
            if (upperName == "BRAKES") { matchGroup = KSPActionGroup.Brakes; }
            if (upperName == "RCS") { matchGroup = KSPActionGroup.RCS; }
            if (upperName == "ABORT") { matchGroup = KSPActionGroup.Abort; }
            if (upperName == "AG1") { matchGroup = KSPActionGroup.Custom01; }
            if (upperName == "AG2") { matchGroup = KSPActionGroup.Custom02; }
            if (upperName == "AG3") { matchGroup = KSPActionGroup.Custom03; }
            if (upperName == "AG4") { matchGroup = KSPActionGroup.Custom04; }
            if (upperName == "AG5") { matchGroup = KSPActionGroup.Custom05; }
            if (upperName == "AG6") { matchGroup = KSPActionGroup.Custom06; }
            if (upperName == "AG7") { matchGroup = KSPActionGroup.Custom07; }
            if (upperName == "AG8") { matchGroup = KSPActionGroup.Custom08; }
            if (upperName == "AG9") { matchGroup = KSPActionGroup.Custom09; }
            if (upperName == "AG10") { matchGroup = KSPActionGroup.Custom10; }

            ListValue kScriptParts = new ListValue();

            // This is almost identical to the logic in GetPartsInGroup and it might be a nice idea
            // later to merge them somehow:
            //
            if (matchGroup == KSPActionGroup.None) return kScriptParts;

            foreach (global::Part p in Vessel.parts)
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.Actions.Any(a => a.actionGroup.Equals(matchGroup)))
                    {
                        kScriptParts.Add(PartModuleFieldsFactory.Construct(pm, Shared));
                    }
                }

            return kScriptParts;
        }
        public global::Part GetControlPart()
        {
            global::Part res = Vessel.GetReferenceTransformPart(); //this can actually be null
            if (res != null) { return res; }
            else { return Vessel.rootPart; } //the root part is used as reference in that case
        }
    }
}
