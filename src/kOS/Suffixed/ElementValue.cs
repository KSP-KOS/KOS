using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using UnityEngine;

namespace kOS.Suffixed
{
    public class ElementValue : Structure
    {
        private readonly DockedVesselInfo dockedVesselInfo;
        private readonly IList<global::Part> parts;
        private Color color;
        private static int colorIndex;
        private string colorName;

        public ElementValue(DockedVesselInfo dockedVesselInfo)
        {
            this.dockedVesselInfo = dockedVesselInfo;
            parts = new List<global::Part>();
            BuildColor();

            InitializeSuffixes();
        }

        private void BuildColor()
        {
            switch (colorIndex)
            {
                case 0:
                    color = Color.black;
                    colorName = "black";
                    break;
                case 1:
                    color = Color.black;
                    colorName = "black";
                    break;
                case 2:
                    color = Color.blue;
                    colorName = "blue";
                    break;
                case 3:
                    color = Color.yellow;
                    colorName = "yellow";
                    break;
                case 4:
                    color = Color.magenta;
                    colorName = "magenta";
                    break;
                case 5:
                    color = Color.green;
                    colorName = "green";
                    break;
                case 6:
                    color = Color.cyan;
                    colorName = "cyan";
                    break;
                case 7:
                    colorIndex = -1;
                    break;
            }
            colorIndex++;
        }

        public void AddPart(global::Part part)
        {
            parts.Add(part);     
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<string>(() => dockedVesselInfo.name));
            AddSuffix("UID", new Suffix<string>(() => dockedVesselInfo.rootPartUId.ToString()));
            AddSuffix("PARTS", new Suffix<ListValue>(() => PartsToList(parts)));
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts)
        {
            SafeHouse.Logger.Log("ELEMENTVALUE: START");
            //We need to walk the part tree for this, not just the list. So we start with the vessel.
            Vessel vessel = parts.First().vessel;
            var elements = new Dictionary<uint,ElementValue>();

            Queue<ElementPair> queue = InitQueue(vessel, elements);

            //Runs the queue over the part tree
            WorkQueue(queue, elements);

            vessel.rootPart.SetHighlightColor(Color.red);
            vessel.rootPart.SetHighlight(true, false);

            SafeHouse.Logger.Log("ELEMENTVALUE: STOP");
            return ListValue.CreateList(elements.Values.ToList());
        }

        private static void WorkQueue(Queue<ElementPair> queue, Dictionary<uint, ElementValue> elements)
        {
            var visitedFlightIds = new HashSet<uint>();

            while (queue.Any())
            {
                ElementPair pair = queue.Dequeue();
                SafeHouse.Logger.Log(string.Format("ELEMENTVALUE: Queue Pop: {0}:{1} {2}", pair.Part.partName, pair.Part.flightID, pair.Element));
                if (AlreadyVisited(pair.Part, visitedFlightIds)) continue;


                var dockingNodes = pair.Part.Modules.OfType<ModuleDockingNode>().ToList();

                if (dockingNodes.Any())
                {
                    foreach (var dockingNode in dockingNodes)
                    {
                        ElementValue element;
                        if (dockingNode.vesselInfo == null)
                        {
                            if (pair.Part.children.Any())
                            {
                                SafeHouse.Logger.Log("ELEMENTVALUE: blarg docking node with no info and children?");
                            }

                            element = pair.Element;
                        }
                        else
                        {

                            DockedVesselInfo info = dockingNode.vesselInfo;

                            if (!elements.TryGetValue(info.rootPartUId, out element))
                            {
                                SafeHouse.Logger.Log("ELEMENTVALUE: New Element: " + info.rootPartUId);
                                element = new ElementValue(info);
                                elements.Add(info.rootPartUId, element);
                            }
                        }

                        element.AddPart(pair.Part);
                        EnqueueChildren(queue, element, pair.Part);
                    }
                }
                else
                {
                    pair.Element.AddPart(pair.Part);
                    EnqueueChildren(queue, pair.Element, pair.Part);                    
                }

            }
        }

        private static void EnqueueChildren(Queue<ElementPair> queue, ElementValue element, global::Part part)
        {
            part.SetHighlightColor(element.Color);
            part.SetHighlight(true, false);

            if (!part.children.Any()) return;

            foreach (var child in part.children)
            {
                queue.Enqueue(new ElementPair(element,child));
            }
        }

        public Color Color
        {
            get { return color; }
        }

        private static bool AlreadyVisited(global::Part part, HashSet<uint> visitedFlightIds)
        {
            if (visitedFlightIds.Contains(part.flightID))
            {
                return true;
            }
            visitedFlightIds.Add(part.flightID);
            return false;
        }

        /// <summary>
        /// Builds the first element in the queue
        /// </summary>
        /// <param name="vessel">The vessel that you want to crawl for elements</param>
        /// <param name="elements"></param>
        /// <returns>a queue with a starting entry</returns>
        private static Queue<ElementPair> InitQueue(Vessel vessel, Dictionary<uint, ElementValue> elements)
        {
            var toReturn = new Queue<ElementPair>();

            var rootUid = vessel.rootPart.flightID;
            var rootInfo = new DockedVesselInfo
            {
                name = vessel.vesselName,
                rootPartUId = rootUid,
                vesselType = vessel.vesselType
            };
            var rootElement = new ElementValue(rootInfo);
            elements.Add(rootUid, rootElement);
            toReturn.Enqueue(new ElementPair(rootElement, vessel.rootPart));
            return toReturn;
        }

        public override string ToString()
        {
            return "ELEMENT( " + dockedVesselInfo.name + ", " + parts.Count + ", " + colorName + " )" ;
        }
    }

    public struct ElementPair
    {
        public ElementValue Element { get; private set; }
        public global::Part Part { get; private set; }

        public ElementPair(ElementValue element, global::Part part) : this()
        {
            Element = element;
            Part = part;
        }
    }

}