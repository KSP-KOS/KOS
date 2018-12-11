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
        public StageValues StageValues { get; }

        //TODO: share these between all CPUs (or maybe have one VesselTarget instance?)
        //TODO: create single list of parts and _slices_ for `children`
        //..... [root, child1, child2, ..., part1-1, part1-2, ..., part2-1, ... heap ;)
        private PartValue rootPart;
        private DecouplerValue nextDecoupler;
        private ListValue<PartValue> allParts;
        private ListValue<DockingPortValue> dockingPorts;
        private ListValue<DecouplerValue> decouplers;
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
        public ListValue<PartValue> Parts
        {
            get
            {
                if (allParts == null)
                    ConstructParts();
                return allParts;
            }
        }
        public ListValue<DockingPortValue> DockingPorts
        {
            get
            {
                if (dockingPorts == null)
                    ConstructParts();
                return dockingPorts;
            }
        }
        public ListValue<DecouplerValue> Decouplers
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
            allParts = new ListValue<PartValue>();
            dockingPorts = new ListValue<DockingPortValue>();
            decouplers = new ListValue<DecouplerValue>();
            partCache = new Dictionary<global::Part, PartValue>();

            ConstructPart(Vessel.rootPart, null, null);

            allParts.IsReadOnly = true;
            dockingPorts.IsReadOnly = true;
            decouplers.IsReadOnly = true;
        }
        private void ConstructPart(global::Part part, PartValue parent, DecouplerValue decoupler)
        {
            if (part.State == PartStates.DEAD || part.transform == null)
                return;

            PartValue self = null;
            foreach (var module in part.Modules)
            {
                var engine = module as IEngineStatus;
                if (engine != null)
                {
                    self = new EngineValue(Shared, part, parent, decoupler);
                    break;
                }
                if (module is IStageSeparator)
                {
                    var dock = module as ModuleDockingNode;
                    if (dock != null)
                    {
                        var port = new DockingPortValue(Shared, part, parent, decoupler, dock);
                        self = port;
                        dockingPorts.Add(port);
                        if (!module.StagingEnabled())
                            break;
                        decoupler = port;
                        decouplers.Add(decoupler);
                    }
                    else
                    {
                        // ignore anything with staging disabled and continue the search
                        // this can e.g. be heat shield or some sensor with integrated decoupler
                        if (!module.StagingEnabled())
                            continue;
                        if (module is LaunchClamp)
                            self = decoupler = new LaunchClampValue(Shared, part, parent, decoupler);
                        else if (module is ModuleDecouple || module is ModuleAnchoredDecoupler)
                            self = decoupler = new DecouplerValue(Shared, part, parent, decoupler);
                        else // ModuleServiceModule ?
                            continue; // rather continue the search
                        decouplers.Add(decoupler);
                    }
                    // ignore leftover decouplers
                    if (decoupler == null || decoupler.Part.inverseStage >= StageManager.CurrentStage)
                        break;
                    // check if we just created closer decoupler (see StageValues.CreatePartSet)
                    if (nextDecoupler == null || decoupler.Part.inverseStage > nextDecoupler.Part.inverseStage)
                        nextDecoupler = decoupler;
                    break;
                }
                var sensor = module as ModuleEnviroSensor;
                if (sensor != null)
                {
                    self = new SensorValue(Shared, part, parent, decoupler, sensor);
                    break;
                }
            }
            if (self == null)
                self = new PartValue(Shared, part, parent, decoupler);
            if (rootPart == null)
                rootPart = self;
            partCache[part] = self;
            allParts.Add(self);
            foreach (var child in part.children)
                ConstructPart(child, self, decoupler);
            self.Children.IsReadOnly = true;
        }

        private ListValue GetPartsDubbed(StringValue searchTerm)
        {
            // Get the list of all the parts where the part's API name OR its GUI title or its tag name matches.
            List<global::Part> kspParts = new List<global::Part>();
            kspParts.AddRange(GetRawPartsNamed(searchTerm));
            kspParts.AddRange(GetRawPartsTitled(searchTerm));
            kspParts.AddRange(GetRawPartsTagged(searchTerm));

            // The "Distinct" operation is there because it's possible for someone to use a tag name that matches the part name.
            return PartValueFactory.Construct(kspParts.Distinct(), Shared);
        }

        private ListValue GetPartsDubbedPattern(StringValue searchPattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(searchPattern, RegexOptions.IgnoreCase);
            // Get the list of all the parts where the part's API name OR its GUI title or its tag name matches the pattern.
            List<global::Part> kspParts = new List<global::Part>();
            kspParts.AddRange(GetRawPartsNamedPattern(r));
            kspParts.AddRange(GetRawPartsTitledPattern(r));
            kspParts.AddRange(GetRawPartsTaggedPattern(r));

            // The "Distinct" operation is there because it's possible for someone to use a tag name that matches the part name.
            return PartValueFactory.Construct(kspParts.Distinct(), Shared);
        }

        private ListValue GetPartsNamed(StringValue partName)
        {
            return PartValueFactory.Construct(GetRawPartsNamed(partName), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsNamed(string partName)
        {
            // Get the list of all the parts where the part's KSP API title matches:
            return Vessel.parts.FindAll(
                part => String.Equals(part.name, partName, StringComparison.CurrentCultureIgnoreCase));
        }

        private ListValue GetPartsNamedPattern(StringValue partNamePattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(partNamePattern, RegexOptions.IgnoreCase);
            return PartValueFactory.Construct(GetRawPartsNamedPattern(r), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsNamedPattern(Regex partNamePattern)
        {
            // Get the list of all the parts where the part's KSP API title matches the pattern:
            return Vessel.parts.FindAll(
                part => partNamePattern.IsMatch(part.name));
        }

        private ListValue GetPartsTitled(StringValue partTitle)
        {
            return PartValueFactory.Construct(GetRawPartsTitled(partTitle), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTitled(string partTitle)
        {
            // Get the list of all the parts where the part's GUI title matches:
            return Vessel.parts.FindAll(
                part => String.Equals(part.partInfo.title, partTitle, StringComparison.CurrentCultureIgnoreCase));
        }

        private ListValue GetPartsTitledPattern(StringValue partTitlePattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(partTitlePattern, RegexOptions.IgnoreCase);
            return PartValueFactory.Construct(GetRawPartsTitledPattern(r), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTitledPattern(Regex partTitlePattern)
        {
            // Get the list of all the parts where the part's GUI title matches the pattern:
            return Vessel.parts.FindAll(
                part => partTitlePattern.IsMatch(part.partInfo.title));
        }

        private ListValue GetPartsTagged(StringValue tagName)
        {
            return PartValueFactory.Construct(GetRawPartsTagged(tagName), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTagged(string tagName)
        {
            return Vessel.parts
                .Where(p => p.Modules.OfType<KOSNameTag>()
                .Any(tag => String.Equals(tag.nameTag, tagName, StringComparison.CurrentCultureIgnoreCase)));
        }

        private ListValue GetPartsTaggedPattern(StringValue tagPattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(tagPattern, RegexOptions.IgnoreCase);
            return PartValueFactory.Construct(GetRawPartsTaggedPattern(r), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTaggedPattern(Regex tagPattern)
        {
            return Vessel.parts
                .Where(p => p.Modules.OfType<KOSNameTag>()
                .Any(tag => tagPattern.IsMatch(tag.nameTag)));
        }

        /// <summary>
        /// Get all the parts which have at least SOME non-default name:
        /// </summary>
        /// <returns></returns>
        private ListValue GetAllTaggedParts()
        {
            IEnumerable<global::Part> partsWithName = Vessel.parts
                .Where(p => p.Modules.OfType<KOSNameTag>()
                .Any(tag => !String.Equals(tag.nameTag, "", StringComparison.CurrentCultureIgnoreCase)));

            return PartValueFactory.Construct(partsWithName, Shared);
        }

        private ListValue GetModulesNamed(StringValue modName)
        {
            // This is slow - maybe there should be a faster lookup string hash, but
            // KSP's data model seems to have not implemented it:
            IEnumerable<PartModule> modules = Vessel.parts
                .SelectMany(p => p.Modules.Cast<PartModule>()
                .Where(pMod => String.Equals(pMod.moduleName, modName, StringComparison.CurrentCultureIgnoreCase)));

            return PartModuleFieldsFactory.Construct(modules, Shared);
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
