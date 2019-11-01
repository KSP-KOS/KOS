using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("DockingPort")]
    public class DockingPortValue : DecouplerValue
    {
        private readonly ModuleDockingNode module;

        /// <summary>
        /// Do not call! VesselTarget.ConstructPart uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal DockingPortValue(SharedObjects shared, global::Part part, PartValue parent, DecouplerValue decoupler, ModuleDockingNode module)
            : base(shared, part, parent, decoupler)
        {
            this.module = module;
            RegisterInitializer(DockingInitializeSuffixes);
        }

        private void DockingInitializeSuffixes()
        {
            AddSuffix("AQUIRERANGE", new Suffix<ScalarValue>(() => { throw new Safe.Exceptions.KOSObsoletionException("0.18.0", "AQUIRERANGE", "ACQUIRERANGE", string.Empty); }));
            AddSuffix("AQUIREFORCE", new Suffix<ScalarValue>(() => { throw new Safe.Exceptions.KOSObsoletionException("0.18.0", "AQUIREFORCE", "ACQUIREFORCE", string.Empty); }));
            AddSuffix("AQUIRETORQUE", new Suffix<ScalarValue>(() => { throw new Safe.Exceptions.KOSObsoletionException("0.18.0", "AQUIRETORQUE", "ACQUIRETORQUE", string.Empty); }));
            AddSuffix("ACQUIRERANGE", new Suffix<ScalarValue>(() => module.acquireRange));
            AddSuffix("ACQUIREFORCE", new Suffix<ScalarValue>(() => module.acquireForce));
            AddSuffix("ACQUIRETORQUE", new Suffix<ScalarValue>(() => module.acquireTorque));
            AddSuffix("REENGAGEDISTANCE", new Suffix<ScalarValue>(() => module.minDistanceToReEngage));
            AddSuffix("DOCKEDSHIPNAME", new Suffix<StringValue>(() => module.vesselInfo != null ? module.vesselInfo.name : string.Empty));
            AddSuffix("STATE", new Suffix<StringValue>(() => module.state));
            AddSuffix("TARGETABLE", new Suffix<BooleanValue>(() => true));
            AddSuffix("UNDOCK", new NoArgsVoidSuffix(() => DoUndock()));
            AddSuffix("TARGET", new NoArgsVoidSuffix(() => module.SetAsTarget()));
            AddSuffix("PORTFACING", new NoArgsSuffix<Direction>(GetPortFacing,
                                                               "The direction facing outward from the docking port.  This " +
                                                               "can differ from :FACING in the case of sideways-facing " +
                                                               "docking ports like the inline docking port."));
            AddSuffix("NODEPOSITION", new Suffix<Vector>(GetNodePosition, "The position of the docking node itself rather than the part's center of mass"));
            AddSuffix("NODETYPE", new Suffix<StringValue>(() => module.nodeType, "The type of the docking node"));

            AddSuffix("DOCKWATCHERS", new NoArgsSuffix<UniqueSetValue<UserDelegate>>(() => Shared.DispatchManager.CurrentDispatcher.GetPartCoupleNotifyees(module.part)));
            AddSuffix("UNDOCKWATCHERS", new NoArgsSuffix<UniqueSetValue<UserDelegate>>(() => Shared.DispatchManager.CurrentDispatcher.GetPartUndockNotifyees(module.part)));

            AddSuffix("PARTNER", new Suffix<Structure>(() => (Structure)GetPartner() ?? StringValue.None, "The docking port this docking port is attached to."));
            AddSuffix("HASPARTNER", new Suffix<BooleanValue>(() => module.otherNode != null, "Whether or not this docking port is attached to another docking port."));
        }

        public override ITargetable Target
        {
            get { return module; }
        }

        public PartValue GetPartner()
        {
            var otherNode = module.otherNode;
            
            if (otherNode == null)
            {
                return null;
            }

            var otherVessel = VesselTarget.CreateOrGetExisting(otherNode.vessel, Shared);
            foreach (var part in otherVessel.Parts)
            {
                if (part.Part == otherNode.part)
                {
                    return part;
                }
            }
            
            throw new Safe.Exceptions.KOSException("The docking port indicated that it was connected to another docking port, but that port could not be found. Tried to find: " + otherNode.GetModuleDisplayName());
        }

        public static new ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            var vessel = VesselTarget.CreateOrGetExisting(sharedObj);
            foreach (var part in parts)
            {
                if (part.Modules.Contains<ModuleDockingNode>())
                    toReturn.Add(vessel[part]);
            }
            return toReturn;
        }

        private Direction GetPortFacing()
        {
            // module.nodeTransform describes the transform representing the facing of
            // the docking node as opposed to the facing of the part itself.  In the
            // case of a docking port facing out the side of the part (the in-line
            // docking node for example) they can differ.

            return new Direction(module.nodeTransform.rotation);
        }

        public Vector GetNodePosition()
        {
            // like with GetPortFacing above, the position of the docking node itself difers
            // from the position of the part's center of mass.  This returns the possition
            // of the node where the two docking ports will join together, which will help
            // with docking operations

            return new Vector(module.nodeTransform.position - Shared.Vessel.CoMD);
        }

        public void DoUndock()
        {
            // if the module is not currently docked, fail silently.
            if (module.otherNode != null)
            {
                // check to see if either the undock or decouple events are available
                // and execute accordingly.
                var evnt1 = module.Events["Undock"];
                var evnt2 = module.Events["Decouple"];
                if (evnt1 != null && evnt1.guiActive && evnt1.active)
                {
                    module.Undock();
                }
                else if (evnt2 != null && evnt2.guiActive && evnt2.active)
                {
                    module.Decouple();
                }
                else
                {
                    // If you can't do either event on this port, check to see if
                    // you can on the port it's docked too!
                    evnt1 = module.otherNode.Events["Undock"];
                    evnt2 = module.otherNode.Events["Decouple"];
                    if (evnt1 != null && evnt1.guiActive && evnt1.active)
                    {
                        module.otherNode.Undock();
                    }
                    else if (evnt2 != null && evnt2.guiActive && evnt2.active)
                    {
                        module.otherNode.Decouple();
                    }
                }
            }
        }
    }
}
