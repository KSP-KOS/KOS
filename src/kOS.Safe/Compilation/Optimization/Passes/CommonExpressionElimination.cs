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
            // Track expressions at the outermost context for
            // this code part unit.
            Dictionary<(IResultingInstruction Expression, IRScope Scope), ExpressionData> expressions =
                new Dictionary<(IResultingInstruction, IRScope), ExpressionData>(ExpressionComparer.Instance);

            // Populate the known expression in reverse post-order
            // to ensure that the definition sites are before their
            // first use.
            foreach (BasicBlock block in BasicBlock.GetReversePostOrder(rootBlock, BasicBlock.GetSuccessors))
            {
                IdentifyExpressions(block, expressions);
            }
            // Replace expressions when appropriate.
            foreach (KeyValuePair<(IResultingInstruction Expression, IRScope), ExpressionData> expressionData in expressions)
            {
                IResultingInstruction expression = expressionData.Key.Expression;
                ExpressionData data = expressionData.Value;
                // Skip expressions that are not reused.
                int useCount = data.uses.Count;
                if (useCount == 0)
                    continue;
                ushort opcodeCount = expression.OpcodeCount;
                // Skip expressions that as fast to recompute.
                if (opcodeCount < 2)
                    continue;
                // Skip expressions that are not reused enough to
                // be worth the cost of storing them.
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

                // Flag to only replace the first usage (in case an
                // instruction both defines and reuses an expression).
                bool first = true;
                CompilerTemporaryVariable storedExpression = new CompilerTemporaryVariable(expression, ((IRInstruction)data.origin).Block);
                // Replace the first use with the temporary variable definition.
                data.origin.MutateEachOperand(op =>
                {
                    if (first && op.Equals(expression))
                    {
                        first = false;
                        return storedExpression;
                    }
                    return op;
                });
                // Replace subsequent uses with the temporary variable.
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
                        // Break from the subexpression loop if a
                        // non-invariant call is encountered so as
                        // to not 'optimize' away a call that does something.
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
        // Custom equality comparer to ensure the scoping of the
        // temporary variable is appropriate.
        private class ExpressionComparer : IEqualityComparer<(IResultingInstruction Expression, IRScope Scope)>
        {
            public static readonly ExpressionComparer Instance = new ExpressionComparer();
            // IRScope.IsEqualOrEncompassedBy(IRScope) is directional
            // so the y.Scope.IsEqualOrEncompassedBy(x.Scope) is a specific
            // choice to work with how the dictionary keys are checked.
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
            // If this were invariant, it would already be replaced in
            // constant folding. Always return false to protect it from
            // being folded later.
            public bool IsInvariant => false;
            public Type Type => Value.Type;

            public CompilerTemporaryVariable(IResultingInstruction value, BasicBlock block)
            {
                Value = value;
                ushort id;
                // Wrapping is fine - no one should have more than
                // 65,000 re-used expressions in a single scope.
                unchecked
                {
                    id = nextID++;
                }
                // This identified is impossible for a user to
                // create in a .ks file.
                Identifier = $"$compiler.Temp.{id}";
                Definition = new SSASetDefinition(Identifier, new IRAssign(block, new OpcodeStoreLocal(Identifier), value));
            }

            // This implementation of Clone gives the original
            // expression back because the scope can't be assured.
            public IInterimOperand Clone(BasicBlock block)
                => Value.Clone(block);

            public IEnumerable<Opcode> EmitOpcodes()
            {
                // Emit the original expression.
                foreach (Opcode opcode in Value.EmitOpcodes())
                    yield return opcode;
                // Duplicate it's result on the stack.
                yield return new OpcodeDup();
                // Store one of those copies.
                yield return new OpcodeStoreLocal(Identifier);
                // The remaining copy is consumed by the user of the
                // original point of this expression.
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
