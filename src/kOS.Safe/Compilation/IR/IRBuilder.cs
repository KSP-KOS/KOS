using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class is used to convert the Opcode representation to a form
    /// that is more appropriate for optimization passes. A single
    /// instance must be used for a complete program element
    /// (i.e. a <see cref="CodePart"/>).
    /// </summary>
    public class IRBuilder
    {
        /// <summary>
        /// Lowers the specified code from a sequence of <see cref="Opcode"/>s
        /// to a three-address code interim representation.
        /// </summary>
        /// <param name="code">The code to lower.</param>
        /// <returns>A sequence of <see cref="BasicBlock"/> objects, representing the instructions.</returns>
        public List<BasicBlock> Lower(List<Opcode> code, IRCodePart codePart, IRScope parentScope = null)
        {
            List<BasicBlock> blocks = new List<BasicBlock>();
            if (code.Count == 0)
                return blocks;
            Dictionary<string, int> labels = ProgramBuilder.MapLabels(code);
            CreateBlocks(code, codePart, labels, blocks, parentScope);
            FillBlocks(code, labels, blocks, codePart);
            return blocks;
        }

        private void CreateBlocks(List<Opcode> code, IRCodePart codePart, Dictionary<string, int> labels, List<BasicBlock> blocks, IRScope parentScope)
        {
            IRScope globalScope = parentScope ?? new IRScope(parentScope, null);
            SortedSet<int> leaders = new SortedSet<int>() { 0 };
            HashSet<int> scopePushes = new HashSet<int>();
            HashSet<int> scopePops = new HashSet<int>();
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i] is BranchOpcode branch)
                {
                    leaders.Add(i + 1);
                    if (branch.DestinationLabel != string.Empty)
                        leaders.Add(labels[branch.DestinationLabel]);
                    else
                        leaders.Add(i + branch.Distance);
                }
                else if (code[i] is OpcodeJumpStack)
                {
                    throw new NotImplementedException("OpcodeJumpStack is not implemented for optimization because it is non-deterministic. Use OptimizationLevel.None.");
                }
                else if (code[i] is OpcodeReturn ret)
                {
                    leaders.Add(i + 1);
                    if (ret.Depth > 0)
                        scopePops.Add(i);
                }
                else if (code[i] is OpcodePushScope)
                {
                    leaders.Add(i);
                    scopePushes.Add(i);
                }
                else if (code[i] is OpcodePopScope)
                {
                    leaders.Add(i + 1);
                    scopePops.Add(i);
                }
            }
            leaders.Add(code.Count);
            foreach (int startIndex in leaders.Take(leaders.Count - 1))
            {
                int endIndex = leaders.First(i => i > startIndex) - 1;
                string label = code[startIndex].Label;
                if (label.StartsWith("@"))
                    label = null;
                BasicBlock block = new BasicBlock(codePart, startIndex, endIndex, label);

                blocks.Add(block);
            }
            foreach (BasicBlock block in blocks)
            {
                Opcode lastOpcode = code[block.EndIndex];
                if (lastOpcode is BranchOpcode branch)
                {
                    int destinationIndex = branch.DestinationLabel != string.Empty ? labels[branch.DestinationLabel] : block.EndIndex + branch.Distance;
                    block.AddSuccessor(GetBlockFromStartIndex(blocks, destinationIndex));
                    if (!(branch is OpcodeBranchJump))
                        block.AddSuccessor(GetBlockFromStartIndex(blocks, block.EndIndex + 1));
                }
                else if (blocks.Any(b => b.StartIndex == block.EndIndex + 1))
                {
                    BasicBlock successor = GetBlockFromStartIndex(blocks, block.EndIndex + 1);
                    block.FallthroughJump = new IRJump(block, successor, lastOpcode.SourceLine, lastOpcode.SourceColumn);
                    block.AddSuccessor(successor);
                }
#if DEBUG
                block.OriginalOpcodes = code.ToArray();
#endif
            }

            BasicBlock rootBlock = GetBlockFromStartIndex(blocks, 0);
            rootBlock.EstablishDominance();

            List<BasicBlock> exitBlocks = blocks.Where(b => !b.Successors.Any()).ToList();
            BasicBlock unifiedReturn = new SyntheticReturnBlock(codePart) { Scope = globalScope };
            foreach (BasicBlock exitBlock in exitBlocks)
                exitBlock.AddSuccessor(unifiedReturn);
            unifiedReturn.EstablishPostDominance();

            AssignScopes(rootBlock, globalScope, scopePushes, scopePops);
        }

        private BasicBlock GetBlockFromStartIndex(List<BasicBlock> blocks, int startIndex)
            => blocks.First(b => b.StartIndex == startIndex);

        private static void AssignScopes(BasicBlock root, IRScope globalScope, HashSet<int> scopePushIndices, HashSet<int> scopePopIndices)
        {
            Stack<IRScope> scopeStack = new Stack<IRScope>();
            scopeStack.Push(globalScope);

            void Visit(BasicBlock block)
            {
                if (scopePushIndices.Contains(block.StartIndex))
                    scopeStack.Push(new IRScope(scopeStack.Peek(), block));

                block.Scope = scopeStack.Peek();

                // Return statements can pop multiple scopes
                if (scopePopIndices.Contains(block.EndIndex))
                    scopeStack.Pop().FooterBlock = block;

                foreach (BasicBlock child in block.Dominates)
                    Visit(child);
            }

            Visit(root);
        }

        private void FillBlocks(List<Opcode> code, Dictionary<string, int> labels, List<BasicBlock> blocks, IRCodePart codePart)
        {
            Stack<IInterimOperand> stack = new Stack<IInterimOperand>();
            BasicBlock currentBlock = GetBlockFromStartIndex(blocks, 0);
            for (int i = 0; i < code.Count; i++)
            {
                if (i > currentBlock.EndIndex)
                {
                    SetStackState(stack, currentBlock);
                    currentBlock = GetBlockFromStartIndex(blocks, i);
                }
                ParseInstruction(code[i], currentBlock, stack, labels, i, blocks, codePart);
            }
        }

        private static void SetStackState(Stack<IInterimOperand> stack, BasicBlock block)
        {
            List<IRInstruction> instructions = block.Instructions;
            int insertIndex = instructions.Count > 0 ? instructions.Count - 1 : 0;
            while (insertIndex > 0 &&
                (instructions[insertIndex - 1] is IRBranch ||
                instructions[insertIndex - 1] is IRJump ||
                instructions[insertIndex - 1] is IRJumpStack))
                insertIndex--;

            while (stack.Count > 0)
            {
                IInterimOperand stackValue = stack.Pop();
                IRPushStack push;
                if (stackValue is InterimConstantValue argMarker &&
                    argMarker.Value is Execution.KOSArgMarkerType)
                    push = new IRPushStackArgMarker(block, stackValue);
                else
                    push = new IRPushStack(block, stackValue);
                instructions.Insert(insertIndex, push);
            }
        }

        private static IInterimOperand PopFromStack(Stack<IInterimOperand> stack, BasicBlock block)
        {
            if (stack.Count > 0)
                return stack.Pop();
            return block.AddParameter();
        }

        private void ParseInstruction(Opcode opcode, BasicBlock currentBlock, Stack<IInterimOperand> stack, Dictionary<string, int> labels, int index, List<BasicBlock> blocks, IRCodePart codePart)
        {
            IInterimOperand PopStack()
                =>PopFromStack(stack, currentBlock);

            switch (opcode)
            {
                case OpcodeStore store:
                    Store(PopStack(), currentBlock, store, codePart);
                    break;
                case OpcodeStoreExist storeExist:
                    Store(PopStack(), currentBlock, storeExist, codePart, assertExist: true);
                    break;
                case OpcodeStoreLocal storeLocal:
                    Store(PopStack(), currentBlock, storeLocal, codePart, IRAssign.StoreScope.Local);
                    break;
                case OpcodeStoreGlobal storeGlobal:
                    Store(PopStack(), currentBlock, storeGlobal, codePart, IRAssign.StoreScope.Global);
                    break;
                case OpcodeExists exists:
                    IResultingInstruction instruction = new IRUnaryOp(currentBlock, exists, PopStack());
                    stack.Push(instruction);
                    break;
                case OpcodeUnset unset:
                    IInterimOperand variableIdentifier = PopStack();
                    currentBlock.Add(new IRUnset(currentBlock, unset, variableIdentifier));
                    break;
                case OpcodeGetMethod getMethod:
                    instruction = new IRSuffixGetMethod(currentBlock, PopStack(), getMethod);
                    stack.Push(instruction);
                    break;
                case OpcodeGetMember getMember:
                    instruction = new IRSuffixGet(currentBlock, PopStack(), getMember);
                    stack.Push(instruction);
                    break;
                case OpcodeSetMember setMember:
                    IInterimOperand value = PopStack();
                    IInterimOperand memberObj = PopStack();
                    currentBlock.Add(new IRSuffixSet(currentBlock, memberObj, value, setMember));
                    break;
                case OpcodeGetIndex getIndex:
                    IInterimOperand targetIndex = PopStack();
                    IInterimOperand indexObj = PopStack();
                    instruction = new IRIndexGet(currentBlock, indexObj, targetIndex, getIndex);
                    stack.Push(instruction);
                    break;
                case OpcodeSetIndex setIndex:
                    value = PopStack();
                    targetIndex = PopStack();
                    indexObj = PopStack();
                    currentBlock.Add(new IRIndexSet(currentBlock, indexObj, targetIndex, value, setIndex));
                    break;
                case OpcodeEOF _:
                case OpcodeEOP _:
                case OpcodeNOP _:
                case OpcodeBogus _:
                case OpcodeArgBottom _:
                    currentBlock.Add(new IRNoStackInstruction(currentBlock, opcode));
                    break;
                case OpcodePushScope _:
                case OpcodePopScope _:
                    currentBlock.Add(new IRNoStackInstruction(currentBlock, opcode, true));
                    break;
                case OpcodeTestArgBottom _:
                    instruction = new IRNonVarPush(currentBlock, opcode);
                    stack.Push(instruction);
                    break;
                case OpcodeBranchIfTrue branchIfTrue:
                    int target;
                    if (string.IsNullOrEmpty(branchIfTrue.DestinationLabel))
                        target = index + branchIfTrue.Distance;
                    else
                        target = labels[branchIfTrue.DestinationLabel];
                    currentBlock.Add(new IRBranch(currentBlock, PopStack(),
                        GetBlockFromStartIndex(blocks, target),
                        GetBlockFromStartIndex(blocks, currentBlock.EndIndex + 1),
                        branchIfTrue));
                    break;
                case OpcodeBranchIfFalse branchIfFalse:
                    if (string.IsNullOrEmpty(branchIfFalse.DestinationLabel))
                        target = index + branchIfFalse.Distance;
                    else
                        target = labels[branchIfFalse.DestinationLabel];
                    currentBlock.Add(new IRBranch(currentBlock, PopStack(),
                        GetBlockFromStartIndex(blocks, currentBlock.EndIndex + 1),
                        GetBlockFromStartIndex(blocks, target),
                        branchIfFalse));
                    break;
                case OpcodeBranchJump branchJump:
                    int destinationIndex = branchJump.DestinationLabel != string.Empty ? labels[branchJump.DestinationLabel] : index + branchJump.Distance;
                    currentBlock.Add(new IRJump(currentBlock, GetBlockFromStartIndex(blocks, destinationIndex), branchJump));
                    break;
                case OpcodeJumpStack _:
                    throw new NotImplementedException("OpcodeJumpStack is not implemented for optimization because it is non-deterministic. Use OptimizationLevel.None.");
                case OpcodeCompareGT _:
                case OpcodeCompareLT _:
                case OpcodeCompareGTE _:
                case OpcodeCompareLTE _:
                case OpcodeCompareNE _:
                case OpcodeCompareEqual _:
                case OpcodeMathAdd _:
                case OpcodeMathSubtract _:
                case OpcodeMathMultiply _:
                case OpcodeMathDivide _:
                case OpcodeMathPower _:
                    IInterimOperand right = PopStack();
                    IInterimOperand left = PopStack();
                    instruction = new IRBinaryOp(currentBlock, (BinaryOpcode)opcode, left, right);
                    stack.Push(instruction);
                    break;
                case OpcodeMathNegate _:
                case OpcodeLogicToBool _:
                case OpcodeLogicNot _:
                    instruction = new IRUnaryOp(currentBlock, opcode, PopStack());
                    stack.Push(instruction);
                    break;
                case OpcodeCall call:
                    Stack<IInterimOperand> arguments = new Stack<IInterimOperand>();
                    while (stack.Count > 0)
                    {
                        IInterimOperand stackResult = PopStack();
                        if (stackResult is InterimConstantValue constant && constant.Value is Execution.KOSArgMarkerType)
                            break;
                        arguments.Push(stackResult);
                    }
                    instruction = new IRCall(currentBlock, call, arguments);
                    if (stack.Count > 0 && !((IRCall)instruction).Direct)
                    {
                        ((IRCall)instruction).IndirectMethod = PopStack();
                    }
                    stack.Push(instruction);
                    break;
                case OpcodeReturn opcodeReturn:
                    currentBlock.Add(new IRReturn(currentBlock, opcodeReturn.Depth, opcodeReturn) { Value = PopStack() });
                    break;
                case OpcodePush opcodePush:
                    object argument = opcodePush.Argument;
                    if (IsPushingVariable(opcodePush))
                        stack.Push(new InterimVariableReference((string)argument, opcodePush));
                    else
                        stack.Push(new InterimConstantValue(argument, opcodePush));
                    break;
                case OpcodePushDelegateRelocateLater delegateRelocateLater:
                    stack.Push(new IRDelegateRelocateLater(delegateRelocateLater.DestinationLabel, delegateRelocateLater.WithClosure, delegateRelocateLater));
                    break;
                case OpcodePushRelocateLater relocateLater:
                    stack.Push(new IRRelocateLater(relocateLater.DestinationLabel, relocateLater));
                    break;
                case OpcodeAddTrigger _:
                    IInterimOperand pointer = PopStack();
                    currentBlock.Add(new IRUnaryConsumer(currentBlock, opcode, pointer, false));
                    codePart.EnrollClosure((string)((InterimConstantValue)pointer).Value, currentBlock.Scope);
                    break;
                case OpcodeRemoveTrigger _:
                    currentBlock.Add(new IRUnaryConsumer(currentBlock, opcode, PopStack(), false));
                    break;
                case OpcodeWait _:
                    currentBlock.Add(new IRUnaryConsumer(currentBlock, opcode, PopStack(), true));
                    break;
                case OpcodePop pop:
                    currentBlock.Add(new IRPop(currentBlock, PopStack(), pop));
                    break;
                case OpcodeDup _:
                    stack.Push(stack.Peek());
                    break;
                case OpcodeSwap _:
                    IInterimOperand first = stack.Pop();
                    IInterimOperand second = stack.Pop();
                    stack.Push(first);
                    stack.Push(second);
                    break;
                default:
                    throw new NotImplementedException($"The Opcode of type {opcode.GetType()} is not implemented.");
            }
        }

        private static void Store(IInterimOperand value, BasicBlock block, OpcodeIdentifierBase opcode, IRCodePart codePart, IRAssign.StoreScope storeScope = IRAssign.StoreScope.Ambivalent, bool assertExist = false)
        {
            IInterimOperand stackValue = value;
            IRAssign assignment = new IRAssign(block, opcode, stackValue) { Scope = storeScope, AssertExists = assertExist };
            IRScope scope;
            switch (storeScope)
            {
                case IRAssign.StoreScope.Local:
                    scope = block.Scope;
                    break;
                case IRAssign.StoreScope.Global:
                    scope = block.Scope.GetGlobalScope();
                    break;
                default:
                    scope = block.Scope.GetScopeForVariableNamed(opcode.Identifier);
                    break;
            }
            block.Add(assignment);

            scope.StoreLocalVariable(opcode.Identifier);

            if (stackValue is IRRelocateLater lockOrFunctionPointer)
            {
                codePart.EnrollFunction(opcode.Identifier, (string)lockOrFunctionPointer.Value, block.Scope, storeScope == IRAssign.StoreScope.Global);
            }
        }

        private static bool IsPushingVariable(OpcodePush opcodePush)
            => opcodePush.Argument is string identifier && identifier.StartsWith("$");
    }
}