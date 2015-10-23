using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;

namespace kOS.Suffixed.Part
{
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
            AddSuffix("AQUIRERANGE", new Suffix<float>(() => { throw new kOS.Safe.Exceptions.KOSDeprecationException("0.18.0", "AQUIRERANGE", "ACQUIRERANGE", string.Empty); }));
            AddSuffix("AQUIREFORCE", new Suffix<float>(() => { throw new kOS.Safe.Exceptions.KOSDeprecationException("0.18.0", "AQUIREFORCE", "ACQUIREFORCE", string.Empty); }));
            AddSuffix("AQUIRETORQUE", new Suffix<float>(() => { throw new kOS.Safe.Exceptions.KOSDeprecationException("0.18.0", "AQUIRETORQUE", "ACQUIRETORQUE", string.Empty); }));
            AddSuffix("ACQUIRERANGE", new Suffix<float>(() => module.acquireRange));
            AddSuffix("ACQUIREFORCE", new Suffix<float>(() => module.acquireForce));
            AddSuffix("ACQUIRETORQUE", new Suffix<float>(() => module.acquireTorque));
            AddSuffix("REENGAGEDISTANCE", new Suffix<float>(() => module.minDistanceToReEngage));
            AddSuffix("DOCKEDSHIPNAME", new Suffix<string>(() => module.vesselInfo != null ? module.vesselInfo.name : string.Empty));
            AddSuffix("STATE", new Suffix<string>(() => module.state));
            AddSuffix("TARGETABLE", new Suffix<bool>(() => true));
            AddSuffix("UNDOCK", new NoArgsSuffix(() => module.Undock()));
            AddSuffix("TARGET", new NoArgsSuffix(() => module.SetAsTarget()));
            AddSuffix("PORTFACING", new NoArgsSuffix<Direction>(GetPortFacing,
                                                               "The direction facing outward from the docking port.  This " +
                                                               "can differ from :FACING in the case of sideways-facing " +
                                                               "docking ports like the inline docking port."));
            AddSuffix("NODEPOSITION", new Suffix<Vector>(GetNodePosition, "The position of the docking node itself rather than the part's center of mass"));
            AddSuffix("NODETYPE", new Suffix<string>(() => this.module.nodeType, "The type of the docking node"));
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

            return new Vector(module.nodeTransform.position - shared.Vessel.findWorldCenterOfMass());
        }
    }
}