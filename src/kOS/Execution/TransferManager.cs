using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;

namespace kOS.Execution
{
    public class TransferManager : Structure, IUpdateObserver
    {
        public enum TransferStatus        
        {
            Failed,
            Finished,
            Paused,
            Transfering
        }

        private readonly SharedObjects shared;
        private readonly List<ResourceTransferValue> transfers;

        public TransferManager(SharedObjects shared)
        {
            this.shared = shared;
            transfers = new List<ResourceTransferValue>();
            shared.UpdateHandler.AddObserver(this);
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("CLEAR", new NoArgsSuffix(() => transfers.Clear()));
            AddSuffix("LIST", new Suffix<ListValue>(() => ListValue.CreateList(transfers)));
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

        public static PartResourceDefinition ParseResource(string resourceName)
        {
            var resourceDefs = PartResourceLibrary.Instance.resourceDefinitions;
            var resourceInfo = resourceDefs.FirstOrDefault(rd => string.Equals(rd.name, resourceName, StringComparison.CurrentCultureIgnoreCase));
            return resourceInfo;
        }

        public void Update(double deltaTime)
        {
            foreach (var transfer in Transfers)
            {
                transfer.Update(deltaTime);
            }
        }
    }
}