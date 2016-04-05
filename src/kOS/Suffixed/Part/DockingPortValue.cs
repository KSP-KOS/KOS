using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("DockingPort")]
    public class DockingPortValue : PartValue
    {
        private readonly ModuleDockingNode module;

        public DockingPortValue(ModuleDockingNode module, SharedObjects sharedObj)
            : base(module.part, sharedObj)
        {
            this.module = module;
            DockingInitializeSuffixes();
        }

        private void DockingInitializeSuffixes()
        {
            AddSuffix("AQUIRERANGE", new Suffix<ScalarValue>(() => { throw new Safe.Exceptions.KOSDeprecationException("0.18.0", "AQUIRERANGE", "ACQUIRERANGE"); }));
            AddSuffix("AQUIREFORCE", new Suffix<ScalarValue>(() => { throw new Safe.Exceptions.KOSDeprecationException("0.18.0", "AQUIREFORCE", "ACQUIREFORCE"); }));
            AddSuffix("AQUIRETORQUE", new Suffix<ScalarValue>(() => { throw new Safe.Exceptions.KOSDeprecationException("0.18.0", "AQUIRETORQUE", "ACQUIRETORQUE"); }));
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
        }

        public override ITargetable Target
        {
            get { return module; }
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var dockingNode = module as ModuleDockingNode;
                    if (dockingNode != null)
                    {
                        toReturn.Add(new DockingPortValue(dockingNode, sharedObj));
                    }
                }
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

            return new Vector(module.nodeTransform.position - Shared.Vessel.findWorldCenterOfMass());
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