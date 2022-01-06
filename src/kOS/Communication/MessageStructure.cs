using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Safe;
using kOS.Serialization;
using kOS.Safe.Serialization;
using kOS.Safe.Exceptions;

namespace kOS.Communication
{
    [kOS.Safe.Utilities.KOSNomenclature("Message")]
    public class MessageStructure : Structure
    {
        private const string DumpMessage = "message";

        public Message Message { get; private set; }
        private SharedObjects shared;

        public SharedObjects Shared
        {
            set
            {
                shared = value;
            }
        }

        public MessageStructure(Message message, SharedObjects shared)
        {
            Message = message;
            this.shared = shared;

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("SENTAT", new Suffix<kOS.Suffixed.TimeStamp>(() => new kOS.Suffixed.TimeStamp(Message.SentAt)));
            AddSuffix("RECEIVEDAT", new Suffix<kOS.Suffixed.TimeStamp>(() => new kOS.Suffixed.TimeStamp(Message.ReceivedAt)));
            AddSuffix("SENDER", new Suffix<Structure>(GetVesselTarget));
            AddSuffix("HASSENDER", new Suffix<BooleanValue>(GetVesselExists));
            AddSuffix("CONTENT", new Suffix<Structure>(DeserializeContent));
        }

        public Vessel GetVessel()
        {
            return (FlightGlobals.Vessels.Find((v) => v.id.ToString().Equals(Message.Vessel)));
        }

        public Structure GetVesselTarget()
        {
            Vessel vessel = GetVessel();

            if (vessel == null)
            {
                return new BooleanValue(false);
            }

            return VesselTarget.CreateOrGetExisting(vessel, shared);
        }
        
        public BooleanValue GetVesselExists()
        {
            return new BooleanValue((GetVessel() != null));
        }

        public Structure DeserializeContent()
        {
            return Message.Content;        }

        public override string ToString()
        {
            return "Message(" + Message.Vessel.ToString() + ")";
        }

        [DumpDeserializer]
        public static MessageStructure CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            var message = Message.CreateFromDump(d.GetDump(DumpMessage) as DumpDictionary, shared);
            return new MessageStructure(message, shared as SharedObjects);
        }

        [DumpPrinter]
        public static void Print(DumpDictionary d, IndentedStringBuilder sb)
        {
            Message.Print(d, sb);
        }
    }
}

