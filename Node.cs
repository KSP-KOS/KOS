using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class Node : SpecialValue
    {
        ManeuverNode nodeRef;
        Vessel vesselRef;
        public double UT;
        public double Pro;
        public double RadOut;
        public double Norm;

        public static Dictionary<ManeuverNode, Node> NodeLookup = new Dictionary<ManeuverNode, Node>();

        public Node(double ut, double radialOut, double normal, double prograde)
        {
            this.UT = ut;
            this.Pro = prograde;
            this.RadOut = radialOut;
            this.Norm = normal;
        }

        public Node(Vessel v, ManeuverNode existingNode)
        {
            nodeRef = existingNode;
            vesselRef = v;
            NodeLookup.Add(existingNode, this);

            updateValues();
        }

        public static Node FromExisting(Vessel v, ManeuverNode existingNode)
        {
            if (NodeLookup.ContainsKey(existingNode)) return NodeLookup[existingNode];
            
            return new Node(v, existingNode);
        }
        
        public void AddToVessel(Vessel v)
        {
            if (nodeRef != null) throw new kOSException("Node has already been added");

            vesselRef = v;
            nodeRef = v.patchedConicSolver.AddManeuverNode(UT);

            var progradeAtUT = v.orbit.getOrbitalVelocityAtUT(UT).normalized;

            Vector3d dv = new Vector3d(RadOut, Norm, Pro);
            nodeRef.DeltaV = dv;

            v.patchedConicSolver.UpdateFlightPlan();

            NodeLookup.Add(nodeRef, this);
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

            return new Vector(nodeRef.GetBurnVector(vesselRef.orbit));
        }

        private void updateValues()
        {
            // If this node is attached, and the values on the attached node have chaged, I need to reflect that
            if (nodeRef != null)
            {
                UT = nodeRef.UT;

                RadOut = nodeRef.DeltaV.x;
                Norm = nodeRef.DeltaV.y;
                Pro = nodeRef.DeltaV.z;
            }
        }

        public string GetClosestEncounter()
        {
            CheckNodeRef(); 
            var cep = nodeRef.patch.closestEncounterPatch;

            if (cep == null) return "NO PATCH";
            else
            {
                return cep.closestEncounterBody.name;
            }
        }

        public string debugStats(Orbit patch)
        {
            String patchtext = "\nPatch \n";

            try { patchtext += "Start:" + patch.patchStartTransition.ToString() + " End:" + patch.patchEndTransition.ToString() + " \n"; }
            catch (Exception e) { patchtext += "No start or end \n"; }

            try { patchtext += "Ap:" + patch.ApA; }
            catch (Exception e) { patchtext += "No apo \n"; }

            try { patchtext += "Pe:" + patch.PeA; }
            catch (Exception e) { patchtext += "No peri \n"; }

            if (patch.referenceBody != null) patchtext += "Ref:" + patch.referenceBody.ToString() + " \n";
            if (patch.closestEncounterBody != null) patchtext += "Body:" + patch.closestEncounterBody.ToString() + " \n";

            //if (patch.nextPatch != null) patchtext += debugStats(patch.nextPatch, i);

            return patchtext;
        }
        
        public override object GetSuffix(string suffixName)
        {
            updateValues();
            
            if (suffixName == "BURNVECTOR") return GetBurnVector();
            else if (suffixName == "ETA") return UT - Planetarium.GetUniversalTime();
            else if (suffixName == "DELTAV") return new Vector(RadOut, Norm, Pro);
            else if (suffixName == "PROGRADE") return Pro;
            else if (suffixName == "RADIALOUT") return RadOut;
            else if (suffixName == "NORMAL") return Norm;
            else if (suffixName == "APOAPSIS") { CheckNodeRef(); return nodeRef.patch.ApA; }
            else if (suffixName == "PERIAPSIS") { CheckNodeRef(); return nodeRef.patch.PeA; }
            else if (suffixName == "ENCOUNTER") 
            { 
                return GetClosestEncounter(); 
            }
            else if (suffixName == "TRANSITION") { return nodeRef.patch.patchEndTransition.ToString(); }
            else if (suffixName == "GOCRAZY")
            {
                String stats = "";

                foreach (Orbit patch in nodeRef.solver.flightPlan)
                {
                    stats += debugStats(patch);
                }

                UnityEngine.Debug.Log(stats);

                return stats;
            }
            
            return base.GetSuffix(suffixName);
        }

        public void Remove()
        {
            if (nodeRef != null)
            {
                NodeLookup.Remove(nodeRef);

                vesselRef.patchedConicSolver.RemoveManeuverNode(nodeRef);

                nodeRef = null;
                vesselRef = null;
            }
        }

        public override string ToString()
        {
            return "NODE(" + UT + "," + Pro + "," + RadOut + "," + Norm + ")";
        }
    }
}
