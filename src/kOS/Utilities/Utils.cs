﻿using kOS.Safe.Utilities;
using kOS.Suffixed;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace kOS.Utilities
{
    public static class Utils
    {
        public static Camera GetCurrentCamera()
        {
            // man, KSP could really just use a simple "get whatever the current camera is" method:
            return HighLogic.LoadedSceneIsEditor ?
                       EditorLogic.fetch.editorCamera :
                       (MapView.MapIsEnabled ?
                           PlanetariumCamera.Camera : FlightCamera.fetch.mainCamera);
        }
        
        public static string GetAssemblyFileVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;            
        }

        public static bool IsValidNumber(double input)
        {
            return !(double.IsInfinity(input) || double.IsNaN(input));
        }

        public static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double RadiansToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static Vector3d SwapYZ(Vector3d vector)
        {
            return new Vector3d(vector.x, vector.z, vector.y);
        }

        public static Vector3 SwapYZ(Vector3 vector)
        {
            return new Vector3(vector.x, vector.z, vector.y);
        }

        public static Vector SwapYZ(Vector vector)
        {
            return new Vector(vector.X, vector.Z, vector.Y);
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
            const bool DEBUG_WALK = false;
            
            if (DEBUG_WALK) SafeHouse.Logger.Log("BodyOrbitsBody(" + a.name + "," + b.name + ")");
            if (DEBUG_WALK) SafeHouse.Logger.Log("a's ref body = " + (a.referenceBody == null ? "null" : a.referenceBody.name));
            Boolean found = false;
            for (var curBody = a.referenceBody;
                 curBody != null && curBody != curBody.referenceBody; // reference body of Sun points to itself, weirdly.
                 curBody = curBody.referenceBody)
            {
                if (DEBUG_WALK) SafeHouse.Logger.Log("curBody=" + curBody.name);
                if (!curBody.name.Equals(b.name)) continue;

                found = true;
                break;
            }
            return found;
        }
        
        /// <summary>
        /// Given any CSharp object, return the string name of the type in
        /// a way that makes more sense to kOS users, using kOS names rather
        /// than Csharp names.
        /// </summary>
        /// <param name="type">native c-sharp object</param>
        /// <returns>kOS name for this object</returns>
        public static string KOSType(Type type)
        {
            // This logic doesn't seem to work.
            // if the type is Int32, it still prints as
            // "Int32" not "Number", indicating that this
            // isn't quite working right:
            if (type.IsSubclassOf(typeof(Single)) ||
                type.IsSubclassOf(typeof(Double)) ||
                type.IsSubclassOf(typeof(Int32)) || type.IsSubclassOf(typeof(UInt32)) ||
                type.IsSubclassOf(typeof(Int64)) || type.IsSubclassOf(typeof(UInt64)) )
            {
                return "Number";
            }
            if (type.IsSubclassOf(typeof(Boolean)))
            {
                return "Boolean";
            }
            if (type.IsSubclassOf(typeof(String)))
            {
                return "String";
            }
            if (type.IsSubclassOf(typeof(Safe.Encapsulation.Structure)) )
            {
                // If it's one of our suffixed Types, then
                // first chop it down to just the lastmost term
                // in the fully qualified name:
                string name = type.Name;
                int lastDotPos = name.LastIndexOf('.');
                name = (lastDotPos < 0) ? name : name.Remove(0,lastDotPos);
                
                // Then drop the suffix "Target" or "Value", which we use a lot:
                name = name.Replace("Value","");
                name = name.Replace("Target","");

                return name;
            }
            // fallback to use the System's native type name:
            return type.Name;
        }

        /// <summary>
        /// Meant to be an override for stock KSP's CelestialBody.GetObtVelocity(), which (literally) always
        /// stack overflows because it's implemented as just infinite recursion without a base case.
        /// <br/>
        /// Returns the celestial body's velocity relative to the current universe's SOI body.  It's
        /// identical to body.orbit.GetVel() except that it also works for The Sun, which
        /// normally can't call that because it's orbit is null.
        /// </summary>
        /// <param name="body">The body to get the value for. (this will be hidden when this is an extension method of CelestialBody).</param>
        /// <param name="shared">Ubiquitous shared objects</param>
        /// <returns>body position in current unity world coords</returns>
        public static Vector3d KOSExtensionGetObtVelocity(this CelestialBody body, SharedObjects shared)
        {
            if (body.orbit != null)
                return body.orbit.GetVel();
            
            // When we can't use body.orbit, then manually perform the work that (probably) body.orbit.GetVel()
            // is doing itself.  This isn't DRY, but SQUAD made it impossible to be DRY when they didn't implement
            // the algorithm for the Sun so we have to repeat it again ourselves:

            // If we assume this happens when the body is the sun, then the sun's body is the
            // reference frame of the SOI's rotation            
            CelestialBody soiBody = shared.Vessel.mainBody;
            if (soiBody.orbit != null)
            {
                Vector3d wonkyAxesVel = soiBody.orbit.GetFrameVel();
                Vector3d correctedVel = new Vector3d(wonkyAxesVel.x, wonkyAxesVel.z, wonkyAxesVel.y); // have to swap axes because KSP API is weird.
                return -correctedVel; // invert direction because the above gives vel of my body rel to sun, and I want vel of sun rel to my body.
            }
            return (-1)*shared.Vessel.obt_velocity;
        }

        /// <summary>
        /// Return the parent body of this body, just like KSP's built-in referenceBody, except that
        /// it exhibits more sane behavior in the case of the Sun where there is no parent.  Default
        /// KSP's referenceBody will sometimes return null and sometimes return the Sun itself as
        /// the parent of the Sun.  This makes it always return null as the parent of the Sun no matter
        /// what.
        /// </summary>
        /// <param name="body">Body to get parent of (this will be hidden when called as an extension method)</param>
        /// <returns>parent body or null</returns>
        public static CelestialBody KOSExtensionGetParentBody(this CelestialBody body)
        {
            CelestialBody parent = body.referenceBody;            
            if (parent == body)
                parent = null;
            return parent;
        }

        /// <summary>
        /// Determines if a given case insensitive name corresponds with a defined resource, and
        /// outputs the case sensitive name for easy access from Squad's lists.
        /// </summary>
        /// <param name="insensitiveName">case insensitive name</param>
        /// <param name="fixedName">output case sensitive name</param>
        /// <returns>true if a matching resource definition is found</returns>
        public static bool IsResource(string insensitiveName, out string fixedName)
        {
            var defs = PartResourceLibrary.Instance.resourceDefinitions;
            // PartResourceDefinitionList's array index accessor uses the resource id
            // instead of as a list index, so we need to use an enumerator.
            foreach (var def in defs)
            {
                // loop through definitions looking for a case insensitive name match,
                // return true if a match is found
                if (def.name.Equals(insensitiveName, StringComparison.OrdinalIgnoreCase))
                {
                    fixedName = def.name;
                    return true;
                }
            }
            fixedName = insensitiveName;
            return false;
        }

        /// <summary>
        /// Determines if a given case insensitive name corresponds with a defined resource, and
        /// outputs the resource id for easy access from Squad's lists.
        /// </summary>
        /// <param name="insensitiveName">case insensitive name</param>
        /// <param name="foundId">output id of the found resource, zero if none found</param>
        /// <returns>true if a matching resource definition is found</returns>
        public static bool IsResource(string insensitiveName, out int foundId)
        {
            var defs = PartResourceLibrary.Instance.resourceDefinitions;
            // PartResourceDefinitionList's array index accessor uses the resource id
            // instead of as a list index, so we need to use an enumerator.
            foreach (var def in defs)
            {
                // loop through definitions looking for a case insensitive name match,
                // return true if a match is found
                if (def.name.Equals(insensitiveName, StringComparison.OrdinalIgnoreCase))
                {
                    foundId = def.id;
                    return true;
                }
            }
            foundId = 0;
            return false;
        }
    }
}
