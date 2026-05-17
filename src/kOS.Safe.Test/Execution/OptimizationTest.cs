using System;
using System.Collections.Generic;
using NUnit.Framework;
using kOS.Safe.Compilation;
using kOS.Safe.Compilation.IR;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Test.Execution
{
    [TestFixture]
    public class OptimizationTest : BaseIntegrationTest
    {
        protected override OptimizationLevel OptimizationLevel => optimizationLevel;
        protected OptimizationLevel optimizationLevel = OptimizationLevel.Minimal;

        protected List<CodePart> CompileCodePart(string fileName)
        {
            string contents = System.IO.File.ReadAllText(System.IO.Path.Combine(baseDir, fileName));
            Safe.Persistence.GlobalPath path = shared.VolumeMgr.GlobalPathFromObject("0:/" + fileName);
            var compiled = shared.ScriptHandler.Compile(path, 1, contents, "test", new CompilerOptions()
            {
                LoadProgramsInSameAddressSpace = false,
                IsCalledFromRun = false,
                FuncManager = shared.FunctionManager,
                BindManager = shared.BindingMgr,
                AllowClobberBuiltins = Utilities.SafeHouse.Config.AllowClobberBuiltIns,
                OptimizationLevel = OptimizationLevel
            });

            return compiled;
        }
        public List<Safe.Compilation.Opcode> GetOpcodesAfterOptimization(List<Safe.Compilation.Opcode> code, OptimizationLevel optimizationLevel = OptimizationLevel.Minimal)
        {
            IRCodePart codePart = new IRCodePart(code, new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            var optimizer = new Safe.Compilation.Optimization.Optimizer(optimizationLevel);
            optimizer.Optimize(codePart);
            CodePart result = new CodePart();
            codePart.EmitCode(result);
            return result.MainCode;
        }

        #region Holistic Tests
        [Test]
        public void TestSSA()
        {
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> _code = CompileCodePart("integration/blank.ks");
            optimizationLevel = OptimizationLevel.Minimal;
            IRCodePart codePart = new IRCodePart(_code[0], new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            BasicBlock block = codePart.MainCode[0];
            List<IRInstruction> instructions = new List<IRInstruction>
            {
                // Block 1: Local store will be SSA
                new IRAssign(block, new OpcodeStoreLocal("a"), new InterimConstantValue(Encapsulation.ScalarIntValue.One, -1, -1)) {Scope = IRAssign.StoreScope.Local},
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), true, new InterimVariableReference("a", -1, -1)), new OpcodePop()),
                // Block 2 & 3: Store exist should be SSA and overwrite Block 1
                new IRAssign(block, new OpcodeStoreExist("a"), new InterimConstantValue(Encapsulation.ScalarIntValue.Two, -1, -1)),
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), true, new InterimVariableReference("a", -1, -1)), new OpcodePop()),
                // Block 4: The variable "a" should now refer to a global, unresolved reference.
                new IRUnset(block, new OpcodeUnset(), new InterimConstantValue("a", -1, -1)),
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), true, new InterimVariableReference("a", -1, -1)), new OpcodePop()),
                // Block 5: The variable "a" should now refer to a global reference, which will not be resolved.
                new IRAssign(block, new OpcodeStore("a"), new InterimConstantValue(Encapsulation.ScalarIntValue.One, -1, -1)),
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), true, new InterimVariableReference("a", -1, -1)), new OpcodePop()),
                // Block 6: The variable "a" exists again at the local scope and should be resolved.
                new IRAssign(block, new OpcodeStoreLocal("a"), new InterimConstantValue(Encapsulation.ScalarIntValue.One, -1, -1)) {Scope = IRAssign.StoreScope.Local},
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), true, new InterimVariableReference("a", -1, -1)), new OpcodePop())

            };
            block.Instructions.InsertRange(1, instructions);
            SingleStaticAssignment.FinalizeSSA(codePart);
            instructions = codePart.MainCode[0].Instructions;

            // Block 1
            Assert.IsInstanceOf<IRPop>(instructions[2]);
            Assert.IsInstanceOf<IRCall>((instructions[2] as IRPop)?.Value);
            Assert.IsInstanceOf<InterimResolvedReference>(((instructions[2] as IRPop)?.Value as IRCall)?.Arguments[0]);
            // Block 2
            Assert.IsInstanceOf<IRPop>(instructions[4]);
            Assert.IsInstanceOf<IRCall>((instructions[4] as IRPop)?.Value);
            Assert.IsInstanceOf<InterimResolvedReference>(((instructions[4] as IRPop)?.Value as IRCall)?.Arguments[0]);
            // Block 3
            Assert.IsNotNull(((InterimResolvedReference)((instructions[2] as IRPop)?.Value as IRCall)?.Arguments[0]).Reference);
            Assert.IsNotNull(((InterimResolvedReference)((instructions[4] as IRPop)?.Value as IRCall)?.Arguments[0]).Reference);
            Assert.AreNotEqual(((InterimResolvedReference)((instructions[2] as IRPop)?.Value as IRCall)?.Arguments[0]).Reference,
                ((InterimResolvedReference)((instructions[4] as IRPop)?.Value as IRCall)?.Arguments[0]).Reference);
            // Block 4
            Assert.IsInstanceOf<IRPop>(instructions[6]);
            Assert.IsInstanceOf<IRCall>((instructions[6] as IRPop)?.Value);
            Assert.IsInstanceOf<InterimVariableReference>(((instructions[6] as IRPop)?.Value as IRCall)?.Arguments[0]);
            // Block 5
            Assert.IsInstanceOf<IRPop>(instructions[8]);
            Assert.IsInstanceOf<IRCall>((instructions[8] as IRPop)?.Value);
            Assert.IsInstanceOf<InterimVariableReference>(((instructions[8] as IRPop)?.Value as IRCall)?.Arguments[0]);
            // Block 6
            Assert.IsInstanceOf<IRPop>(instructions[10]);
            Assert.IsInstanceOf<IRCall>((instructions[10] as IRPop)?.Value);
            Assert.IsInstanceOf<InterimResolvedReference>(((instructions[10] as IRPop)?.Value as IRCall)?.Arguments[0]);
        }

        [Test]
        public void TestBasic()
        {
            // Tests that the bare minimum works

            RunScript("integration/basic.ks");
            RunSingleStep();
            AssertOutput(
                "text"
            );
        }

        [Test]
        public void TestVars()
        {
            // Tests that basic variable assignment and reference works

            RunScript("integration/vars.ks");
            RunSingleStep();
            AssertOutput(
                "1",
                "2",
                "3"
            );
        }

        [Test]
        public void TestFunc()
        {
            // Tests that basic no-args function calls work

            RunScript("integration/func.ks");
            RunSingleStep();
            AssertOutput(
                "a"
            );
        }

        [Test]
        public void TestFuncArgs()
        {
            // Tests that explicit and default function parameters work

            RunScript("integration/func_args.ks");
            RunSingleStep();
            AssertOutput(
                "0",
                "1",
                "2",
                "3"
            );
        }

        [Test]
        public void TestOperators()
        {
            // Test that all the basic operators work

            RunScript("integration/operators.ks");
            RunSingleStep();
            AssertOutput(
                "1",
                "1",
                "True",
                "True",
                "3",
                "6",
                "2",
                "9",
                "True",
                "True",
                "True",
                "True",
                "True",
                "True",
                "True",
                "True",
                "Ab",
                "0",
                "1",
                "0",
                "1"
            );
        }

        [Test]
        public void TestOperatorsException()
        {
            // Test that an invalid operation throws a compile error during constant folding
            Assert.Throws<KOSBinaryOperandTypeException>(() =>
            {
                RunScript("integration/operators_invalid.ks");
            });
        }

        [Test]
        public void TestLock()
        {
            // Test that locks in the same file works
            RunScript("integration/lock.ks");
            RunSingleStep();
            AssertOutput(
                "3",
                "4",
                "5"
            );
        }

        [Test]
        public void TestSuffixes()
        {
            // Test that various suffix and index combinations work for getting and setting
            RunScript("integration/suffixes.ks");
            RunSingleStep();
            RunSingleStep();
            AssertOutput(
                "0",
                "1",
                "2",
                "3",
                "0",
                "False"
            );
        }
        #endregion

        #region Suffix Replacement
        [Test]
        public void TestSuffixReplacement()
        {
            // Test that certain suffixes are replaced
            RunScript("integration/suffixReplacement.ks");
            RunSingleStep();
            AssertOutput(
                "9.80665",
                "3.14159265358979",
                "100"
            );
        }

        [Test]
        public void TestSuffixReplacement_Inspect()
        {
            // Tests that ship suffixes are replaced with direct accesses.
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePush("$ship"),
                    new OpcodeGetMember("name"),
                    new OpcodePop(),
                    new OpcodePush("$ship"),
                    new OpcodeGetMember("body"),
                    new OpcodePop(),
                    new OpcodePush("$ship"),
                    new OpcodeGetMember("nonexistentSuffix"),
                    new OpcodePop()
                }
            );

            Assert.IsInstanceOf<OpcodePush>(result[0]);
            Assert.IsInstanceOf<string>((result[0] as OpcodePush)?.Argument);
            Assert.That("$shipname".Equals((result[0] as OpcodePush)?.Argument as string, StringComparison.OrdinalIgnoreCase));

            Assert.IsInstanceOf<OpcodePush>(result[2]);
            Assert.IsInstanceOf<string>((result[2] as OpcodePush)?.Argument);
            Assert.That("$body".Equals((result[2] as OpcodePush)?.Argument as string, StringComparison.OrdinalIgnoreCase));

            Assert.IsInstanceOf<OpcodePush>(result[4]);
            Assert.IsInstanceOf<string>((result[4] as OpcodePush)?.Argument);
            Assert.That("$ship".Equals((result[4] as OpcodePush)?.Argument as string, StringComparison.OrdinalIgnoreCase));

            Assert.IsInstanceOf<OpcodeGetMember>(result[5]);
        }
        #endregion

        #region Peephole Optimizations
        [Test]
        public void TestPeepholeOptimizations()
        {
            HashSet<Type> passesToSkip = Safe.Compilation.Optimization.Optimizer.PassesToSkip;
            passesToSkip.Add(typeof(Safe.Compilation.Optimization.Passes.ConstantFolding));
            // Test that certain suffixes are replaced
            RunScript("integration/peepholeOptimizations.ks");
            RunSingleStep();
            AssertOutput(
                "String",
                "6",
                "6",
                "Print this.",
                "21",
                "21",
                "21",
                "21",
                "3",
                "-3",
                "7",
                "16",
                "125",
                "64",
                "3125",
                "256",
                "4",
                "25",
                "125",
                "125",
                "512"
            );
            passesToSkip.Clear();
        }

        [Test]
        public void TestParameterlessSuffixMethodReplacement()
        {
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush("$ship"),
                    new OpcodeGetMethod("method"),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("<indirect>"),
                    new OpcodePop(),
                    new OpcodePush("$ship"),
                    new OpcodeGetMethod("method"),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodePush(new Safe.Compilation.PseudoNull()),
                    new OpcodeCall("<indirect>"),
                    new OpcodePop(),
                    new OpcodePopScope()
                }
            );
            // The first one should be replaced
            Assert.IsInstanceOf<OpcodeGetMember>(result[1]);
            Assert.IsNotInstanceOf<OpcodeGetMethod>(result[1]);
            // The second one should not.
            Assert.IsInstanceOf<OpcodeGetMethod>(result[4]);
        }

        [Test]
        public void TestVectorDotProductReplacement()
        {
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeCall("vectordotproduct"),
                    new OpcodePop(),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeCall("vdot"),
                    new OpcodePop(),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodePush("$c"),
                    new OpcodeCall("vectordotproduct"),
                    new OpcodePop(),
                    new OpcodePopScope()
                }
            );
            
            Assert.IsInstanceOf<OpcodeMathMultiply>(result[2]);
            Assert.IsInstanceOf<OpcodeMathMultiply>(result[6]);
            Assert.IsInstanceOf<OpcodeCall>(result[12]);
        }

        [Test]
        public void TestRedundantUnaryOps()
        {
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush("$a"),
                    new OpcodeMathNegate(),
                    new OpcodeMathNegate(),
                    new OpcodePop(),
                    new OpcodePush("$b"),
                    new OpcodeLogicNot(),
                    new OpcodeLogicNot(),
                    new OpcodePop(),
                    new OpcodePopScope()
                }
            );
            Assert.IsInstanceOf<OpcodePop>(result[1]);
            Assert.IsInstanceOf<OpcodePop>(result[3]);
        }

        [Test]
        public void TestLexIndexReplacement()
        {
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush("$a"),
                    new OpcodePush(new Encapsulation.StringValue("test")),
                    new OpcodeGetIndex(),
                    new OpcodePop(),
                    new OpcodePopScope()
                }
            );
            Assert.IsInstanceOf<OpcodeGetMember>(result[1]);
            Assert.That((result[1] as OpcodeGetMember)?.Identifier is string index &&
                index.Equals("test", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void TestMultiplicationDistribution()
        {
            // A*B + A*C = A*(B+C)
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Add(typeof(Safe.Compilation.Optimization.Passes.ConstantFolding));
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeStoreLocal("$a"),
                    new OpcodePush(Encapsulation.ScalarIntValue.One),
                    new OpcodeStoreLocal("$b"),
                    new OpcodePush(new Encapsulation.ScalarIntValue(3)),
                    new OpcodeStoreLocal("$c"),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathMultiply(),
                    new OpcodePush("$a"),
                    new OpcodePush("$c"),
                    new OpcodeMathMultiply(),
                    new OpcodeMathAdd(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathMultiply(),
                    new OpcodePush("$a"),
                    new OpcodePush("$c"),
                    new OpcodeMathMultiply(),
                    new OpcodeMathSubtract(),
                    new OpcodePop(),
                    new OpcodePopScope()
                }
            );
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Clear();
            Assert.IsInstanceOf<OpcodeMathAdd>(result[10]);
            Assert.IsInstanceOf<OpcodeMathMultiply>(result[11]);
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[16]);
            Assert.IsInstanceOf<OpcodeMathMultiply>(result[17]);
        }

        [Test]
        public void TestAdditionSubtraction()
        {
            // -B+A = A+-B = A-B
            // -A+B = B-A
            // A--B=A+B
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Add(typeof(Safe.Compilation.Optimization.Passes.ConstantFolding));
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeStoreLocal("$a"),
                    new OpcodePush(Encapsulation.ScalarIntValue.One),
                    new OpcodeStoreLocal("$b"),
                    new OpcodePush("$b"),
                    new OpcodeMathNegate(),
                    new OpcodePush("$a"),
                    new OpcodeMathAdd(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathNegate(),
                    new OpcodeMathAdd(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodeMathNegate(),
                    new OpcodePush("$b"),
                    new OpcodeMathAdd(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathNegate(),
                    new OpcodeMathSubtract(),
                    new OpcodePopScope()
                }
            );
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Clear();
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[7]);
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[11]);
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[15]);
        }

        [Test]
        public void TestDivisionSubtraction()
        {
            // X^N/X=X^(N-1)
            // X^N/X^M=X^(N-M)
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Add(typeof(Safe.Compilation.Optimization.Passes.ConstantFolding));
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeStoreLocal("$a"),
                    new OpcodePush(new Encapsulation.ScalarDoubleValue(2.5)),
                    new OpcodeStoreLocal("$b"),
                    new OpcodePush(new Encapsulation.ScalarDoubleValue(3)),
                    new OpcodeStoreLocal("$c"),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathPower(),
                    new OpcodePush("$a"),
                    new OpcodeMathDivide(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathPower(),
                    new OpcodePush("$a"),
                    new OpcodePush("$c"),
                    new OpcodeMathPower(),
                    new OpcodeMathDivide(),
                    new OpcodePop(),
                    new OpcodePopScope()
                }
            );
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Clear();
            Assert.IsInstanceOf<OpcodePush>(result[8]);
            Assert.AreEqual((result[8] as OpcodePush)?.Argument, new Encapsulation.ScalarDoubleValue(1.5));
            Assert.IsInstanceOf<OpcodePush>(result[12]);
            Assert.AreEqual((result[12] as OpcodePush)?.Argument, new Encapsulation.ScalarDoubleValue(-0.5));
        }

        [Test]
        public void TestPowerAddition()
        {
            // X^N*X=X^(N+1)
            // X^N*X^M=X^(N+M)
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Add(typeof(Safe.Compilation.Optimization.Passes.ConstantFolding));
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeStoreLocal("$a"),
                    new OpcodePush(new Encapsulation.ScalarDoubleValue(2.5)),
                    new OpcodeStoreLocal("$b"),
                    new OpcodePush(new Encapsulation.ScalarDoubleValue(3)),
                    new OpcodeStoreLocal("$c"),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathPower(),
                    new OpcodePush("$a"),
                    new OpcodeMathMultiply(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathPower(),
                    new OpcodePush("$a"),
                    new OpcodePush("$c"),
                    new OpcodeMathPower(),
                    new OpcodeMathMultiply(),
                    new OpcodePop(),
                    new OpcodePopScope()
                }
            );
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Clear();
            Assert.IsInstanceOf<OpcodePush>(result[8]);
            Assert.AreEqual((result[8] as OpcodePush)?.Argument, new Encapsulation.ScalarDoubleValue(3.5));
            Assert.IsInstanceOf<OpcodePush>(result[12]);
            Assert.AreEqual((result[12] as OpcodePush)?.Argument, new Encapsulation.ScalarDoubleValue(5.5));
        }

        [Test]
        public void TestPowerCreation()
        {
            // X*X*X = X^3
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Add(typeof(Safe.Compilation.Optimization.Passes.ConstantFolding));
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeStoreLocal("$a"),
                    new OpcodePush("$a"),
                    new OpcodePush("$a"),
                    new OpcodePush("$a"),
                    new OpcodeMathMultiply(),
                    new OpcodeMathMultiply(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeMathPower(),
                    new OpcodePush("$a"),
                    new OpcodeMathDivide(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodePush(new Encapsulation.ScalarIntValue(3)),
                    new OpcodeMathPower(),
                    new OpcodePush("$a"),
                    new OpcodeMathDivide(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeMathPower(),
                    new OpcodePush("$a"),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeMathPower(),
                    new OpcodeMathDivide(),
                    new OpcodePop(),
                    new OpcodePush("$a"),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeMathPower(),
                    new OpcodePush("$a"),
                    new OpcodeMathDivide(),
                    new OpcodePush("$a"),
                    new OpcodeMathDivide(),
                    new OpcodePop(),
                    new OpcodePopScope()
                }
            );
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Clear();
            Assert.IsInstanceOf<OpcodePush>(result[4]);
            Assert.AreEqual((result[4] as OpcodePush)?.Argument, new Encapsulation.ScalarIntValue(3));
            Assert.IsInstanceOf<OpcodeMathPower>(result[5]);
            Assert.IsInstanceOf<OpcodePush>(result[7]);
            Assert.IsInstanceOf<OpcodePop>(result[8]);
            Assert.IsInstanceOf<OpcodeMathMultiply>(result[11]);
            Assert.IsInstanceOf<OpcodePush>(result[13]);
            Assert.AreEqual((result[13] as OpcodePush)?.Argument, Encapsulation.ScalarIntValue.One);
            Assert.IsInstanceOf<OpcodePush>(result[15]);
            Assert.AreEqual((result[15] as OpcodePush)?.Argument, Encapsulation.ScalarIntValue.One);
        }
        #endregion

        #region Constant Propagation
        [Test]
        public void TestConstantPropagation()
        {
            // Test that local constant variables propagate
            RunScript("integration/constantPropagation.ks");
            RunSingleStep();
            AssertOutput(
                "test",
                "6",
                "False",
                "6",
                "9",
                "7",
                "10",
                "14",
                "True",
                "False",
                "7",
                "6",
                "11",
                "9",
                "5",
                "6",
                "1"
            );
        }

        [Test]
        public void TestConstantPropagationAndFolding()
        {
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(Encapsulation.ScalarIntValue.One),
                    new OpcodeStoreLocal("$b"),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeStoreGlobal("$g"),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeStoreLocal("$c"),
                    new OpcodePush("$b"),
                    new OpcodePush("$c"),
                    new OpcodeMathAdd(),
                    new OpcodeStoreLocal("$d"),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathAdd(),
                    new OpcodeStoreLocal("$e"),
                    new OpcodePush("$b"),
                    new OpcodePush("$g"),
                    new OpcodeMathAdd(),
                    new OpcodeStoreLocal("$f"),
                    new OpcodePopScope()
                }
            );
            Assert.IsInstanceOf<OpcodePush>(result[7]);
            Assert.AreEqual((result[7] as OpcodePush)?.Argument, new Encapsulation.ScalarIntValue(3));
            Assert.IsInstanceOf<OpcodeMathAdd>(result[11]);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[15]);
        }
        #endregion
    }
}
