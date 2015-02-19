using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    public class ElementValue : Structure
    {
        private readonly DockedVesselInfo dockedVesselInfo;
        private readonly IList<global::Part> parts;

        public ElementValue(DockedVesselInfo dockedVesselInfo)
        {
            this.dockedVesselInfo = dockedVesselInfo;
            parts = new List<global::Part>();


            InitializeSuffixes();
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
            Safe.Utilities.Debug.Logger.Log("ELEMENTVALUE: START");
            //We need to walk the part tree for this, not just the list. So we start with the vessel.
            Vessel vessel = parts.First().vessel;
            Queue<ElementPair> queue = InitQueue(vessel);

            //Runs the queue over the part tree
            IList<ElementValue> elements = WorkQueue(queue);

            Safe.Utilities.Debug.Logger.Log("ELEMENTVALUE: STOP");
            return ListValue.CreateList(elements);
        }

        private static IList<ElementValue> WorkQueue(Queue<ElementPair> queue)
        {
            var elements = new Dictionary<uint,ElementValue>();
            var visitedFlightIds = new HashSet<uint>();

            while (queue.Any())
            {
                ElementPair pair = queue.Dequeue();
                Safe.Utilities.Debug.Logger.Log("ELEMENTVALUE: Queue Pop: " + pair.Part.flightID + " " + pair.Element);
                if (AlreadyVisited(pair.Part, visitedFlightIds)) continue;


                var dockingNodes = pair.Part.Modules.OfType<ModuleDockingNode>().ToList();


                if (dockingNodes.Any())
                {
                    Safe.Utilities.Debug.Logger.Log("ELEMENTVALUE: PART HAS DOCKING NODE: " + dockingNodes.Count);
                    foreach (var dockingNode in dockingNodes)
                    {
                        ElementValue element;
                        if (dockingNode.vesselInfo == null)
                        {
                            element = pair.Element;
                        }
                        else
                        {
                            DockedVesselInfo info = dockingNode.vesselInfo;

                            if (!elements.TryGetValue(info.rootPartUId, out element))
                            {
                                Safe.Utilities.Debug.Logger.Log("ELEMENTVALUE: New Element: " + info.rootPartUId);
                                element = new ElementValue(info);
                                elements.Add(info.rootPartUId, element);
                            }
                        }

                        EnqueueChildren(queue, element, pair.Part);
                    }
                }
                else
                {
                    EnqueueChildren(queue, pair.Element, pair.Part);                    
                }

            }
            return elements.Values.ToList();
        }

        private static void EnqueueChildren(Queue<ElementPair> queue, ElementValue element, global::Part part)
        {
            Safe.Utilities.Debug.Logger.Log("ELEMENTVALUE: ADDING CHILDREN: " + part.children.Count);

            element.AddPart(part);
            if (!part.children.Any()) return;

            foreach (var child in part.children)
            {
                queue.Enqueue(new ElementPair(element,child));
            }
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
        /// <returns>a queue with a starting entry</returns>
        private static Queue<ElementPair> InitQueue(Vessel vessel)
        {
            var toReturn = new Queue<ElementPair>();

            var rootInfo = new DockedVesselInfo
            {
                name = vessel.vesselName,
                rootPartUId = vessel.rootPart.flightID,
                vesselType = vessel.vesselType
            };
            toReturn.Enqueue(new ElementPair(new ElementValue(rootInfo), vessel.rootPart));
            return toReturn;
        }

        public override string ToString()
        {
            return "ELEMENT(" + dockedVesselInfo.name + ", " + parts.Count + ")";
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