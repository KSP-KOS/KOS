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
    [kOS.Safe.Utilities.KOSNomenclature("Transfer")]
    public class ResourceTransferValue : Structure
    {
        private const float RESOURCE_SHARE_PER_UPDATE = 0.005f;
        private enum TransferPartType
        {
            Part,
            Parts,
            Element
        }

        private readonly double? amount;
        private double transferredAmount;
        private readonly TransferManager transferManager;
        private readonly PartResourceDefinition resourceInfo;
        private readonly object transferTo;
        private TransferPartType transferToType;
        private readonly object transferFrom;
        private TransferPartType transferFromType;
        private TransferManager.TransferStatus status;

        public string StatusMessage { get; private set; }

        public TransferManager.TransferStatus Status
        {
            get { return status; }
            private set
            {
                status = value;
                transferManager.ReregisterTransfer(this);
                SafeHouse.Logger.Log(StatusChangeMessage());
            }
        }

        public ResourceTransferValue(TransferManager transferManager, PartResourceDefinition resourceInfo, object transferTo, object transferFrom, double amount) :this (transferManager, resourceInfo, transferTo, transferFrom)
        {
            this.amount = amount;
        }

        public ResourceTransferValue(TransferManager transferManager, PartResourceDefinition resourceInfo, object transferTo, object transferFrom)
        {
            this.transferManager = transferManager;
            this.resourceInfo = resourceInfo;
            this.transferTo = transferTo;
            this.transferFrom = transferFrom;

            DetermineTypes();
            InitializeSuffixes();

            Status = TransferManager.TransferStatus.Inactive; // Last because the setter for Status prints some of the values calculated above to the log
        }

        public void Update(double deltaTime)
        {
            if (Status != TransferManager.TransferStatus.Transferring) { return; }

            IList<global::Part> fromParts = GetParts(transferFromType, transferFrom);
            IList<global::Part> toParts = GetParts(transferFromType, transferTo);

            if (!AllPartsAreConnected(fromParts, toParts)) { return; }

            if (!CanTransfer(fromParts, toParts))
            {
                return;
            }
            WorkTransfer(fromParts, toParts, deltaTime);
        }

        public override string ToString()
        {
            return string.Format("{0}(\"{1}\", {2}, {3}{4})",
                                 amount.HasValue ? "TRANSFER" : "TRANSFERALL",
                                 resourceInfo.name, transferFromType, transferToType,
                                 amount.HasValue ? (", " + amount) : "" );
        }

        private void InitializeSuffixes()
        {
            AddSuffix("GOAL", new Suffix<ScalarValue>(() => amount ?? -1));
            AddSuffix("TRANSFERRED", new Suffix<ScalarValue>(() => transferredAmount));
            AddSuffix("STATUS", new Suffix<StringValue>(() => Status.ToString()));
            AddSuffix("MESSAGE", new Suffix<StringValue>(() => StatusMessage));
            AddSuffix("RESOURCE", new Suffix<StringValue>(() => resourceInfo.name));
            AddSuffix("ACTIVE", new SetSuffix<BooleanValue>(() => Status == TransferManager.TransferStatus.Transferring, 
            value => {
                Status = value ? TransferManager.TransferStatus.Transferring : TransferManager.TransferStatus.Inactive;
            }));
        }
        
        private string StatusChangeMessage()
        {
            return string.Format("{0} is now {1}.", ToString(), Status);
        }

        private void DetermineTypes()
        {
            transferToType = DetermineType(transferTo);
            transferFromType = DetermineType(transferFrom);
        }

        private TransferPartType DetermineType(object toTest)
        {
            if (toTest is PartValue)
            {
                return TransferPartType.Part;
            }
            if (toTest is ListValue || toTest is ListValue<PartValue> || toTest is List<PartValue>)
            {
                return TransferPartType.Parts;
            }
            if (toTest is ElementValue)
            {
                return TransferPartType.Element;
            }
            throw new ArgumentOutOfRangeException("toTest", @"Type: " + toTest.GetType());
        }

        private void WorkTransfer(IList<global::Part> fromParts, IList<global::Part> toParts, double deltaTime)
        {
            var transferGoal = CalculateTransferGoal(toParts);

            double pulledAmount = PullResources(fromParts, transferGoal, deltaTime);

            PutResources(toParts, pulledAmount);

            transferredAmount += pulledAmount;

            if (Status == TransferManager.TransferStatus.Transferring)
            {
                StatusMessage = string.Format("Transferred: {0}", transferredAmount);
            }
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
            while (remaining > 0.0001)
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

                    remaining += part.TransferResource(resource.info.id, transferAmount);
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
        private double PullResources(IList<global::Part> parts, double transferGoal, double deltaTime)
        {
            double toReturn = 0.0;
            var availableResources = CalculateAvailableResource(parts);
            foreach (var part in parts)
            {
                var resource = part.Resources.Get(resourceInfo.id);
                if (resource == null) continue;

                var thisPartsPercentage = resource.amount/availableResources;

                // Throttle the transfer
                var thisPartsShare = transferGoal*thisPartsPercentage;
                var thisPartsRate = resource.maxAmount*RESOURCE_SHARE_PER_UPDATE*deltaTime/0.02;
                
                // The amount you pull must be negative 
                thisPartsShare = -Math.Min(thisPartsShare, thisPartsRate);
                // the amount is subject to floating point lameness, if we round it here it is not material to the request but should make the numbers work out nicer.
                thisPartsShare = Math.Round(thisPartsShare, 5);

                toReturn += part.TransferResource(resourceInfo.id, thisPartsShare);
            }
            return toReturn;
        }

        private double CalculateTransferGoal(IEnumerable<global::Part> toParts)
        {
            double toReturn;

            var destinationAvailableCapacity = CalculateAvailableSpace(toParts);

            if (amount.HasValue)
            {
                var rawGoal = amount.Value - transferredAmount; 
                toReturn = Math.Min(destinationAvailableCapacity, rawGoal);
            }
            else
            {
                toReturn = destinationAvailableCapacity;
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
                if (Math.Abs(amount.Value - transferredAmount) < 0.00001)
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
            SafeHouse.Logger.LogError(message);
            StatusMessage = message;
            Status = TransferManager.TransferStatus.Failed;
        }

        private void MarkFinished()
        {
            transferredAmount = Math.Round(transferredAmount, 5);
            StatusMessage = string.Format("Transferred: {0}", transferredAmount);
            Status = TransferManager.TransferStatus.Finished;
        }

        private double CalculateAvailableSpace(IEnumerable<global::Part> parts)
        {
            var resources = new List<PartResource>();
            // GetAll no longer returns a list, but instead fills an existing list
            foreach (var part in parts)
            {
                part.Resources.GetAll(resources, resourceInfo.id);
            }
            var toReturn =  resources.Sum(r => r.maxAmount - r.amount);
            return toReturn;
        }

        private double CalculateAvailableResource(IEnumerable<global::Part> fromParts)
        {
            var resources = new List<PartResource>();
            // GetAll no longer returns a list, but instead fills an existing list
            foreach (var part in fromParts)
            {
                part.Resources.GetAll(resources, resourceInfo.id);
            }
            var toReturn = resources.Sum(r => r.amount);
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
                    var listValue = (obj as ListValue);
                    if (listValue != null)
                    {
                        var partValues = listValue.OfType<PartValue>();
                        parts.AddRange(partValues.Select(pv => pv.Part));
                        break;
                    }
                    var partListValue = obj as ListValue<PartValue>;
                    if (partListValue != null)
                    {
                        parts.AddRange(partListValue.Select(pv => pv.Part));
                        break;
                    }
                    var partList= obj as List<PartValue>;
                    if (partList!= null)
                    {
                        parts.AddRange(partList.Select(pv => pv.Part));
                        break;
                    }
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
            if (parts.Count == 0)
                return null;
            var vessel = parts[0].vessel;
            if (parts.All(p => p.vessel == vessel)) return vessel.id;

            // Some parts are from a different vessel? bail
            return null;
        }
    }
}