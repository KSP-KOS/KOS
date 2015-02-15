using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed
{
    public class Node : Structure
    {
        private static readonly Dictionary<ManeuverNode, Node> nodeLookup;

        private ManeuverNode nodeRef;
        private Vessel vesselRef;
        private readonly SharedObjects shared;
        private double time;
        private double pro;
        private double radOut;
        private double norm;

        static Node()
        {
            nodeLookup = new Dictionary<ManeuverNode, Node>();
        }

        public Node(double time, double radialOut, double normal, double prograde, SharedObjects shareObj)
            : this(shareObj)
        {
            this.time = time;
            pro = prograde;
            radOut = radialOut;
            norm = normal;
        }

        private Node(Vessel v, ManeuverNode existingNode, SharedObjects shareObj)
            : this(shareObj)
        {
            nodeRef = existingNode;
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
            AddSuffix("ETA", new SetSuffix<double>(() => time - Planetarium.GetUniversalTime(), value => time = value + Planetarium.GetUniversalTime()));

            AddSuffix("PROGRADE", new SetSuffix<double>(() => time - Planetarium.GetUniversalTime(), value =>
            {
                FromNodeRef();
                pro = value;
                ToNodeRef();
            }));

            AddSuffix("RADIALOUT", new SetSuffix<double>(() => radOut, value =>
            {
                FromNodeRef();
                radOut = value;
                ToNodeRef();
            }));

            AddSuffix("NORMAL", new SetSuffix<double>(() => time - Planetarium.GetUniversalTime(), value =>
            {
                FromNodeRef();
                norm = value;
                ToNodeRef();
            }));
            AddSuffix("ORBIT", new Suffix<OrbitInfo>(() =>
            {
                if (nodeRef == null) throw new Exception("Node must be added to flight plan first");
                return new OrbitInfo(nodeRef.nextPatch, shared);
            }));

        }

        public static Node FromExisting(Vessel v, ManeuverNode existingNode, SharedObjects shared)
        {
            return nodeLookup.ContainsKey(existingNode) ? nodeLookup[existingNode] : new Node(v, existingNode, shared);
        }

        public void AddToVessel(Vessel v)
        {
            if (nodeRef != null) throw new Exception("Node has already been added");

            string careerReason;
            if (! Career.CanMakeNodes(out careerReason))
                throw new KOSLowTechException("use maneuver nodes", careerReason);

            vesselRef = v;
            nodeRef = v.patchedConicSolver.AddManeuverNode(time);

            UpdateNodeDeltaV();

            v.patchedConicSolver.UpdateFlightPlan();

            nodeLookup.Add(nodeRef, this);
        }

        public Vector GetBurnVector()
        {
            CheckNodeRef();

            return new Vector(nodeRef.GetBurnVector(vesselRef.GetOrbit()));
        }


        public void Remove()
        {
            if (nodeRef == null) return;

            string careerReason;
            if (! Career.CanMakeNodes(out careerReason))
                throw new KOSLowTechException("use maneuver nodes", careerReason);

            nodeLookup.Remove(nodeRef);

            vesselRef.patchedConicSolver.RemoveManeuverNode(nodeRef);

            nodeRef = null;
            vesselRef = null;
        }

        public override string ToString()
        {
            FromNodeRef();
            return "NODE(" + time + "," + radOut + "," + norm + "," + pro + ")";
        }

        private void ToNodeRef()
        {
            nodeRef.OnGizmoUpdated(new Vector3d(radOut, norm, pro), time);
        }

        private void UpdateNodeDeltaV()
        {
            if (nodeRef == null) return;
            var dv = new Vector3d(radOut, norm, pro);
            nodeRef.DeltaV = dv;
        }

        private void CheckNodeRef()
        {
            if (nodeRef == null)
            {
                throw new Exception("Must attach node first");
            }
        }

        private void FromNodeRef()
        {
            // If this node is attached, and the values on the attached node have changed, I need to reflect that
            if (nodeRef == null) return;

            time = nodeRef.UT;
            radOut = nodeRef.DeltaV.x;
            norm = nodeRef.DeltaV.y;
            pro = nodeRef.DeltaV.z;
        }
    }
}