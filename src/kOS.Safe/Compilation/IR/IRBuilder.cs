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
    public static class IRBuilder
    {
        /// <summary>
        /// Lowers the specified code from a sequence of <see cref="Opcode"/>s
        /// to a three-address code interim representation.
        /// </summary>
        /// <param name="code">The code to lower.</param>
        /// <returns>A sequence of <see cref="BasicBlock"/> objects, representing the instructions.</returns>
        public static List<BasicBlock> Lower(List<Opcode> code, ICodeComponent codeComponent, IRScope parentScope = null)
        {
            if (code.Count == 0)
                return new List<BasicBlock>();
            Dictionary<string, int> labels = ProgramBuilder.MapLabels(code);
            List<BasicBlock> blocks = CreateBlocks(code, codeComponent, labels, parentScope, out HashSet<int> scopePushes, out Dictionary<int, int> scopePops);
            FillBlocks(code, labels, blocks, out List<(string, string, BasicBlock, bool)> functionsToEnroll, out List<(string, BasicBlock)> closuresToEnroll);

            IRScope globalScope = parentScope ?? new IRScope(parentScope, null);
            AssignScopes(GetBlockFromStartIndex(blocks, 0), globalScope, scopePushes, scopePops);

            foreach ((string identifier, string pointer, BasicBlock block, bool global) in functionsToEnroll)
                codeComponent.CodePart.EnrollFunction(identifier, pointer, block.Scope, global);
            foreach ((string identifier, BasicBlock block) in closuresToEnroll)
                codeComponent.CodePart.EnrollClosure(identifier, block.Scope);

            return blocks;
        }

        private static List<BasicBlock> CreateBlocks(List<Opcode> code, ICodeComponent codeComponent, Dictionary<string, int> labels, IRScope parentScope,
            out HashSet<int> scopePushes, out Dictionary<int, int> scopePops)
        {
            List<BasicBlock> blocks = new List<BasicBlock>();
            IRScope globalScope = parentScope ?? new IRScope(parentScope, null);
            SortedSet<int> leaders = new SortedSet<int>() { 0 };
            scopePushes = new HashSet<int>();
            scopePops = new Dictionary<int, int>();
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
                        scopePops.Add(i, ret.Depth);
                }
                else if (code[i] is OpcodePushScope)
                {
                    leaders.Add(i);
                    scopePushes.Add(i);
                }
                else if (code[i] is OpcodePopScope popScope)
                {
                    leaders.Add(i + 1);
                    scopePops.Add(i, popScope.NumLevels);
                }
            }
            leaders.Add(code.Count);

            foreach (int startIndex in leaders.Take(leaders.Count - 1))
            {
                int endIndex = leaders.First(i => i > startIndex) - 1;
                string label = code[startIndex].Label;
                if (label.StartsWith("@"))
                    label = null;
                BasicBlock block = new BasicBlock(codeComponent, startIndex, endIndex, label);

                blocks.Add(block);
            }

            BasicBlock rootBlock = GetBlockFromStartIndex(blocks, 0);
            BasicBlock unifiedReturn = new SyntheticReturnBlock(codeComponent.CodePart) { Scope = globalScope };
            codeComponent.RootBlock = rootBlock;
            if (codeComponent.TerminalBlock == null)
                codeComponent.TerminalBlock = unifiedReturn;
            else
                unifiedReturn = codeComponent.TerminalBlock;

            foreach (BasicBlock block in blocks)
            {
                Opcode lastOpcode = code[block.EndIndex];
                if (blocks.Any(b => b.StartIndex == block.EndIndex + 1))
                {
                    BasicBlock successor = GetBlockFromStartIndex(blocks, block.EndIndex + 1);
                    block.Continuation = new JumpContinuation(successor, lastOpcode.SourceLine, lastOpcode.SourceColumn);
                }
#if DEBUG
                block.OriginalOpcodes = code.ToArray();
#endif
            }

            List<BasicBlock> exitBlocks = blocks.Where(b => !b.Successors.Any()).ToList();
            foreach (BasicBlock exitBlock in exitBlocks)
                exitBlock.Continuation = new JumpContinuation(unifiedReturn, code[exitBlock.EndIndex].SourceLine, code[exitBlock.EndIndex].SourceColumn);
            
            rootBlock.EstablishDominance();
            unifiedReturn.EstablishPostDominance();

            return blocks;
        }

        private static BasicBlock GetBlockFromStartIndex(List<BasicBlock> blocks, int startIndex)
            => blocks.First(b => b.StartIndex == startIndex);

        private static void AssignScopes(BasicBlock root, IRScope globalScope, HashSet<int> scopePushIndices, Dictionary<int, int> scopePopIndices)
        {
            void Visit(BasicBlock block)
            {
                IRScope incomingScope;
                int numLevels;
                if (block.Dominator == null)
                    incomingScope = globalScope;
                else
                {
                    incomingScope = block.Dominator.Scope;
                    if (scopePopIndices.TryGetValue(block.Dominator.EndIndex, out numLevels))
                    {
                        for (int i = 0; i < numLevels; i++)
                            incomingScope = incomingScope.ParentScope;
                    }
                }

                if (scopePushIndices.Contains(block.StartIndex))
                    block.Scope = new IRScope(incomingScope, block);
                else
                    block.Scope = incomingScope;

                if (scopePopIndices.TryGetValue(block.EndIndex, out numLevels))
                {
                    IRScope scope = block.Scope;
                    for (int i = 0; i < numLevels; i++)
                    {
                        scope.FooterBlocks.Add(block);
                        scope = scope.ParentScope;
                    }
                }

                foreach (BasicBlock child in block.Dominates)
                    Visit(child);
            }

            Visit(root);
        }

        private static void FillBlocks(List<Opcode> code, Dictionary<string, int> labels, List<BasicBlock> blocks,
            out List<(string, string, BasicBlock, bool)> functionsToEnroll, out List<(string, BasicBlock)> closuresToEnroll)
        {
            Stack<IInterimOperand> stack = new Stack<IInterimOperand>();
            BasicBlock currentBlock = GetBlockFromStartIndex(blocks, 0);
            closuresToEnroll = new List<(string, BasicBlock)>();
            functionsToEnroll = new List<(string, string, BasicBlock, bool)>();
            for (int i = 0; i < code.Count; i++)
            {
                if (i > currentBlock.EndIndex)
                {
                    SetStackState(stack, currentBlock);
                    currentBlock = GetBlockFromStartIndex(blocks, i);
                }
                ParseInstruction(code[i], currentBlock, stack, labels, i, blocks, ref functionsToEnroll, ref closuresToEnroll);
            }
        }

        private static void SetStackState(Stack<IInterimOperand> stack, BasicBlock block)
        {
            List<IRInstruction> instructions = block.Instructions;

            while (stack.Count > 0)
            {
                IInterimOperand stackValue = stack.Pop();
                IRPushStack push;
                if (stackValue is InterimConstantValue argMarker &&
                    argMarker.Value is Execution.KOSArgMarkerType)
                    push = new IRPushStackArgMarker(block, stackValue);
                else
                    push = new IRPushStack(block, stackValue);
                instructions.Add(push);
            }
        }

        private static IInterimOperand PopFromStack(Stack<IInterimOperand> stack, BasicBlock block)
        {
            if (stack.Count > 0)
                return stack.Pop();
            return block.AddParameter();
        }

        private static void ParseInstruction(Opcode opcode, BasicBlock currentBlock, Stack<IInterimOperand> stack, Dictionary<string, int> labels, int index, List<BasicBlock> blocks,
            ref List<(string, string, BasicBlock, bool)> functionsToEnroll, ref List<(string, BasicBlock)> closuresToEnroll)
        {
            IInterimOperand PopStack()
                =>PopFromStack(stack, currentBlock);

            switch (opcode)
            {
                case OpcodeStore store:
                    Store(PopStack(), currentBlock, store, functionsToEnroll);
                    break;
                case OpcodeStoreExist storeExist:
                    Store(PopStack(), currentBlock, storeExist, functionsToEnroll, assertExist: true);
                    break;
                case OpcodeStoreLocal storeLocal:
                    Store(PopStack(), currentBlock, storeLocal, functionsToEnroll, IRAssign.StoreScope.Local);
                    break;
                case OpcodeStoreGlobal storeGlobal:
                    Store(PopStack(), currentBlock, storeGlobal, functionsToEnroll, IRAssign.StoreScope.Global);
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
                    currentBlock.Continuation = new BranchContinuation(PopStack(),
                        GetBlockFromStartIndex(blocks, target),
                        GetBlockFromStartIndex(blocks, currentBlock.EndIndex + 1),
                        branchIfTrue);
                    break;
                case OpcodeBranchIfFalse branchIfFalse:
                    if (string.IsNullOrEmpty(branchIfFalse.DestinationLabel))
                        target = index + branchIfFalse.Distance;
                    else
                        target = labels[branchIfFalse.DestinationLabel];
                    currentBlock.Continuation = new BranchContinuation(PopStack(),
                        GetBlockFromStartIndex(blocks, currentBlock.EndIndex + 1),
                        GetBlockFromStartIndex(blocks, target),
                        branchIfFalse);
                    break;
                case OpcodeBranchJump branchJump:
                    int destinationIndex = branchJump.DestinationLabel != string.Empty ? labels[branchJump.DestinationLabel] : index + branchJump.Distance;
                    currentBlock.Continuation = new JumpContinuation(GetBlockFromStartIndex(blocks, destinationIndex), branchJump.SourceLine, branchJump.SourceColumn);
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
                    closuresToEnroll.Add(((string)((InterimConstantValue)pointer).Value, currentBlock));
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

        private static void Store(IInterimOperand value, BasicBlock block, OpcodeIdentifierBase opcode, List<(string, string, BasicBlock, bool)> functionsToEnroll, IRAssign.StoreScope storeScope = IRAssign.StoreScope.Ambivalent, bool assertExist = false)
        {
            IRAssign assignment = new IRAssign(block, opcode, value) { Scope = storeScope, AssertExists = assertExist };
            block.Add(assignment);


            if (value is IRRelocateLater lockOrFunctionPointer)
                functionsToEnroll.Add((opcode.Identifier, (string)lockOrFunctionPointer.Value, block, storeScope == IRAssign.StoreScope.Global));
        }

        private static bool IsPushingVariable(OpcodePush opcodePush)
            => opcodePush.Argument is string identifier && identifier.StartsWith("$");
    }
}