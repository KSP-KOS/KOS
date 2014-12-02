using kOS.Safe.Compilation;
using kOS.Suffixed;
using System;
using System.Collections.Generic;
using System.Linq;
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
                           MapView.MapCamera.camera : FlightCamera.fetch.mainCamera);
        }
        
        public static string GetAssemblyFileVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            // Fully qualified name used instead of "using" here because using System.Diagnostics causes ambiguities
            // with all the Debug.Log's:
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;            
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
                // The recursive algorithm is written to assume all the engines are on
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

        public static GUISkin GetSkinCopy(GUISkin toCopy)
        {
            // If we just did something like this:
            //     theSkin = HighLogic.Skin;
            //     theSkin.label.fontSize=10;
            // Then theSkin would have just been a reference to the same skin
            // everybody else uses in KSP and therefore it would have altered
            // the look of all the KSP windows from stock and all the other mods.
            // (Yes, this is what happened the first time I tried this).
            //
            // Therefore we want to make theSkin a deep copy of HighLogic.Skin, then
            // alter it after making the copy, but GUISkin has no copy constructor.
            // The individual GUIStyle's inside it do, however, and that is what
            // causes this next block of code.  It's starting off theSkin as a deep
            // copy of HighLogic.Skin, at least as much as it can:
            //
            return new GUISkin
            {
                // This is literally every GUISTyle mentioned in Unity's documention for
                // GUISkin, as of 11/11/2014.  If Unity updates these, we could be screwed:
                //
                box = new GUIStyle(toCopy.box),
                button = new GUIStyle(toCopy.button),
                horizontalScrollbar = new GUIStyle(toCopy.horizontalScrollbar),
                horizontalScrollbarLeftButton = new GUIStyle(toCopy.horizontalScrollbarLeftButton),
                horizontalScrollbarRightButton = new GUIStyle(toCopy.horizontalScrollbarRightButton),
                horizontalScrollbarThumb = new GUIStyle(toCopy.horizontalScrollbarThumb),
                horizontalSlider = new GUIStyle(toCopy.horizontalSlider),
                horizontalSliderThumb = new GUIStyle(toCopy.horizontalSliderThumb),
                label = new GUIStyle(toCopy.label),
                scrollView = new GUIStyle(toCopy.scrollView),
                textArea = new GUIStyle(toCopy.textArea),
                textField = new GUIStyle(toCopy.textField),
                toggle = new GUIStyle(toCopy.toggle),
                verticalScrollbar = new GUIStyle(toCopy.verticalScrollbar),
                verticalScrollbarDownButton = new GUIStyle(toCopy.verticalScrollbarDownButton),
                verticalScrollbarThumb = new GUIStyle(toCopy.verticalScrollbarThumb),
                verticalScrollbarUpButton = new GUIStyle(toCopy.verticalScrollbarUpButton),
                verticalSlider = new GUIStyle(toCopy.verticalSlider),
                verticalSliderThumb = new GUIStyle(toCopy.verticalSliderThumb),
                window = new GUIStyle(toCopy.window),
            };
        }

        /// <summary>
        /// Given a Vector3, construct a new Vector3D out of it.
        /// By all rights SQUAD should have had this as a constructor in their Vector3d class.  I don't know why they didn't.
        /// </summary>
        /// <param name="convertFrom">The Vector3 to convert</param>
        /// <returns>A Vector3d that has the same values as the Vector3 passed in.</returns>
        public static Vector3d Vector3ToVector3d(Vector3 convertFrom)
        {
            return new Vector3d( convertFrom.x, convertFrom.y, convertFrom.z);
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
            
            if (DEBUG_WALK) Debug.Log("BodyOrbitsBody(" + a.name + "," + b.name + ")");
            if (DEBUG_WALK) Debug.Log("a's ref body = " + (a.referenceBody == null ? "null" : a.referenceBody.name));
            Boolean found = false;
            for (var curBody = a.referenceBody;
                 curBody != null && curBody != curBody.referenceBody; // reference body of Sun points to itself, weirdly.
                 curBody = curBody.referenceBody)
            {
                if (DEBUG_WALK) Debug.Log("curBody=" + curBody.name);
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
            else if (type.IsSubclassOf(typeof(Boolean)))
            {
                return "Boolean";
            }
            else if (type.IsSubclassOf(typeof(String)))
            {
                return "String";
            }
            else if (type.IsSubclassOf(typeof(Safe.Encapsulation.Structure)) )
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
            else // fallback to use the System's native type name:
            {
                return type.Name;
            }
                
        }

        /// <summary>
        /// This is copied almost verbatim from ProgramContext,
        /// It's here to help debug.
        /// </summary>
        public static string GetCodeFragment(List<Opcode> codes)
        {
            var codeFragment = new List<string>();
            
            const string FORMAT_STR = "{0,-20} {1,4}:{2,-3} {3:0000} {4} {5} {6} {7}";
            codeFragment.Add(string.Format(FORMAT_STR, "File", "Line", "Col", "IP  ", "Label  ", "opcode", "operand", "Destination" ));
            codeFragment.Add(string.Format(FORMAT_STR, "----", "----", "---", "----", "-------", "---------------------", "", "" ));

            for (int index = 0; index < codes.Count; index++)
            {
                codeFragment.Add(string.Format(FORMAT_STR,
                                               codes[index].SourceName ?? "null",
                                               codes[index].SourceLine,
                                               codes[index].SourceColumn ,
                                               index,
                                               codes[index].Label ?? "null",
                                               codes[index] ?? new OpcodeBogus(),
                                               "DEST: " + (codes[index].DestinationLabel ?? "null" ),
                                               "" ) );
            }

            return codeFragment.Aggregate(string.Empty, (current, s) => current + (s + "\n"));
        }
                
    }
}