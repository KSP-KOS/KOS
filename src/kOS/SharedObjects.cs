using System;
using System.Collections.Generic;
using kOS.InterProcessor;
using kOS.Binding;
using kOS.Factories;
using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Screen;
using kOS.Suffixed;
using kOS.Suffixed.Part;

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

        private readonly double amount;
        private readonly PartResourceDefinition resourceInfo;
        private readonly object transferTo;
        private TransferPartType transferToType;
        private readonly object transferFrom;
        private TransferPartType transferFromType;

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

        private void InitializeSuffixes()
        {
            throw new NotImplementedException();
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
            if (toTest is ListValue)
            {
                return TransferPartType.Parts;
            }
            if (toTest is ElementValue)
            {
                return TransferPartType.Element;
            }
        }
    }
}