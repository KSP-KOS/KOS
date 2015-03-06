using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Execution;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using kOS.Suffixed.Part;
using Math = System.Math;

namespace kOS.Suffixed
{
    public class ResourceTransferValue : Structure
    {
        private const float RESOURCE_SHARE_PER_UPDATE = 0.001f;
        private enum TransferPartType
        {
            Part,
            Parts,
            Element
        }

        private readonly double? amount;
        private double transferedAmount;
        private readonly PartResourceDefinition resourceInfo;
        private readonly object transferTo;
        private TransferPartType transferToType;
        private readonly object transferFrom;
        private TransferPartType transferFromType;

        public string StatusMessage { get; private set; }
        public TransferManager.TransferStatus Status { get; private set; }

        public ResourceTransferValue(PartResourceDefinition resourceInfo, object transferTo, object transferFrom, double amount) :this (resourceInfo, transferTo, transferFrom)
        {
            this.amount = amount;
        }

        public ResourceTransferValue(PartResourceDefinition  resourceInfo, object transferTo, object transferFrom)
        {
            this.resourceInfo = resourceInfo;
            this.transferTo = transferTo;
            this.transferFrom = transferFrom;
            Status = TransferManager.TransferStatus.Transfering;

            DetermineTypes();
            InitializeSuffixes();
        }

        public void Update(double deltaTime)
        {
            if (Status != TransferManager.TransferStatus.Transfering) { return; }

            IList<global::Part> fromParts = GetParts(transferFromType, transferFrom);
            SafeHouse.Logger.Log("TRANSFER: FromParts Count: " + fromParts.Count);
            IList<global::Part> toParts = GetParts(transferFromType, transferTo);
            SafeHouse.Logger.Log("TRANSFER: ToParts Count: " + toParts.Count);

            if (!AllPartsAreConnected(fromParts, toParts)) { return; }
            SafeHouse.Logger.Log("TRANSFER: All Parts Connected");

            if (!CanTransfer(fromParts, toParts))
            {
                return;
            }
            WorkTransfer(fromParts, toParts);
        }

        public override string ToString()
        {
            return string.Format("Transfer( {0}, {1} )", Status, StatusMessage);
        }

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

        private void WorkTransfer(IList<global::Part> fromParts, IList<global::Part> toParts)
        {
            SafeHouse.Logger.Log("TRANSFER WORK: Have already transfered: " + transferedAmount);

            var transferGoal = CalculateTransferGoal(toParts);

            double pulledAmount = PullResources(fromParts, transferGoal);

            transferedAmount += pulledAmount;

            PutResources(toParts, pulledAmount);

            SafeHouse.Logger.Log("TRANSFER WORK: Have now transfered: " + transferedAmount);
        }

        /// <summary>
        /// Transfers resources into the specified parts 
        /// </summary>
        /// <param name="parts">All of the recipient parts</param>
        /// <param name="pulledAmount">the aggregate amount we are seeking to add to the parts</param>
        private void PutResources(IList<global::Part> parts, double pulledAmount)
        {
            var retries = 0;
            var evenShare = pulledAmount/parts.Count;

            var remaining = pulledAmount;
            while (remaining > 0)
            {
                if (retries > 10)
                {
                    MarkFailed("Error in putting resource with " + remaining + " remaining.");
                    break;
                }
                foreach (var part in parts)
                {
                    var resource = part.Resources.Get(resourceInfo.id);
                    if (resource == null) continue;

                    var transferAmount = Math.Min(remaining, evenShare);

                    SafeHouse.Logger.Log(string.Format("TRANSFER WORK: {0} PUT AMOUNT: {1}", part.flightID, transferAmount));

                    remaining += part.TransferResource(resource.info.id, transferAmount);

                    SafeHouse.Logger.Log(string.Format("TRANSFER WORK: {0} PUT REMAINING: {1}", part.flightID, remaining));
                }
                retries++;
            }
        }

        /// <summary>
        /// Requests the resource from all parts in the collection
        /// </summary>
        /// <param name="parts">All of the donor parts</param>
        /// <param name="transferGoal">the aggregate amount we are seeking to remove from parts</param>
        /// <returns>the amount we were successful at pulling</returns>
        private double PullResources(IList<global::Part> parts, double transferGoal)
        {
            double toReturn = 0.0;
            var availableResources = CalculateAvailableResource(parts);
            foreach (var part in parts)
            {
                var resource = part.Resources.Get(resourceInfo.id);
                if (resource == null) continue;

                var thisPartsPercentage = resource.amount/availableResources;

                // The amount you pull must be negative 
                var thisPartsShare = -transferGoal*thisPartsPercentage;

                SafeHouse.Logger.Log(string.Format("TRANSFER WORK: {0} PULL AMOUNT: {1}", part.flightID, thisPartsShare));

                toReturn += part.TransferResource(resourceInfo.id, thisPartsShare);

                SafeHouse.Logger.Log(string.Format("TRANSFER WORK: {0} PULLED RESOURCE: {1}", part.flightID, toReturn));
            }
            SafeHouse.Logger.Log("TRANSFER WORK: Pulled: " + toReturn);
            return toReturn;
        }

        private double CalculateTransferGoal(IEnumerable<global::Part> toParts)
        {
            double toReturn;

            var destinationAvailableCapacity = CalculateAvailableSpace(toParts);

            if (!amount.HasValue)
            {
                toReturn = destinationAvailableCapacity;
            }
            else
            {
                var rawGoal = amount.Value - transferedAmount; 
                toReturn = Math.Min(destinationAvailableCapacity, rawGoal);
            }

            return toReturn;
        }


        /// <summary>
        /// Tests to see if the transfer has reached its goal
        /// </summary>
        /// <returns>Can the transfer continue</returns>
        private bool CanTransfer(IEnumerable<global::Part> fromParts, IEnumerable<global::Part> toParts)
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

        private bool SourceReady(IEnumerable<global::Part> fromParts)
        {
            var sourceAvailable = CalculateAvailableResource(fromParts);
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

        private bool DestinationReady(IEnumerable<global::Part> toParts)
        {
            var destinationAvailableCapacity = CalculateAvailableSpace(toParts);
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
            Status = TransferManager.TransferStatus.Failed;
        }

        private void MarkFinished()
        {
            SafeHouse.Logger.Log(string.Format("TRANSFER FINISHED: Transfered {0} {1}", transferedAmount, resourceInfo.name));
            StatusMessage = string.Format("Transfered: {0}", transferedAmount);
            Status = TransferManager.TransferStatus.Finished;
        }

        private double CalculateMaxSpace(IEnumerable<global::Part> parts)
        {
            var resources = parts.SelectMany(p => p.Resources.GetAll(resourceInfo.id));
            return resources.Sum(r => r.maxAmount);
        }

        private double CalculateAvailableSpace(IEnumerable<global::Part> parts)
        {
            var resources = parts.SelectMany(p => p.Resources.GetAll(resourceInfo.id));
            var toReturn =  resources.Sum(r => r.maxAmount - r.amount);
            SafeHouse.Logger.Log("TRANSFER: Available Space: " + toReturn);
            return toReturn;
        }

        private double CalculateAvailableResource(IEnumerable<global::Part> fromParts)
        {
            var resources = fromParts.SelectMany(p => p.Resources.GetAll(resourceInfo.id));
            var toReturn = resources.Sum(r => r.amount);
            SafeHouse.Logger.Log("TRANSFER: Available Resource: " + toReturn);
            return toReturn;
        }

        private IList<global::Part> GetParts(TransferPartType type, object obj)
        {
            var parts = new List<global::Part>();
            switch (type)
            {
                case TransferPartType.Part:
                    var partValue = obj as PartValue;
                    parts.Add(partValue.Part);
                    break;
                case TransferPartType.Parts:
                    var partList = (obj as ListValue).Cast<PartValue>();
                    parts.AddRange(partList.Select(part => part.Part));
                    break;
                case TransferPartType.Element:
                    var element = obj as ElementValue;
                    var elementParts = element.Parts.Cast<PartValue>();
                    parts.AddRange(elementParts.Select(part => part.Part));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }

            return parts.Where(part => part.Resources != null && part.Resources.Contains(resourceInfo.id)).ToList();
        }

        private bool AllPartsAreConnected(IList<global::Part> fromParts, IList<global::Part> toParts)
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
        private Guid? GetVesselId(IList<global::Part> parts)
        {
            var vessel = parts[0].vessel;
            if (parts.All(p => p.vessel == vessel)) return vessel.id;

            // Some parts are from a different vessel? bail
            return null;
        }
    }
}