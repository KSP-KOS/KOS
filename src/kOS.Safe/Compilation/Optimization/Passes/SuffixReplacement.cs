using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class SuffixReplacement : IOptimizationPass<IRInstruction>
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;
        public short SortIndex => 10;

        public void ApplyPass(IEnumerable<IRInstruction> codeList)
        {
            List<IRInstruction> code = (List<IRInstruction>)codeList;
            for (int i = 0; i < code.Count; i++)
            {
                IRInstruction instruction = code[i];
                foreach (IOperandInstructionBase operandInstruction in instruction.DepthFirst())
                {
                    operandInstruction.MutateEachOperand(AttemptReplacement);
                }
            }
        }

        private static IInterimOperand AttemptReplacement(IInterimOperand operand)
        {
            if (operand is IRSuffixGet suffixGet)
                return AttempReplaceSuffix(suffixGet);
            return operand;
        }
        private static IInterimOperand AttempReplaceSuffix(IRSuffixGet suffixGet)
        {
            if (suffixGet.Object is InterimVariableReference variableReference)
            {
                if (variableReference.Name.Equals("$ship", StringComparison.OrdinalIgnoreCase))
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
                        // All other suffixes don't have an alias, so just return the original 
                        default:
                            return suffixGet;
                    }
                    return new InterimVariableReference($"${suffixGet.Suffix}", suffixGet.SourceLine, suffixGet.SourceColumn);
                }
                if (variableReference.Name.Equals("$constant", StringComparison.OrdinalIgnoreCase))
                {
                    return ReplaceConstantSuffix(suffixGet);
                }
            }
            else if (suffixGet.Object is IRCall call && call.Function.Equals("constant()", StringComparison.OrdinalIgnoreCase) && call.Arguments.Count == 0)
            {
                return ReplaceConstantSuffix(suffixGet);
            }
            return suffixGet;
        }
        private static InterimConstantValue ReplaceConstantSuffix(IRSuffixGet suffixGet)
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
            return new InterimConstantValue(Optimizer.InterimCPU.PopValueArgument(), suffixGet);
        }
    }
}
