using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    public class ElementValue : Structure
    {
        private readonly DockedVesselInfo dockedVesselInfo;
        private readonly SharedObjects shared;
        private readonly IList<global::Part> parts;

        public ListValue Parts { get { return PartValueFactory.Construct(parts, shared); } }

        public ElementValue(DockedVesselInfo dockedVesselInfo, SharedObjects shared)
        {
            this.dockedVesselInfo = dockedVesselInfo;
            this.shared = shared;
            parts = new List<global::Part>();

            InitializeSuffixes();
        }

        public void AddPart(global::Part part)
        {
            parts.Add(part);     
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new SetSuffix<string>(() => dockedVesselInfo.name, SetName ));
            AddSuffix("UID", new Suffix<string>(() => dockedVesselInfo.rootPartUId.ToString()));
            AddSuffix("PARTS", new Suffix<ListValue>(() => PartValueFactory.Construct(parts, shared)));
            AddSuffix("RESOURCES", new Suffix<ListValue>(GetResourceManifest));
        }

        private void SetName(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                dockedVesselInfo.name = value;
            }
        }

        private ListValue GetResourceManifest()
        {
            return PropellantFactory.PartsToList(parts, shared);
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects shared)
        {
            //We need to walk the part tree for this, not just the list. So we start with the vessel.
            Vessel vessel = parts.First().vessel;
            var elements = new Dictionary<uint,ElementValue>();

            Queue<ElementPair> queue = InitQueue(vessel, elements, shared);

            //Runs the queue over the part tree
            WorkQueue(queue, elements, shared);

            return ListValue.CreateList(elements.Values.ToList());
        }

        private static void WorkQueue(Queue<ElementPair> queue, Dictionary<uint, ElementValue> elements, SharedObjects shared)
        {
            var visitedFlightIds = new HashSet<uint>();

            while (queue.Any())
            {
                ElementPair pair = queue.Dequeue();
                if (AlreadyVisited(pair.Part, visitedFlightIds)) continue;

                var dockingNodes = pair.Part.Modules.OfType<ModuleDockingNode>().ToList();

                if (dockingNodes.Any())
                {
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
                                element = new ElementValue(info, shared);
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
        /// <param name="elements">The final element list, is it included here to have the root element added</param>
        /// <param name="shared">the ever present shared object</param>
        /// <returns>a queue with a starting entry</returns>
        private static Queue<ElementPair> InitQueue(Vessel vessel, Dictionary<uint, ElementValue> elements, SharedObjects shared)
        {
            var toReturn = new Queue<ElementPair>();

            var rootUid = vessel.rootPart.flightID;
            var rootInfo = new DockedVesselInfo
            {
                name = vessel.vesselName,
                rootPartUId = rootUid,
                vesselType = vessel.vesselType
            };
            var rootElement = new ElementValue(rootInfo, shared);
            elements.Add(rootUid, rootElement);
            toReturn.Enqueue(new ElementPair(rootElement, vessel.rootPart));
            return toReturn;
        }

        public override string ToString()
        {
            return string.Format("ELEMENT: {0} {1} Parts", dockedVesselInfo.name, parts.Count);
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