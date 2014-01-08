using System.Collections.Generic;

namespace kOS
{
    public class Node : SpecialValue
    {
        private ManeuverNode nodeRef;
        private Vessel vesselRef;

        public static Dictionary<ManeuverNode, Node> NodeLookup = new Dictionary<ManeuverNode, Node>();
        public double Time { get; set; }
        public double Pro { get; set; }
        public double RadOut { get; set; }
        public double Norm { get; set; }


        public Node(double time, double radialOut, double normal, double prograde)
        {
            Time = time;
            Pro = prograde;
            RadOut = radialOut;
            Norm = normal;
        }

        public Node(Vessel v, ManeuverNode existingNode)
        {
            nodeRef = existingNode;
            vesselRef = v;
            NodeLookup.Add(existingNode, this);

            UpdateValues();
        }

        public static Node FromExisting(Vessel v, ManeuverNode existingNode)
        {
            return NodeLookup.ContainsKey(existingNode) ? NodeLookup[existingNode] : new Node(v, existingNode);
        }

        public void AddToVessel(Vessel v)
        {
            if (nodeRef != null) throw new kOSException("Node has already been added");

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
                throw new kOSException("Must attach node first");
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
                case "BURNVECTOR":
                    return GetBurnVector();
                case "ETA":
                    return Time - Planetarium.GetUniversalTime();
                case "DELTAV":
                    return nodeRef.DeltaV;
                case "PROGRADE":
                    return Pro;
                case "RADIALOUT":
                    return RadOut;
                case "NORMAL":
                    return Norm;
                case "APOAPSIS":
                    if (nodeRef == null) throw new kOSException("Node must be added to flight plan first");
                    return nodeRef.nextPatch.ApA;
                case "PERIAPSIS":
                    if (nodeRef == null) throw new kOSException("Node must be added to flight plan first");
                    return nodeRef.nextPatch.PeA;
            }

            return base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            UpdateValues();

            switch (suffixName)
            {
                case "DELTAV":
                case "BURNVECTOR":
                    throw new kOSReadOnlyException(suffixName);
                case "ETA":
                    Time = ((double)value) + Planetarium.GetUniversalTime();
                    UpdateAll();
                    return true;
                case "PROGRADE":
                    Pro = (double)value;
                    UpdateAll();
                    return true;
                case "RADIALOUT":
                    RadOut = (double)value;
                    UpdateAll();
                    return true;
                case "NORMAL":
                    Norm = (double)value;
                    UpdateAll();
                    return true;
            }

            return false;
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
