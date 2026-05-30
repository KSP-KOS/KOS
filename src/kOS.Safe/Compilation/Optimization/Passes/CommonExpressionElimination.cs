using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class CommonExpressionElimination : IHolisticOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Balanced;
        public short SortIndex => 32100;

        public void ApplyPass(IRCodePart codePart)
        {
            foreach (BasicBlock rootBlock in codePart.RootBlocks)
                EliminateCommonExpressions(rootBlock);
        }
        private void EliminateCommonExpressions(BasicBlock rootBlock)
        {
            Dictionary<(IResultingInstruction Expression, IRScope Scope), ExpressionData> expressions =
                new Dictionary<(IResultingInstruction, IRScope), ExpressionData>(ExpressionComparer.Instance);
            foreach (BasicBlock block in BasicBlock.GetReversePostOrder(rootBlock, BasicBlock.GetSuccessors))
            {
                IdentifyExpressions(block, expressions);
            }
            foreach (KeyValuePair<(IResultingInstruction Expression, IRScope), ExpressionData> expressionData in expressions)
            {
                IResultingInstruction expression = expressionData.Key.Expression;
                ExpressionData data = expressionData.Value;
                int useCount = data.uses.Count;
                if (useCount == 0)
                    continue;
                ushort opcodeCount = expression.OpcodeCount;
                if (opcodeCount < 2)
                    continue;
                switch (opcodeCount)
                {
                    case 2:
                        if (useCount < 3)
                            continue;
                        break;
                    case 3:
                        if (useCount < 2)
                            continue;
                        break;
                }

                bool first = true;
                CompilerTemporaryVariable storedExpression = new CompilerTemporaryVariable(expression, ((IRInstruction)data.origin).Block);
                data.origin.MutateEachOperand(op =>
                {
                    if (first && op.Equals(expression))
                    {
                        first = false;
                        return storedExpression;
                    }
                    return op;
                });
                foreach (IOperandInstructionBase use in data.uses)
                {
                    use.MutateEachOperand(op =>
                        op.Equals(expression) ?
                            new InterimResolvedReference(storedExpression.Definition, (IRInstruction)use) : op);
                }
            }
        }
        private void IdentifyExpressions(BasicBlock block, Dictionary<(IResultingInstruction Expression, IRScope Scope), ExpressionData> expressions)
        {
            foreach (IRInstruction instruction in block.Instructions)
            {
                foreach (IRInstruction subexpression in instruction.DepthFirst())
                {
                    bool breaking = false;
                    void AddToExpressions(IInterimOperand operand)
                    {
                        if (operand is IRCall call && !call.IsCallInvariant())
                            breaking = true;
                        else if (operand is IResultingInstruction resultingInstruction)
                        {
                            if (expressions.TryGetValue((resultingInstruction, block.Scope), out ExpressionData data))
                                data.uses.Add((IOperandInstructionBase)subexpression);
                            else
                                expressions[(resultingInstruction, block.Scope)] = new ExpressionData((IOperandInstructionBase)subexpression);
                        }
                    }

                    if (breaking)
                        break;
                    if (subexpression is IOperandInstructionBase operandInstruction)
                        operandInstruction.ForEachOperand(AddToExpressions);
                }
            }
        }
        private class ExpressionComparer : IEqualityComparer<(IResultingInstruction Expression, IRScope Scope)>
        {
            public static readonly ExpressionComparer Instance = new ExpressionComparer();
            public bool Equals((IResultingInstruction Expression, IRScope Scope) x, (IResultingInstruction Expression, IRScope Scope) y)
                => x.Expression.Equals(y.Expression) && y.Scope.IsEqualOrEncompassedBy(x.Scope);
            public int GetHashCode((IResultingInstruction Expression, IRScope Scope) obj)
                => obj.Expression.GetHashCode();
        }
        private readonly struct ExpressionData
        {
            public readonly IOperandInstructionBase origin;
            public readonly List<IOperandInstructionBase> uses;
            public ExpressionData(IOperandInstructionBase origin)
            {
                this.origin = origin;
                uses = new List<IOperandInstructionBase>();
            }
        }

        public class CompilerTemporaryVariable : IResultingInstruction, IOperandInstructionBase
        {
            private static ushort nextID;

            public IResultingInstruction Value { get; }
            public SSASetDefinition Definition { get; }
            public string Identifier { get; }
            public ushort OpcodeCount => (ushort)(Value.OpcodeCount + 2);
            public bool IsInvariant => false;
            public Type Type => Value.Type;

            public CompilerTemporaryVariable(IResultingInstruction value, BasicBlock block)
            {
                Value = value;
                ushort id;
                unchecked
                {
                    id = nextID++;
                }
                Identifier = $"$compiler.Temp.{id}";
                Definition = new SSASetDefinition(Identifier, new IRAssign(block, new OpcodeStoreLocal(Identifier), value));
            }

            public IInterimOperand Clone(BasicBlock block)
                => Value.Clone(block);

            public IEnumerable<Opcode> EmitOpcodes()
            {
                foreach (Opcode opcode in Value.EmitOpcodes())
                    yield return opcode;
                yield return new OpcodeDup();
                yield return new OpcodeStoreLocal(Identifier);
            }

            public bool Equals(IInterimOperand other)
                => other == this;

            public InterimConstantValue Evaluate()
                => throw new InvalidOperationException();

            public void ForEachOperand(Action<IInterimOperand> action)
            {
                if (Value is IOperandInstructionBase operandInstruction)
                    operandInstruction.ForEachOperand(action);
            }
            public void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
            {
                if (Value is IOperandInstructionBase operandInstruction)
                    operandInstruction.MutateEachOperand(mutateFunc);
            }

            public override string ToString()
                => Definition.ToString();
        }
    }
}
