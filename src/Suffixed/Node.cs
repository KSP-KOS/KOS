using System;
using System.Collections.Generic;

namespace kOS.Suffixed
{
    public class Node : SpecialValue
    {
        public static Dictionary<ManeuverNode, Node> NodeLookup { get; private set; } 

        private ManeuverNode nodeRef;
        private Vessel vesselRef;
        private readonly SharedObjects shared;

        public Node(double time, double radialOut, double normal, double prograde, SharedObjects shareObj) :this (shareObj)
        {
            Time = time;
            Pro = prograde;
            RadOut = radialOut;
            Norm = normal;
        }

        public Node(Vessel v, ManeuverNode existingNode, SharedObjects shareObj) :this(shareObj)
        {
            nodeRef = existingNode;
            vesselRef = v;
            NodeLookup.Add(existingNode, this);

            UpdateValues();
        }

        private Node(SharedObjects shared)
        {
            this.shared = shared;
        }

        public double Time { get; set; }
        public double Pro { get; set; }
        public double RadOut { get; set; }
        public double Norm { get; set; }

        static Node()
        {
            NodeLookup = new Dictionary<ManeuverNode, Node>();
        }

        public static Node FromExisting(Vessel v, ManeuverNode existingNode, SharedObjects shared)
        {
            return NodeLookup.ContainsKey(existingNode) ? NodeLookup[existingNode] : new Node(v, existingNode, shared);
        }

        public void AddToVessel(Vessel v)
        {
            if (nodeRef != null) throw new Exception("Node has already been added");
            
            vesselRef = v;
            nodeRef = v.patchedConicSolver.AddManeuverNode(Time);

            UpdateNodeDeltaV();

            v.patchedConicSolver.UpdateFlightPlan();

            NodeLookup.Add(nodeRef, this);
        }

        public void UpdateAll()
        {
            nodeRef.OnGizmoUpdated(new Vector3d(RadOut, Norm, Pro), Time);
        }

        private void UpdateNodeDeltaV()
        {
            if (nodeRef == null) return;
            var dv = new Vector3d(RadOut, Norm, Pro);
            nodeRef.DeltaV = dv;
        }

        public void CheckNodeRef()
        {
            if (nodeRef == null)
            {
                throw new Exception("Must attach node first");
            }
        }

        public Vector GetBurnVector()
        {
            CheckNodeRef();

            return new Vector(nodeRef.GetBurnVector(vesselRef.GetOrbit()));
        }

        private void UpdateValues()
        {
            // If this node is attached, and the values on the attached node have chaged, I need to reflect that
            if (nodeRef == null) return;

            Time = nodeRef.UT;
            RadOut = nodeRef.DeltaV.x;
            Norm = nodeRef.DeltaV.y;
            Pro = nodeRef.DeltaV.z;
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
                    return Time - Planetarium.GetUniversalTime();
                case "PROGRADE":
                    return Pro;
                case "RADIALOUT":
                    return RadOut;
                case "NORMAL":
                    return Norm;
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
                    Time = ((double) value) + Planetarium.GetUniversalTime();
                    break;
                case "PROGRADE":
                    Pro = (double) value;
                    break;
                case "RADIALOUT":
                    RadOut = (double) value;
                    break;
                case "NORMAL":
                    Norm = (double) value;
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

            NodeLookup.Remove(nodeRef);

            vesselRef.patchedConicSolver.RemoveManeuverNode(nodeRef);

            nodeRef = null;
            vesselRef = null;
        }

        public override string ToString()
        {
            UpdateValues();
            return "NODE(" + Time + "," + RadOut + "," + Norm + "," + Pro + ")";
        }
    }
}
