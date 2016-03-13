using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using kOS.Serialization;
using kOS.Safe;

namespace kOS.Communication
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames | ScenarioCreationOptions.AddToExistingGames, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
    public class InterVesselManager : ScenarioModule
    {
        private const string Id = "id";
        private static string VesselQueue = "vesselQueue";
        private static string MessageQueue = "messageQueue";

        private Dictionary<string, MessageQueue> vesselQueues;

        public static InterVesselManager Instance { get; private set; }

        static InterVesselManager() {
            // normally we do this in SerializationMgr, but KSPScenarios run before we create any instances
            SafeSerializationMgr.AddAssembly(typeof(SerializationMgr).Assembly.FullName);
        }

        public InterVesselManager()
        {
        }

        public override void OnLoad(ConfigNode node)
        {
            Instance = this;
            vesselQueues = new Dictionary<string, MessageQueue>();

            foreach (ConfigNode subNode in node.GetNodes())
            {
                if (subNode.name.Equals(VesselQueue))
                {
                    string id = subNode.GetValue(Id);

                    ConfigNode queueNode = subNode.GetNode(MessageQueue);

                    Dump queueDump = ConfigNodeFormatter.Instance.FromConfigNode(queueNode);

                    MessageQueue queue = new SafeSerializationMgr().CreateFromDump(queueDump) as MessageQueue;

                    if (queue.Count() > 0)
                    {
                        vesselQueues[id] = queue;
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                string id = vessel.id.ToString();
                if (vesselQueues.ContainsKey(id) && vesselQueues[id].Count() > 0)
                {
                    ConfigNode vesselEntry = new ConfigNode(VesselQueue);
                    vesselEntry.AddValue(Id, id);

                    ConfigNode queueNode = ConfigNodeFormatter.Instance.ToConfigNode(new SafeSerializationMgr().Dump(vesselQueues[id]));
                    queueNode.name = MessageQueue;
                    vesselEntry.AddNode(queueNode);

                    node.AddNode(vesselEntry);
                }
            }
        }

        public MessageQueueStructure GetQueue(Vessel vessel, SharedObjects sharedObjects)
        {
            string vesselId = vessel.id.ToString();

            if (!vesselQueues.ContainsKey(vesselId))
            {
                vesselQueues.Add(vesselId, new MessageQueue());
            }

            return new MessageQueueStructure(vesselQueues[vesselId], sharedObjects);
        }
    }
}

