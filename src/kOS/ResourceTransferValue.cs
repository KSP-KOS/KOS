using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Suffixed.Part;

namespace kOS
{
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
        private TransferPartType toType;
        private readonly object transferFrom;
        private TransferPartType fromType;
        private bool active;

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

        public bool Update(double deltaTime)
        {

        }

        private void InitializeSuffixes()
        {
            AddSuffix("RESOURCE", new Suffix<string>(() => resourceInfo.name));
            AddSuffix("FROM", new Suffix<object>(() => transferFrom));
            AddSuffix("TO", new Suffix<object>(() => transferTo));
            AddSuffix("ACTIVE", new SetSuffix<bool>(() => active, value => active = value));
            AddSuffix("TOTALAMOUNT", new Suffix<bool>(() => active));
        }

        private void DetermineTypes()
        {
            toType = DetermineType(transferTo);
            fromType = DetermineType(transferFrom);
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
            throw new ArgumentException();
        }
    }
}