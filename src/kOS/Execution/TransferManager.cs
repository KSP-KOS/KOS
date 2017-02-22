using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe;
using kOS.Suffixed;

namespace kOS.Execution
{
    public class TransferManager : IFixedUpdateObserver
    {
        public enum TransferStatus        
        {
            Failed,
            Finished,
            Inactive,
            Transferring
        }

        private readonly SharedObjects shared;
        private readonly HashSet<ResourceTransferValue> transfers;

        public TransferManager(SharedObjects shared)
        {
            this.shared = shared;
            transfers = new HashSet<ResourceTransferValue>();
            shared.UpdateHandler.AddFixedObserver(this);
        }

        public ResourceTransferValue CreateTransfer(PartResourceDefinition resourceInfo, object transferTo, object transferFrom, double amount)
        {
            var toReturn = new ResourceTransferValue(this, resourceInfo, transferTo, transferFrom, amount);
            transfers.Add(toReturn);
            return toReturn;
        }

        public ResourceTransferValue CreateTransfer(PartResourceDefinition resourceInfo, object transferTo, object transferFrom)
        {
            var toReturn = new ResourceTransferValue(this, resourceInfo, transferTo, transferFrom);
            transfers.Add(toReturn);
            return toReturn;
        }

        public void Dispose()
        {
            shared.UpdateHandler.RemoveFixedObserver(this);
        }

        public static PartResourceDefinition ParseResource(string resourceName)
        {
            var resourceDefs = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceInfo = null;
            // PartResourceDefinitionList's array index accessor uses the resource id
            // instead of as a list index, so we need to use an enumerator.
            foreach (var def in resourceDefs)
            {
                // loop through definitions looking for a case insensitive name match,
                // return true if a match is found
                if (def.name.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    resourceInfo = def;
                }
            }
            return resourceInfo;
        }

        public void KOSFixedUpdate(double deltaTime)
        {
            if (!transfers.Any()) return;

            foreach (var transfer in transfers)
            {
                transfer.Update(deltaTime);

            }
            transfers.RemoveWhere(t => t.Status == TransferStatus.Finished || t.Status == TransferStatus.Failed);
        }

        public void ReregisterTransfer(ResourceTransferValue resourceTransferValue)
        {
            if (resourceTransferValue.Status == TransferStatus.Transferring)
            {
                if (!transfers.Contains(resourceTransferValue))
                {
                    transfers.Add(resourceTransferValue);
                }
            }
        }
    }
}