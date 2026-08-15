using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public class StackTransferPhi : PhiNode<IStackTransferObject>, IStackTransferObject, IOperandInstructionBase
    {
        private readonly HashSet<StackTransferPhi> controllers = new HashSet<StackTransferPhi>();
        private readonly HashSet<IRParameter> references = new HashSet<IRParameter>();
        public bool AdoptTypeHints { get; set; } = false;
        public IReadOnlyCollection<StackTransferPhi> Controllers => controllers;
        public IReadOnlyCollection<IRParameter> References => references;
        public IEnumerable<IStackTransferObject> StackTransferObjects
            => new StackTransferPhi[] { this }.Concat(PossibleValues.Values);
        protected override IEnumerable<IInterimOperand> Operands => GetAllInvolvedPushes().Select(p => p.Value);

        public override bool IsInvariant => IsResolvable && SingleValueIs(ObjIsInvariant, this, null);
        public override Type Type
        {
            get
            {
                IEnumerable<IStackTransferObject> reachableValues =
                    PossibleValues.Where(kvp => kvp.Key.IsExecutable && kvp.Value != this).Select(kvp => kvp.Value);
                if (!reachableValues.Any())
                    return null;
                if (AdoptTypeHints)
                    reachableValues = reachableValues.Where(obj => !(obj is IRPushStack pushStack) || pushStack.Block != null);
                if (!reachableValues.Any())
                    return typeof(Encapsulation.Structure);

                Type proposedType = ObjType(reachableValues.First());
                foreach (IStackTransferObject variable in reachableValues.Skip(1))
                    proposedType = GetFirstCommonBaseType(proposedType, ObjType(variable));

                return proposedType;
            }
        }
        protected override bool ObjIsInvariant(IStackTransferObject obj)
            => obj?.IsInvariant ?? false;
        protected override Type ObjType(IStackTransferObject obj)
            => obj?.Type ?? typeof(Encapsulation.Structure);
        protected bool SingleValueIs(Func<IStackTransferObject, bool> predicate, StackTransferPhi caller, HashSet<IStackTransferObject> visited)
        {
            if (visited == null)
                visited = new HashSet<IStackTransferObject>();
            IEnumerable<KeyValuePair<BasicBlock, IStackTransferObject>> reachableValues =
                PossibleValues.Where(kvp => kvp.Key.IsExecutable);
            bool singleValue = false;
            foreach (IStackTransferObject value in reachableValues.Select(kvp => kvp.Value))
            {
                if (visited.Add(value))
                {
                    if (value == caller)
                        return false;
                    if (singleValue)
                        return false;
                    bool result;
                    switch (value)
                    {
                        case StackTransferPhi phi:
                            result = phi.SingleValueIs(predicate, caller, visited);
                            break;
                        default:
                            result = predicate(value);
                            break;
                    }
                    if (!result)
                        return false;
                    singleValue = true;
                }
            }
            return singleValue;
        }
        public override InterimConstantValue Evaluate()
        {
            if (!PossibleValues.Any())
                return null;
            return base.Evaluate();
        }

        protected override InterimConstantValue EvaluateObj(IStackTransferObject obj)
            => (obj.Value as IEvaluatableToConstant)?.Evaluate();


        protected override void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
        {
            foreach (IRPushStack pushStack in GetAllInvolvedPushes())
                pushStack.Value = mutateFunc(pushStack.Value);
        }
        protected IEnumerable<IRPushStack> GetAllInvolvedPushes()
        {
            List<IRPushStack> pushes = new List<IRPushStack>();
            HashSet<StackTransferPhi> visited = new HashSet<StackTransferPhi>() { this };
            BuildLists(pushes, visited);
            return pushes;
        }
        protected void BuildLists(List<IRPushStack> list, HashSet<StackTransferPhi> visited)
        {
            foreach (BasicBlock block in PossibleValues.Keys)
            {
                switch (PossibleValues[block])
                {
                    case IRPushStack pushStack:
                        if (!list.Contains(pushStack))
                            list.Add(pushStack);
                        break;
                    case StackTransferPhi phi:
                        if (visited.Add(phi))
                            phi.BuildLists(list, visited);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        protected override IInterimOperand ValueAsOperand(IStackTransferObject item)
            => item is IRPushStack pushStack ? pushStack.Value : throw new NotImplementedException();

        public bool IsResolvable => IRParameter.IsSetResolvable(this);
        public bool IsSelfResolvable => PossibleValues.Where(kvp => kvp.Key.IsExecutable).Distinct().Count() == 1;
        protected virtual bool ObjIsResolvable(IStackTransferObject obj)
            => obj?.IsResolvable ?? false;

        public IInterimOperand Value
        {
            get
            {
                if (!IsResolvable)
                    throw new InvalidOperationException();
                return PossibleValues.First(kvp => kvp.Key.IsExecutable).Value.Value;
            }
            set
            {
                foreach (BasicBlock block in PossibleValues.Keys)
                    PossibleValues[block].Value = value;
            }
        }

        public void AddController(StackTransferPhi phi)
            => controllers.Add(phi);
        public void AddReference(IRParameter reference)
            => references.Add(reference);
        public void RemoveReference(IRParameter reference)
            => references.Remove(reference);

        bool IOperandInstructionBase.AllOperands(Func<IInterimOperand, bool> predicate)
            => GetAllInvolvedPushes().Select(ValueAsOperand).All(predicate);
        bool IOperandInstructionBase.AnyOperand(Func<IInterimOperand, bool> predicate)
            => GetAllInvolvedPushes().Select(ValueAsOperand).Any(predicate);
        void IOperandInstructionBase.ForEachOperand(Action<IInterimOperand> action)
        {
            foreach (IInterimOperand operand in GetAllInvolvedPushes().Select(ValueAsOperand))
                action(operand);
        }
    }
}
