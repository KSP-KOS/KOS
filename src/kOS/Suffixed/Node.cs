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
        private double prograde;
        private double radialOut;
        private double normal;

        static Node()
        {
            nodeLookup = new Dictionary<ManeuverNode, Node>();
        }

        public Node(double time, double radialOut, double normal, double prograde, SharedObjects shareObj)
            : this(shareObj)
        {
            this.time = time;
            this.prograde = prograde;
            this.radialOut = radialOut;
            this.normal = normal;
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

            AddSuffix("ETA", new SetSuffix<double>(
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

            AddSuffix("PROGRADE", new SetSuffix<double>(
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

            AddSuffix("RADIALOUT", new SetSuffix<double>(
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

            AddSuffix("NORMAL", new SetSuffix<double>(
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

            if (v.patchedConicSolver == null)
                throw new KOSSituationallyInvalidException(
                    "A KSP limitation makes it impossible to access the manuever nodes of this vessel at this time. " +
                    "(perhaps it's not the active vessel?)");

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

            if (vesselRef.patchedConicSolver == null)
                throw new KOSSituationallyInvalidException(
                    "A KSP limitation makes it impossible to access the manuever nodes of this vessel at this time. " +
                    "(perhaps it's not the active vessel?)");

            vesselRef.patchedConicSolver.RemoveManeuverNode(nodeRef);

            nodeRef = null;
            vesselRef = null;
        }

        public override string ToString()
        {
            FromNodeRef();
            return string.Format("NODE({0},{1},{2},{3})", (time - Planetarium.GetUniversalTime()), radialOut, normal, prograde);
        }

        private void ToNodeRef()
        {
            nodeRef.OnGizmoUpdated(new Vector3d(radialOut, normal, prograde), time);
        }

        private void UpdateNodeDeltaV()
        {
            if (nodeRef == null) return;
            var dv = new Vector3d(radialOut, normal, prograde);
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
            radialOut = nodeRef.DeltaV.x;
            normal = nodeRef.DeltaV.y;
            prograde = nodeRef.DeltaV.z;
        }
    }
}