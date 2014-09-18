using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;

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

            UpdateValues();
        }

        private Node(SharedObjects shared)
        {
            this.shared = shared;
        }

        public static Node FromExisting(Vessel v, ManeuverNode existingNode, SharedObjects shared)
        {
            return nodeLookup.ContainsKey(existingNode) ? nodeLookup[existingNode] : new Node(v, existingNode, shared);
        }

        public void AddToVessel(Vessel v)
        {
            if (nodeRef != null) throw new Exception("Node has already been added");

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

        public override object GetSuffix(string suffixName)
        {
            UpdateValues();

            switch (suffixName)
            {
                case "DELTAV":
                case "BURNVECTOR":
                    return GetBurnVector();

                case "ETA":
                    return time - Planetarium.GetUniversalTime();

                case "PROGRADE":
                    return pro;

                case "RADIALOUT":
                    return radOut;

                case "NORMAL":
                    return norm;

                case "ORBIT":
                    if (nodeRef == null) throw new Exception("Node must be added to flight plan first");
                    return new OrbitInfo(nodeRef.nextPatch, shared);
            }

            return base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            UpdateValues();
            value = ConvertToDoubleIfNeeded(value);

            switch (suffixName)
            {
                case "DELTAV":
                case "BURNVECTOR":
                case "ORBIT":
                    throw new Exception(string.Format("Suffix {0} is read only!", suffixName));
                case "ETA":
                    time = ((double)value) + Planetarium.GetUniversalTime();
                    break;

                case "PROGRADE":
                    pro = (double)value;
                    break;

                case "RADIALOUT":
                    radOut = (double)value;
                    break;

                case "NORMAL":
                    norm = (double)value;
                    break;

                default:
                    return false;
            }
            UpdateAll();
            return true;
        }

        public void Remove()
        {
            if (nodeRef == null) return;

            nodeLookup.Remove(nodeRef);

            vesselRef.patchedConicSolver.RemoveManeuverNode(nodeRef);

            nodeRef = null;
            vesselRef = null;
        }

        public override string ToString()
        {
            UpdateValues();
            return "NODE(" + time + "," + radOut + "," + norm + "," + pro + ")";
        }

        private void UpdateAll()
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

        private void UpdateValues()
        {
            // If this node is attached, and the values on the attached node have chaged, I need to reflect that
            if (nodeRef == null) return;

            time = nodeRef.UT;
            radOut = nodeRef.DeltaV.x;
            norm = nodeRef.DeltaV.y;
            pro = nodeRef.DeltaV.z;
        }
    }
}