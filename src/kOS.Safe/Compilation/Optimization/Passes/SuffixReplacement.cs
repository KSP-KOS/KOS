using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class SuffixReplacement : IOptimizationPass<IRInstruction>
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;
        public short SortIndex => 10;

        public void ApplyPass(List<IRInstruction> code)
        {
            for (int i = 0; i < code.Count; i++)
            {
                IRInstruction instruction = code[i];
                switch (instruction)
                {
                    case IRPop pop:
                        if (AttemptReplacement(pop.Value) is IRConstant)
                        {
                            code.RemoveAt(i);
                            i--;
                        }
                        continue;
                    case IRAssign assign:
                        assign.Value = AttemptReplacement(assign.Value);
                        break;
                    case IRSuffixSet suffixSet:
                        suffixSet.Value = AttemptReplacement(suffixSet.Value);
                        suffixSet.Object = AttemptReplacement(suffixSet.Object);
                        break;
                    case IRIndexSet indexSet:
                        indexSet.Value = AttemptReplacement(indexSet.Value);
                        indexSet.Object = AttemptReplacement(indexSet.Object);
                        indexSet.Index = AttemptReplacement(indexSet.Index);
                        break;
                    case IRBranch branch:
                        AttemptReplacement(branch.Condition);
                        break;
                }
            }
        }

        private static IRValue AttemptReplacement(IRValue input)
        {
            if (input is IRTemp temp)
            {
                if (temp.Parent is IRSuffixGet suffixGet)
                {
                    AttempReplaceSuffix(suffixGet);
                }
                else
                {
                    AttemptReplacement(temp.Parent);
                }
            }
            return input;
        }
        private static IRValue AttempReplaceSuffix(IRSuffixGet suffixGet)
        {
            if (suffixGet.Object is IRVariable objVariable)
            {
                if (objVariable.Name == "$ship")
                {
                    switch (suffixGet.Suffix)
                    {
                        case "name":
                            // Rename the alias shortcut appropriately
                            suffixGet.Suffix = "shipname";
                            break;
                        case "heading":
                        case "prograde":
                        case "retrograde":
                        case "facing":
                        case "maxthrust":
                        case "velocity":
                        case "geoposition":
                        case "latitude":
                        case "longitude":
                        case "up":
                        case "north":
                        case "body":
                        case "angularmomentum":
                        case "angularvel":
                        case "mass":
                        case "verticalSpeed":
                        case "groundspeed":
                        case "airspeed":
                        case "altitude":
                        case "apoapsis":
                        case "periapsis":
                        case "sensors":
                        case "srfprograde":
                        case "srfretrograde":
                        case "obt":
                        case "status":
                            break;
                        // All other suffixes don't have an alias, so just return the original IRTemp
                        default:
                            return suffixGet.Result;
                    }
                    // Instead of the IRTemp, which leads to resolving the suffix, return just the alias shortcut.
                    suffixGet.Result = new IRVariable($"${suffixGet.Suffix}", null, suffixGet.SourceLine, suffixGet.SourceColumn);
                    return suffixGet.Result;
                }
                if (objVariable.Name == "$constant")
                {
                    return ReplaceConstantSuffix(suffixGet);
                }
            }
            else if (suffixGet.Object is IRTemp tempObj && tempObj.Parent is IRCall call && call.Function == "constant()" && call.Arguments.Count == 0)
            {
                return ReplaceConstantSuffix(suffixGet);
            }
            return suffixGet.Result;
        }
        private static IRConstant ReplaceConstantSuffix(IRSuffixGet suffixGet)
        {
            Optimizer.InterimCPU.PushArgumentStack(new Encapsulation.ConstantValue());
            try
            {
                new OpcodeGetMember(suffixGet.Suffix).Execute(Optimizer.InterimCPU);
            }
            catch (Exception e)
            {
                throw new Exceptions.KOSCompileException(suffixGet, e);
            }
            IRConstant result = new IRConstant(Optimizer.InterimCPU.PopValueArgument(), suffixGet);
            suffixGet.Result = result;
            return result;
        }
        private static IRValue AttemptReplacement(IRInstruction instruction)
        {
            switch (instruction)
            {
                case IRUnaryOp unaryOp:
                    return ReduceUnary(unaryOp);
                case IRBinaryOp binaryOp:
                    return ReduceBinary(binaryOp);
                case IRSuffixGet suffixGet:
                    return ReduceSuffixGet(suffixGet);
                case IRIndexGet indexGet:
                    return ReduceIndexGet(indexGet);
                case IRCall call:
                    return ReduceCall(call);
                case IResultingInstruction resulting:
                    return resulting.Result;
            }
            throw new ArgumentException($"{instruction.GetType()} is not supported.");
        }
        private static IRValue ReduceUnary(IRUnaryOp instruction)
        {
            if (instruction.Operand is IRTemp temp)
                instruction.Operand = AttemptReplacement(temp);

            return instruction.Result;
        }
        private static IRValue ReduceBinary(IRBinaryOp instruction)
        {
            if (instruction.Left is IRTemp tempL)
                instruction.Left = AttemptReplacement(tempL);
            if (instruction.Right is IRTemp tempR)
                instruction.Right = AttemptReplacement(tempR);

            return instruction.Result;
        }

        private static IRValue ReduceSuffixGet(IRSuffixGet instruction)
        {
            if (instruction.Object is IRTemp temp)
                instruction.Object = AttemptReplacement(temp);

            return instruction.Result;
        }
        private static IRValue ReduceIndexGet(IRIndexGet instruction)
        {
            if (instruction.Object is IRTemp tempObj)
                instruction.Object = AttemptReplacement(tempObj);

            if (instruction.Index is IRTemp tempIndex)
                instruction.Index = AttemptReplacement(tempIndex);

            return instruction.Result;
        }
        private static IRValue ReduceCall(IRCall instruction)
        {
            for (int i = instruction.Arguments.Count - 1; i >= 0; i--)
            {
                if (instruction.Arguments[i] is IRTemp temp)
                    instruction.Arguments[i] = AttemptReplacement(temp);
            }

            return instruction.Result;
        }
    }
}
