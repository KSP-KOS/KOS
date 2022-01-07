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

        private Dictionary<string, DumpList> vesselQueueDumps;
        private Dictionary<string, MessageQueue> vesselQueues;

        public static InterVesselManager Instance { get; private set; }

        public InterVesselManager()
        {
        }

        public override void OnLoad(ConfigNode node)
        {
            Instance = this;
            vesselQueueDumps = new Dictionary<string, DumpList>();
            vesselQueues = new Dictionary<string, MessageQueue>();

            foreach (ConfigNode subNode in node.GetNodes())
            {
                if (subNode.name.Equals(VesselQueue))
                {
                    string id = subNode.GetValue(Id);

                    ConfigNode queueNode = subNode.GetNode(MessageQueue);

                    DumpList queueDump = ConfigNodeFormatter.FromConfigNode(queueNode);
                    vesselQueueDumps[id] = queueDump;
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

                    ConfigNode queueNode = ConfigNodeFormatter.ToConfigNode(new SafeSerializationMgr(null).Dump(vesselQueues[id]));
                    queueNode.name = MessageQueue;
                    vesselEntry.AddNode(queueNode);

                    node.AddNode(vesselEntry);
                }
            }
        }

        public MessageQueueStructure GetQueue(Vessel vessel, SharedObjects sharedObjects)
        {
            string vesselId = vessel.id.ToString();
            var queue = new MessageQueue();

            if (vesselQueues.ContainsKey(vesselId))
            {
                queue = vesselQueues[vesselId];
            } else
            {
                if (vesselQueueDumps.ContainsKey(vesselId))
                {
                    queue = MessageQueue.;
                    vesselQueueDumps.Remove(vesselId);
                }
                vesselQueues.Add(vesselId, queue);
            }

            return new MessageQueueStructure(vesselQueues[vesselId], sharedObjects);
        }
    }
}

