using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Node")]
    public class Node : Structure
    {
        private static readonly Dictionary<ManeuverNode, Node> nodeLookup;

        public ManeuverNode NodeRef { get; private set; }
        private Vessel vesselRef;
        private readonly SharedObjects shared;
        private double time;
        private double prograde;
        private double radialOut;
        private double normal;

        static Node()
        {
            nodeLookup = new Dictionary<ManeuverNode, Node>();
        }

        public Node(double time, double radialOut, double normal, double prograde, SharedObjects sharedObj)
            : this(sharedObj)
        {
            this.time = time;
            this.prograde = prograde;
            this.radialOut = radialOut;
            this.normal = normal;
        }

        public Node(TimeStamp stamp, double radialOut, double normal, double prograde, SharedObjects sharedObj)
            : this(stamp.ToUnixStyleTime(), radialOut, normal, prograde, sharedObj)
        {
        }

        public Node(kOS.Suffixed.TimeSpan span, double radialOut, double normal, double prograde, SharedObjects sharedObj)
            : this(sharedObj)
        {
            this.time = Planetarium.GetUniversalTime() + span.ToUnixStyleTime();
            this.prograde = prograde;
            this.radialOut = radialOut;
            this.normal = normal;
        }

        private Node(Vessel v, ManeuverNode existingNode, SharedObjects sharedObj)
            : this(sharedObj)
        {
            NodeRef = existingNode;
            vesselRef = v;

            nodeLookup.Add(existingNode, this);

            FromNodeRef();
        }

        private Node(SharedObjects shared)
        {
            this.shared = shared;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix(new[] {"DELTAV", "BURNVECTOR"}, new Suffix<Vector>(GetBurnVector));

            AddSuffix("ETA", new SetSuffix<ScalarValue>(
                () =>
                {
                    FromNodeRef();
                    return time - Planetarium.GetUniversalTime();
                },
                value =>
                {
                    time = value + Planetarium.GetUniversalTime();
                    ToNodeRef();
                }
            ));

            AddSuffix("TIME", new SetSuffix<ScalarValue>(
                () =>
                {
                    FromNodeRef();
                    return time;
                },
                value =>
                {
                    time = value;
                    ToNodeRef();
                }
            ));

            AddSuffix("PROGRADE", new SetSuffix<ScalarValue>(
                () =>
                {
                    FromNodeRef();
                    return prograde;
                }, 
                value =>
                {
                    prograde = value;
                    ToNodeRef();
                }
            ));

            AddSuffix("RADIALOUT", new SetSuffix<ScalarValue>(
                () =>
                {
                    FromNodeRef();
                    return radialOut;
                }, 
                value => {
                    radialOut = value;
                    ToNodeRef();
                }
            ));

            AddSuffix("NORMAL", new SetSuffix<ScalarValue>(
                () =>
                {
                    FromNodeRef();
                    return normal;
                }, 
                value => {
                    normal = value;
                    ToNodeRef();
                }
            ));

            AddSuffix(new[] {"OBT", "ORBIT"}, new Suffix<OrbitInfo>(() =>
            {
                if (NodeRef == null) throw new Exception("Node must be added to flight plan first");
                return new OrbitInfo(NodeRef.nextPatch, shared);
            }));

        }

        public static Node FromExisting(Vessel v, ManeuverNode existingNode, SharedObjects shared)
        {
            return nodeLookup.ContainsKey(existingNode) ? nodeLookup[existingNode] : new Node(v, existingNode, shared);
        }

        public void AddToVessel(Vessel v)
        {
            if (NodeRef != null) throw new Exception("Node has already been added");

            string careerReason;
            if (! Career.CanMakeNodes(out careerReason))
                throw new KOSLowTechException("use maneuver nodes", careerReason);

            vesselRef = v;

            if (v.patchedConicSolver == null)
                throw new KOSSituationallyInvalidException(
                    "A KSP limitation makes it impossible to access the manuever nodes of this vessel at this time. " +
                    "(perhaps it's not the active vessel?)");

            NodeRef = v.patchedConicSolver.AddManeuverNode(time);

            UpdateNodeDeltaV();

            v.patchedConicSolver.UpdateFlightPlan();

            nodeLookup.Add(NodeRef, this);
        }

        public Vector GetBurnVector()
        {
            CheckNodeRef();

            return new Vector(NodeRef.GetBurnVector(vesselRef.GetOrbit()));
        }


        public void Remove()
        {
            if (NodeRef == null) return;

            string careerReason;
            if (! Career.CanMakeNodes(out careerReason))
                throw new KOSLowTechException("use maneuver nodes", careerReason);

            nodeLookup.Remove(NodeRef);

            if (vesselRef.patchedConicSolver == null)
                throw new KOSSituationallyInvalidException(
                    "A KSP limitation makes it impossible to access the manuever nodes of this vessel at this time. " +
                    "(perhaps it's not the active vessel?)");

            NodeRef.RemoveSelf();

            NodeRef = null;
            vesselRef = null;
        }

        public override string ToString()
        {
            FromNodeRef();
            return string.Format("NODE({0},{1},{2},{3})", (time - Planetarium.GetUniversalTime()), radialOut, normal, prograde);
        }

        private void ToNodeRef()
        {
            if (NodeRef == null) return;

            if (NodeRef.attachedGizmo == null)
            {
                // Copy the logic from OnGizmoUpdated, excluding the two calls to attachedGizmo
                NodeRef.DeltaV = new Vector3d(radialOut, normal, prograde);
                NodeRef.UT = time;
                NodeRef.solver.UpdateFlightPlan();
            }
            else
            {
                NodeRef.OnGizmoUpdated(new Vector3d(radialOut, normal, prograde), time);
            }
        }

        private void UpdateNodeDeltaV()
        {
            if (NodeRef == null) return;
            var dv = new Vector3d(radialOut, normal, prograde);
            NodeRef.DeltaV = dv;
        }

        private void CheckNodeRef()
        {
            if (NodeRef == null)
            {
                throw new Exception("Must attach node first");
            }
        }

        private void FromNodeRef()
        {
            // If this node is attached, and the values on the attached node have changed, I need to reflect that
            if (NodeRef == null) return;

            time = NodeRef.UT;
            radialOut = NodeRef.DeltaV.x;
            normal = NodeRef.DeltaV.y;
            prograde = NodeRef.DeltaV.z;
        }
    }
}