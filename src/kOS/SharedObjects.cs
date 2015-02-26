using System;
using System.Collections.Generic;
using System.Linq;
using kOS.InterProcessor;
using kOS.Binding;
using kOS.Factories;
using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using kOS.Screen;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using Math = System.Math;

namespace kOS
{
    public class SharedObjects : Safe.SharedObjects
    {
        public Vessel Vessel { get; set; }
        public BindingManager BindingMgr { get; set; }  
        public ProcessorManager ProcessorMgr { get; set; }
        public IFactory Factory { get; set; }
        public Part KSPPart { get; set; }
        public TermWindow Window { get; set; }
        public TransferManager TransferManager { get; set; }

        public SharedObjects()
        {
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        }

        private void OnVesselDestroy(Vessel data)
        {
            if (data.id == Vessel.id)
            {
                BindingMgr.Dispose();
            }
        }
    }

    public class TransferManager : IUpdateObserver
    {
        private readonly SharedObjects shared;
        private readonly List<ResourceTransferValue> transfers;

        public TransferManager(SharedObjects shared)
        {
            this.shared = shared;
            transfers = new List<ResourceTransferValue>();
            shared.UpdateHandler.AddObserver(this);
        }

        public List<ResourceTransferValue> Transfers
        {
            get { return transfers; }
        }

        public ResourceTransferValue CreateTransfer(PartResourceDefinition resourceInfo, object transferTo, object transferFrom, double amount)
        {
            var toReturn = new ResourceTransferValue(resourceInfo, transferTo, transferFrom, amount);
            transfers.Add(toReturn);
            return toReturn;
        }

        public ResourceTransferValue CreateTransfer(PartResourceDefinition resourceInfo, object transferTo, object transferFrom)
        {
            var toReturn = new ResourceTransferValue(resourceInfo, transferTo, transferFrom);
            transfers.Add(toReturn);
            return toReturn;
        }

        public void Dispose()
        {
            shared.UpdateHandler.RemoveObserver(this);
        }

        public void Update(double deltaTime)
        {
            var finished = new List<ResourceTransferValue>();
            foreach (var transfer in Transfers)
            {
                transfer.Update(deltaTime);
                if (transfer.Finished)
                {
                    finished.Add(transfer);
                }
            }
            Transfers.RemoveAll(finished.Contains);
        }
    }

    public class ResourceTransferValue : Structure
    {
        private enum TransferPartType
        {
            Part,
            Parts,
            Element
        }

        private readonly double? amount;
        private readonly double transferedAmount;
        private readonly PartResourceDefinition resourceInfo;
        private readonly object transferTo;
        private TransferPartType transferToType;
        private readonly object transferFrom;
        private TransferPartType transferFromType;

        public string StatusMessage { get; private set; }
        public TransferStatus Status { get; private set; }

        public ResourceTransferValue(PartResourceDefinition resourceInfo, object transferTo, object transferFrom, double amount) :this (resourceInfo, transferTo, transferFrom)
        {
            this.amount = amount;
        }

        public ResourceTransferValue(PartResourceDefinition  resourceInfo, object transferTo, object transferFrom)
        {
            this.resourceInfo = resourceInfo;
            this.transferTo = transferTo;
            this.transferFrom = transferFrom;
            DetermineTypes();
            InitializeSuffixes();
        }

        public bool Finished { get; private set; }

        private void InitializeSuffixes()
        {
            AddSuffix("AMOUNT", new Suffix<double>(() => amount.HasValue ? amount.Value : -1));
            AddSuffix("TRANSFEREDAMOUNT", new Suffix<double>(() => transferedAmount));
            AddSuffix("STATUS", new Suffix<string>(() => Status.ToString()));
            AddSuffix("MESSAGE", new Suffix<string>(() => StatusMessage));
            AddSuffix("RESOURCE", new Suffix<string>(() => resourceInfo.name));
        }

        private void DetermineTypes()
        {
            transferToType = DetermineType(transferTo);
            transferFromType = DetermineType(transferFrom);
            SafeHouse.Logger.Log("TRANSFER: To Type: " + transferToType);
            SafeHouse.Logger.Log("TRANSFER: From Type: " + transferFromType);
        }

        private TransferPartType DetermineType(object toTest)
        {
            if (toTest is PartValue)
            {
                return TransferPartType.Part;
            }
            if (toTest is ListValue)
            {
                return TransferPartType.Parts;
            }
            if (toTest is ElementValue)
            {
                return TransferPartType.Element;
            }
            throw new ArgumentOutOfRangeException();
        }

        public void Update(double deltaTime)
        {
            if (Status != TransferStatus.Transfering) { return; }

            IList<Part> fromParts = GetParts(transferFromType, transferFrom);
            IList<Part> toParts = GetParts(transferFromType, transferFrom);
            if (!AllPartsAreConnected(fromParts, toParts)) { return; }
            SafeHouse.Logger.Log("TRANSFER: All Parts Connected");

            if (!CanTransfer(fromParts, toParts))
            {
                return;
            }
            WorkTransfer(fromParts, toParts);
        }

        private void WorkTransfer(IList<Part> fromParts, IList<Part> toParts)
        {
            SafeHouse.Logger.Log("TRANSFER WORK: Have already transfered: " +transferedAmount);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests to see if the transfer has reached its goal
        /// </summary>
        /// <returns>Can the transfer continue</returns>
        private bool CanTransfer(IEnumerable<Part> fromParts, IEnumerable<Part> toParts)
        {
            if (!DestinationReady(toParts)) return false;
            if (!SourceReady(fromParts)) return false;

            if (amount.HasValue)
            {
                if (Math.Abs(amount.Value - transferedAmount) < 0.00001)
                {
                    MarkFinished();               
                    return false;
                }
            }

            // We aren't finished yet!
            return true;
        }

        private bool SourceReady(IEnumerable<Part> fromParts)
        {
            var sourceAvailable = AvailableResource(fromParts);
            if (Math.Abs(sourceAvailable) < 0.0001)
            {
                // Nothing to transfer
                if (!amount.HasValue)
                {
                    MarkFinished();
                }
                else
                {
                    MarkFailed("Source is out of " + resourceInfo.name);
                }
                return false;
            }
            return true;
        }

        private bool DestinationReady(IEnumerable<Part> toParts)
        {
            var destinationAvailableCapacity = AvailableSpace(toParts);
            if (Math.Abs(destinationAvailableCapacity) < 0.0001)
            {
                // No room at the inn
                if (!amount.HasValue)
                {
                    MarkFinished();
                }
                else
                {
                    MarkFailed("Destination is out of space");
                }
                return false;
            }
            return true;
        }

        private void MarkFailed(string message)
        {
            SafeHouse.Logger.Log("TRANSFER FAILED: " + message);
            StatusMessage = message;
            Status = TransferStatus.Failed;
        }

        private void MarkFinished()
        {
            SafeHouse.Logger.Log(string.Format("TRANSFER FINISHED: Transfered {0} {1}", transferedAmount, resourceInfo.name));
            StatusMessage = string.Format("Transfered: {0}", transferedAmount);
            Status = TransferStatus.Finished;
        }

        private double AvailableSpace(IEnumerable<Part> parts)
        {
            var resources = parts.SelectMany(p => p.Resources.GetAll(resourceInfo.id));
            return resources.Sum(r => r.maxAmount = r.amount);
        }

        private double AvailableResource(IEnumerable<Part> fromParts)
        {
            var resources = fromParts.SelectMany(p => p.Resources.GetAll(resourceInfo.id));
            return resources.Sum(r => r.amount);
        }

        private IList<Part> GetParts(TransferPartType type, object obj)
        {
            IList<Part> toReturn = new List<Part>();
            switch (type)
            {
                case TransferPartType.Part:
                    var partValue = obj as PartValue;
                    toReturn.Add(partValue.Part);
                    break;
                case TransferPartType.Parts:
                    var partList = (obj as ListValue).Cast<PartValue>();
                    foreach (var part in partList)
                    {
                        toReturn.Add(part.Part);
                    }
                    break;
                case TransferPartType.Element:
                    var element = obj as ElementValue;
                    var elementParts = element.Parts.Cast<PartValue>();
                    foreach (var part in elementParts)
                    {
                        toReturn.Add(part.Part);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
            return toReturn;
        }

        private bool AllPartsAreConnected(IList<Part> fromParts, IList<Part> toParts)
        {
            var fromFlightId = GetVesselId(fromParts);
            var toFlightId = GetVesselId(toParts);

            if (!fromFlightId.HasValue)
            {
                MarkFailed("Not all From parts are connected");
                return false;
            }
            if (!toFlightId.HasValue)
            {
                MarkFailed("Not all To parts are connected");
                return false;
            }
            if (fromFlightId != toFlightId)
            {
                MarkFailed("To and From are not connected");
                return false;
            }
            return true;
        }

        /// <summary>
        /// takes a list of parts and determines if they have a common vessel id
        /// </summary>
        /// <param name="parts">the list of parts to mine for a vessel id</param>
        /// <returns>either the vessel id that is common to all parts, or null</returns>
        private Guid? GetVesselId(IList<Part> parts)
        {
            var vessel = parts[0].vessel;
            if (parts.All(p => p.vessel == vessel)) return vessel.id;

            // Some parts are from a different vessel? bail
            return null;
        }
    }

    public enum TransferStatus        
    {
        Failed,
        Finished,
        Paused,
        Transfering
    }
}