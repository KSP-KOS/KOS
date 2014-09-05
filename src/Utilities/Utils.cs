using kOS.Suffixed;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kOS.Utilities
{
    public enum kOSKeys
    {
        LEFT = 37,
        UP = 38,
        RIGHT = 39,
        DOWN = 40,
        DEL = 46,
        F1 = 112,
        F2 = 113,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
        F10 = 121,
        F11 = 122,
        F12 = 123,
        PGUP = 33,
        PGDN = 34,
        END = 35,
        HOME = 36,
        DELETE = 44,
        INSERT = 45,
        BREAK = 19
    }
    
    public enum NagType
    {
        /// <summary>Make message go away</summary>
        SHUTUP = 0,
        /// <summary>Report this just once, then revert to SHUTUP after that</summary>
        NAGONCE,
        /// <summary>Always give the nag message every time the terminal welcome is printed</summary>
        NAGFOREVER
    }
    
    public class NagMessage
    {
        public NagType nag;
        public string message;
        public NagMessage(NagType n, string msg)
        {
            nag = n;
            message = msg;
        }
    }

    public static class Utils
    {        
        private static List<NagMessage> nags = new List<NagMessage>();
        /// <summary>
        /// Add a string message that should be shown on the terminal
        /// the next time it shows its Welcome message.
        /// It is possible to chain several of these messages together,
        /// but remember that the terminal window is small.  Keep
        /// the message short so there's room for other nag messages too.
        /// </summary>
        /// <param name="nag">Should the message be shown once, or keep being shown
        /// every time the terminal welcome message appears?</param>
        /// <param name="Message">Message to print</param>
        public static void AddNagMessage(NagType nag, string message)
        {
            nags.Add( new NagMessage(nag, message) );
        }
        /// <summary>
        /// Gets a list of all the pending nag messages,
        /// and in the process of doing that it clears out any
        /// that were set to just NAGONCE.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetPendingNags()
        {
            List<string> returnVal = new List<string>();
            foreach (NagMessage nag in nags)
                returnVal.Add(nag.message);
            
            // Only keep the NAGFOREVER ones:
            nags.RemoveAll( delegate(NagMessage nag) { if (nag.nag != NagType.NAGFOREVER) return true; else return false; } );

            return returnVal;
        }
        
        public static float Clamp(float input, float low, float high)
        {
            return (input > high ? high : (input < low ? low : input));
        }

        public static double Clamp(double input, double low, double high)
        {
            return (input > high ? high : (input < low ? low : input));
        }

        public static double? Clamp(double? input, double low, double high)
        {
            if (!input.HasValue)
            {
                return null;
            }
            return Clamp(input.Value, low, high);
        }

        public static float? Clamp(float? input, float low, float high)
        {
            if (!input.HasValue)
            {
                return null;
            }
            return Clamp(input.Value, low, high);
        }

        public static bool IsValidNumber(double input)
        {
            return !(double.IsInfinity(input) || double.IsNaN(input));
        }

        public static bool IsValidVector(Vector3d vector)
        {
            return IsValidNumber(vector.x) &&
                   IsValidNumber(vector.y) &&
                   IsValidNumber(vector.z);
        }

        public static bool IsValidVector(Vector3 vector)
        {
            return IsValidNumber(vector.x) &&
                   IsValidNumber(vector.y) &&
                   IsValidNumber(vector.z);
        }

        public static bool IsValidVector(Vector vector)
        {
            return IsValidNumber(vector.X) &&
                   IsValidNumber(vector.Y) &&
                   IsValidNumber(vector.Z);
        }

        public static bool IsValidRotation(Quaternion quaternion)
        {
            return IsValidNumber(quaternion.x) &&
                   IsValidNumber(quaternion.y) &&
                   IsValidNumber(quaternion.z) &&
                   IsValidNumber(quaternion.w);
        }

        public static double ProspectForResource(string resourceName, List<Part> engines)
        {
            var visited = new List<Part>();

            IEnumerable<FuelLine> lines;
            if (engines.Count > 0)
            {
                // The recursinve algorithm is written to assume all the engines are on
                // the same vessel, so just use one of the engines to get the vessel:
                lines = engines[0].vessel.parts.OfType<FuelLine>();
            }
            else
            {
                // Uhh... no engines in engine list - no point in doing the work.
                return 0.0;
            }
            return engines.Sum(part => ProspectForResource(resourceName, part, lines, 0, ref visited));
        }

        public static double ProspectForResource(string resourceName, Part engine)
        {
            var visited = new List<Part>();

            IEnumerable<FuelLine> lines = engine.vessel.parts.OfType<FuelLine>();

            return ProspectForResource(resourceName, engine, lines, 0, ref visited);
        }

        public static double ProspectForResource(string resourceName, Part part, IEnumerable<FuelLine> lines, int rDepth, ref List<Part> visited)
        {
#pragma warning disable 162
            const bool DEBUG_WALK = false; // set to true to enable the logging of the recursive walk.
            var indent = new String(',', rDepth);

            if (DEBUG_WALK) Debug.Log(indent + "ProspectForResource( " + resourceName + ", " + part.uid + ":" + part.name + ", ...)");
            double ret = 0;

            if (visited.Contains(part))
            {
                if (DEBUG_WALK) Debug.Log(indent + "- Already visited, truncate recurse branch here.");
                return 0;
            }

            visited.Add(part);

            foreach (PartResource resource in part.Resources)
            {
                if (String.Equals(resource.resourceName, resourceName, StringComparison.CurrentCultureIgnoreCase))
                {
                    ret += resource.amount;
                }
            }

            foreach (var attachNode in GetActualAttachedNodes(part))
            {
                if (DEBUG_WALK) Debug.Log(indent + "- AttachNode " + attachNode.id);
                if (!attachNode.ResourceXFeed) continue;

                if (DEBUG_WALK) Debug.Log(indent + "- - It is an xfeed-able attachnode.");
                if (attachNode.attachedPart == null || (!attachNode.attachedPart.fuelCrossFeed)) continue;

                if (DEBUG_WALK) Debug.Log(indent + "- - AttachNode's other part allows crossfeed in general.");
                if (part.NoCrossFeedNodeKey.Length > 0 && attachNode.id.Contains(part.NoCrossFeedNodeKey)) continue;

                if (DEBUG_WALK) Debug.Log(indent + "- - This part allows crossfeed through specifically through this AttachNode.");
                if (attachNode.attachedPart.NoCrossFeedNodeKey.Length > 0 && attachNode.id.Contains(attachNode.attachedPart.NoCrossFeedNodeKey)) continue;

                if (DEBUG_WALK) Debug.Log(indent + "- -  Part on other side allows flow specifically through this AttachNode.");
                ret += ProspectForResource(resourceName, attachNode.attachedPart, lines, rDepth + 1, ref visited);
            }

            // Fuel lines have to be handled specially because they are not in the normal parts tree
            // and are not connected via AttachNodes:
            foreach (var fuelLine in lines.Where(fuelLine => part == fuelLine.target && fuelLine.fuelLineOpen && fuelLine.fuelCrossFeed))
            {
                if (DEBUG_WALK) Debug.Log(indent + "- Part is target of a fuel line, traversing fuel line upstream.");
                ret += ProspectForResource(resourceName, fuelLine.parent, lines, rDepth + 1, ref visited);
            }

            if (DEBUG_WALK) Debug.Log(indent + "Sum from this branch of the recurse tree is " + ret);
            return ret;
#pragma warning restore 162
        }

        /// <summary>
        /// Gets the *actual* list of attachnodes for a part.  Use as a replacement
        /// for the KSP API property part.attachNodes because that doesn't seem to
        /// reliably include the surface attached attachnodes all of the time.
        /// </summary>
        /// <param name="checkPart">part to get the nodes for</param>
        /// <returns>AttachNodes from this part to others</returns>
        private static IEnumerable<AttachNode> GetActualAttachedNodes(Part checkPart)
        {
            var returnList = new List<AttachNode>(checkPart.attachNodes);
            AttachNode srfNode = checkPart.srfAttachNode;
            if (!returnList.Contains(srfNode))
            {
                returnList.Add(srfNode);
            }
            return returnList;
        }

        /// <summary>
        ///   Fix the strange too-large or too-small angle degrees that are sometimes
        ///   returned by KSP, normalizing them into a constrained 360 degree range.
        /// </summary>
        /// <param name="inAngle">input angle in degrees</param>
        /// <param name="rangeStart">
        ///   Bottom of 360 degree range to normalize to.
        ///   ( 0 means the range [0..360]), while -180 means [-180,180] )
        /// </param>
        /// <returns>the same angle, normalized to the range given.</returns>
        public static double DegreeFix(double inAngle, double rangeStart)
        {
            double rangeEnd = rangeStart + 360.0;
            double outAngle = inAngle;
            while (outAngle > rangeEnd)
                outAngle -= 360.0;
            while (outAngle < rangeStart)
                outAngle += 360.0;
            return outAngle;
        }

        /// <summary>
        ///   Returns true if body a orbits body b, either directly or through
        ///   a grandparent chain.
        /// </summary>
        /// <param name="a">Does this body</param>
        /// <param name="b">Orbit around this body</param>
        /// <returns>True if a orbits b.  </returns>
        public static Boolean BodyOrbitsBody(CelestialBody a, CelestialBody b)
        {
            Debug.Log("BodyOrbitsBody(" + a.name + "," + b.name + ")");
            Debug.Log("a's ref body = " + (a.referenceBody == null ? "null" : a.referenceBody.name));
            Boolean found = false;
            for (var curBody = a.referenceBody;
                 curBody != null && curBody != curBody.referenceBody; // reference body of Sun points to itself, weirdly.
                 curBody = curBody.referenceBody)
            {
                Debug.Log("curBody=" + curBody.name);
                if (!curBody.name.Equals(b.name)) continue;

                found = true;
                break;
            }
            return found;
        }
    }
}