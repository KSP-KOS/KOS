using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public class IRBuilder
    {
        private int nextTempId = 0;
        private int blockID = 0;

        public List<BasicBlock> Lower(List<Opcode> code)
        {
            List<BasicBlock> blocks = new List<BasicBlock>();
            if (code.Count == 0)
                return blocks;
            Dictionary<string, int> labels = ProgramBuilder.MapLabels(code);
            CreateBlocks(code, labels, blocks);
            FillBlocks(code, labels, blocks);
            return blocks;
        }

        private void CreateBlocks(List<Opcode> code, Dictionary<string, int> labels, List<BasicBlock> blocks)
        {
            SortedSet<int> leaders = new SortedSet<int>() { 0 };
            for (int i = 1; i < code.Count; i++)    // The first instruction is always a leader so we can skip 0.
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
                else if (code[i] is OpcodeReturn)
                    leaders.Add(i + 1);
                else if (code[i] is OpcodePushScope)
                    leaders.Add(i);
                else if (code[i] is OpcodePopScope)
                    leaders.Add(i + 1);
            }
            leaders.Add(code.Count);
            foreach (int startIndex in leaders.Take(leaders.Count - 1))
            {
                int endIndex = leaders.First(i => i > startIndex) - 1;
                string label = code[startIndex].Label;
                if (label.StartsWith("@"))
                    label = null;
                BasicBlock block = new BasicBlock(startIndex, endIndex, blockID++, label);
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
                    block.FallthroughJump = new IRJump(successor, lastOpcode.SourceLine, lastOpcode.SourceColumn);
                    block.AddSuccessor(successor);
                }
#if DEBUG
                block.OriginalOpcodes = code.ToArray();
#endif
            }
        }

        private BasicBlock GetBlockFromStartIndex(List<BasicBlock> blocks, int startIndex)
            => blocks.First(b => b.StartIndex == startIndex);

        private void FillBlocks(List<Opcode> code, Dictionary<string, int> labels, List<BasicBlock> blocks)
        {
            Stack<IRValue> stack = new Stack<IRValue>();
            BasicBlock currentBlock = GetBlockFromStartIndex(blocks, 0);
            for (int i = 0; i < code.Count; i++)
            {
                if (i > currentBlock.EndIndex)
                {
                    currentBlock.SetStackState(stack);
                    currentBlock = GetBlockFromStartIndex(blocks, i);
                }
                ParseInstruction(code[i], currentBlock, stack, labels, i, blocks);
            }
        }

        private IRTemp CreateTemp()
        {
            IRTemp result = new IRTemp(nextTempId);
            nextTempId++;
            return result;
        }
        private static IRValue PopFromStack(Stack<IRValue> stack, BasicBlock block)
        {
            if (stack.Count > 0)
                return stack.Pop();
            IRParameter parameter = new IRParameter();
            block.AddParameter(parameter);
            return parameter;
        }

        private void ParseInstruction(Opcode opcode, BasicBlock currentBlock, Stack<IRValue> stack, Dictionary<string, int> labels, int index, List<BasicBlock> blocks)
        {
            IRValue PopStack() => PopFromStack(stack, currentBlock);
            switch (opcode)
            {
                case OpcodeStore store:
                    IRAssign assignment = new IRAssign(store, PopStack()) { Scope = IRAssign.StoreScope.Ambivalent };
                    currentBlock.StoreVariable(new IRVariable(assignment.Target, assignment));
                    currentBlock.Add(assignment);
                    break;
                case OpcodeStoreExist storeExist:
                    assignment = new IRAssign(storeExist, PopStack()) { AssertExists = true };
                    if (!currentBlock.TryStoreVariable(new IRVariable(assignment.Target, assignment)))
                        throw new Exceptions.KOSCompileException(new KS.LineCol(assignment.SourceLine, assignment.SourceColumn), "Assert that variable exists failed.");
                    currentBlock.Add(assignment);
                    break;
                case OpcodeStoreLocal storeLocal:
                    assignment = new IRAssign(storeLocal, PopStack()) { Scope = IRAssign.StoreScope.Local };
                    currentBlock.StoreLocalVariable(new IRVariable(assignment.Target, assignment));
                    currentBlock.Add(assignment);
                    break;
                case OpcodeStoreGlobal storeGlobal:
                    assignment = new IRAssign(storeGlobal, PopStack()) { Scope = IRAssign.StoreScope.Global };
                    currentBlock.StoreGlobalVariable(new IRVariable(assignment.Target, assignment));
                    currentBlock.Add(assignment);
                    break;
                case OpcodeExists exists:
                    IRTemp temp = CreateTemp();
                    IRInstruction instruction = new IRUnaryOp(temp, exists, PopStack());
                    temp.Parent = instruction;
                    //currentBlock.Add(instruction);
                    stack.Push(temp);
                    break;
                case OpcodeUnset unset:
                    currentBlock.Add(new IRUnaryConsumer(unset, PopStack(), true));
                    break;
                case OpcodeGetMethod getMethod:
                    temp = CreateTemp();
                    instruction = new IRSuffixGetMethod(temp, PopStack(), getMethod);
                    //currentBlock.Add(instruction);
                    temp.Parent = instruction;
                    stack.Push(temp);
                    break;
                case OpcodeGetMember getMember:
                    temp = CreateTemp();
                    instruction = new IRSuffixGet(temp, PopStack(), getMember);
                    temp.Parent = instruction;
                    //currentBlock.Add(instruction);
                    stack.Push(temp);
                    break;
                case OpcodeSetMember setMember:
                    IRValue value = PopStack();
                    IRValue memberObj = PopStack();
                    currentBlock.Add(new IRSuffixSet(memberObj, value, setMember));
                    break;
                case OpcodeGetIndex getIndex:
                    IRValue targetIndex = PopStack();
                    IRValue indexObj = PopStack();
                    temp = CreateTemp();
                    instruction = new IRIndexGet(temp, indexObj, targetIndex, getIndex);
                    //currentBlock.Add(instruction);
                    temp.Parent = instruction;
                    stack.Push(temp);
                    break;
                case OpcodeSetIndex setIndex:
                    value = PopStack();
                    targetIndex = PopStack();
                    indexObj = PopStack();
                    currentBlock.Add(new IRIndexSet(indexObj, targetIndex, value, setIndex));
                    break;
                case OpcodeEOF _:
                case OpcodeEOP _:
                case OpcodeNOP _:
                case OpcodeBogus _:
                case OpcodePushScope _:
                case OpcodePopScope _:
                case OpcodeArgBottom _:
                    currentBlock.Add(new IRNoStackInstruction(opcode));
                    break;
                case OpcodeTestArgBottom _:
                    temp = CreateTemp();
                    instruction = new IRNonVarPush(temp, opcode);
                    temp.Parent = instruction;
                    //currentBlock.Add(instruction);
                    stack.Push(temp);
                    break;
                case OpcodeBranchIfTrue branchIfTrue:
                    currentBlock.Add(new IRBranch(PopStack(),
                        GetBlockFromStartIndex(blocks, labels[branchIfTrue.DestinationLabel]),
                        GetBlockFromStartIndex(blocks, currentBlock.EndIndex + 1),
                        branchIfTrue));
                    break;
                case OpcodeBranchIfFalse branchIfFalse:
                    currentBlock.Add(new IRBranch(PopStack(),
                        GetBlockFromStartIndex(blocks, currentBlock.EndIndex + 1),
                        GetBlockFromStartIndex(blocks, labels[branchIfFalse.DestinationLabel]),
                        branchIfFalse));
                    break;
                case OpcodeBranchJump branchJump:
                    int destinationIndex = branchJump.DestinationLabel != string.Empty ? labels[branchJump.DestinationLabel] : index + branchJump.Distance;
                    if (branchJump.DestinationLabel == string.Empty)
                    {
                        // TODO
                        bool test = index == currentBlock.EndIndex;
                    }
                    currentBlock.Add(new IRJump(GetBlockFromStartIndex(blocks, destinationIndex), branchJump));
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
                    temp = CreateTemp();
                    IRValue right = PopStack();
                    IRValue left = PopStack();
                    instruction = new IRBinaryOp(temp, (BinaryOpcode)opcode, left, right);
                    //currentBlock.Add(instruction);
                    temp.Parent = instruction;
                    stack.Push(temp);
                    break;
                case OpcodeMathNegate _:
                case OpcodeLogicToBool _:
                case OpcodeLogicNot _:
                    temp = CreateTemp();
                    instruction = new IRUnaryOp(temp, opcode, PopStack());
                    temp.Parent = instruction;
                    //currentBlock.Add(instruction);
                    stack.Push(temp);
                    break;
                case OpcodeCall call:
                    temp = CreateTemp();
                    Stack<IRValue> arguments = new Stack<IRValue>();
                    bool hasArgmarker = stack.Count > 0;   // Not even an argument marker on the stack - the dominator block must have it
                    while (stack.Count > 0)
                    {
                        IRValue stackResult = PopStack();
                        if (stackResult is IRConstant constant && constant.Value is Execution.KOSArgMarkerType)
                            break;
                        arguments.Push(stackResult);
                    }
                    instruction = new IRCall(temp, call, hasArgmarker, arguments);
                    if (stack.Count > 0 && !((IRCall)instruction).Direct)
                    {
                        ((IRCall)instruction).IndirectMethod = PopStack();
                    }
                    temp.Parent = instruction;
                    stack.Push(temp);
                    break;
                case OpcodeReturn opcodeReturn:
                    currentBlock.Add(new IRReturn(opcodeReturn.Depth, opcodeReturn) { Value = PopStack() });
                    break;
                case OpcodePush opcodePush:
                    object argument = opcodePush.Argument;
                    if (argument is string identifier && identifier.StartsWith("$"))
                        stack.Push(currentBlock.PushVariable(identifier, opcodePush));
                    else
                        stack.Push(new IRConstant(argument, opcodePush));
                    break;
                case OpcodePushDelegateRelocateLater delegateRelocateLater:
                    stack.Push(new IRDelegateRelocateLater(delegateRelocateLater.DestinationLabel, delegateRelocateLater.WithClosure, delegateRelocateLater));
                    break;
                case OpcodePushRelocateLater relocateLater:
                    stack.Push(new IRRelocateLater(relocateLater.DestinationLabel, relocateLater));
                    break;
                case OpcodeAddTrigger _:
                case OpcodeRemoveTrigger _:
                    currentBlock.Add(new IRUnaryConsumer(opcode, PopStack(), false));
                    break;
                case OpcodeWait _:
                    currentBlock.Add(new IRUnaryConsumer(opcode, PopStack(), true));
                    break;
                case OpcodePop pop:
                    currentBlock.Add(new IRPop(PopStack(), pop));
                    break;
                default:
                    throw new NotImplementedException($"The Opcode of type {opcode.GetType()} is not implemented.");
            }
        }
    }
}