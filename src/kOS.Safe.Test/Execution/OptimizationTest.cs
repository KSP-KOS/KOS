using System;
using System.Collections.Generic;
using NUnit.Framework;
using kOS.Safe.Compilation;
using kOS.Safe.Compilation.IR;
using kOS.Safe.Exceptions;
using System.Linq;

namespace kOS.Safe.Test.Execution
{
    [TestFixture]
    public class OptimizationTest : BaseIntegrationTest
    {
        protected override OptimizationLevel OptimizationLevel => optimizationLevel;
        protected OptimizationLevel optimizationLevel = OptimizationLevel.Balanced;
        protected OptimizationLevel resetOptimizationLevel = OptimizationLevel.Balanced;

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
        public List<Safe.Compilation.Opcode> GetOpcodesAfterOptimization(List<Safe.Compilation.Opcode> code,
            OptimizationLevel optimizationLevel = OptimizationLevel.Minimal,
            bool allowClobberBuiltins = true)
        {
            CompilerOptions options = new CompilerOptions()
            {
                AllowClobberBuiltins = allowClobberBuiltins,
                OptimizationLevel = optimizationLevel
            };
            IRCodePart codePart = new IRCodePart(code, new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            var optimizer = new Safe.Compilation.Optimization.Optimizer(options);
            optimizer.Optimize(codePart);
            CodePart result = new CodePart();
            codePart.EmitCode(result);
            return result.MainCode;
        }

        protected void EnsureAtLeastLevel(OptimizationLevel level)
        {
            if (optimizationLevel < level)
                optimizationLevel = level;
        }

        [SetUp]
        public void ResetOptimizationBases()
        {
            BasicBlock.ResetNextID();
            optimizationLevel = resetOptimizationLevel;
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Clear();
        }

        #region Holistic Tests
        [Test]
        public void TestSSA()
        {
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> _code = CompileCodePart("integration/blank.ks");
            IRCodePart codePart = new IRCodePart(_code[0], new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            BasicBlock block = codePart.MainCode[0];
            List<IRInstruction> instructions = new List<IRInstruction>
            {
                // Block 1: Local store will be SSA
                new IRAssign(block, new OpcodeStoreLocal("a"), new InterimConstantValue(Encapsulation.ScalarIntValue.One, -1, -1)) {Scope = IRAssign.StoreScope.Local},
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), new InterimVariableReference("a", -1, -1)), new OpcodePop()),
                // Block 2 & 3: Store exist should be SSA and overwrite Block 1
                new IRAssign(block, new OpcodeStoreExist("a"), new InterimConstantValue(Encapsulation.ScalarIntValue.Two, -1, -1)),
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), new InterimVariableReference("a", -1, -1)), new OpcodePop()),
                // Block 4: The variable "a" should now refer to a global, unresolved reference.
                new IRUnset(block, new OpcodeUnset(), new InterimConstantValue("a", -1, -1)),
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), new InterimVariableReference("a", -1, -1)), new OpcodePop()),
                // Block 5: The variable "a" should now refer to a global reference, which will not be resolved.
                new IRAssign(block, new OpcodeStore("a"), new InterimConstantValue(Encapsulation.ScalarIntValue.One, -1, -1)),
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), new InterimVariableReference("a", -1, -1)), new OpcodePop()),
                // Block 6: The variable "a" exists again at the local scope and should be resolved.
                new IRAssign(block, new OpcodeStoreLocal("a"), new InterimConstantValue(Encapsulation.ScalarIntValue.One, -1, -1)) {Scope = IRAssign.StoreScope.Local},
                new IRPop(block, new IRCall(block, new OpcodeCall("print"), new InterimVariableReference("a", -1, -1)), new OpcodePop())

            };
            block.Instructions.InsertRange(1, instructions);
            SingleStaticAssignment.FinalizeSSA(codePart, false);
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
                },
                allowClobberBuiltins: false
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
                },
                allowClobberBuiltins: false
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
        #endregion

        #region Constant Propagation, Folding, and Algebraic Simplifications
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
                    new OpcodeStoreGlobal("$d"),
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathAdd(),
                    new OpcodeStoreGlobal("$e"),
                    new OpcodePush("$b"),
                    new OpcodePush("$g"),
                    new OpcodeMathAdd(),
                    new OpcodeStoreGlobal("$f"),
                    new OpcodePopScope()
                }, OptimizationLevel.Balanced
            );
            Assert.IsInstanceOf<OpcodePush>(result[2]);
            Assert.AreEqual(new Encapsulation.ScalarIntValue(3), (result[2] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[6]);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[10]);
        }

        [Test]
        public void TestConstantPropagationCommutivity()
        {
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("random"),
                    new OpcodeStoreLocal("$a"),
                    // Block 1
                    new OpcodePush(Encapsulation.ScalarIntValue.One),
                    new OpcodePush("$a"),
                    new OpcodeMathAdd(),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeMathAdd(),
                    new OpcodeStoreGlobal("$result"),
                    // Block 2
                    new OpcodePush(Encapsulation.ScalarIntValue.One),
                    new OpcodePush("$a"),
                    new OpcodeMathAdd(),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeMathSubtract(),
                    new OpcodeStoreExist("$result"),
                    // Block 3
                    new OpcodePush(Encapsulation.ScalarIntValue.One),
                    new OpcodePush("$a"),
                    new OpcodeMathSubtract(),
                    new OpcodeStoreExist("$result"),
                    // Block 4
                    new OpcodePush("$a"),
                    new OpcodePush(Encapsulation.ScalarIntValue.One),
                    new OpcodeMathSubtract(),
                    new OpcodeStoreExist("$result"),
                    // Block 5
                    new OpcodePush(Encapsulation.ScalarIntValue.One),
                    new OpcodePush("$a"),
                    new OpcodeMathSubtract(),
                    new OpcodePush(Encapsulation.ScalarIntValue.Two),
                    new OpcodeMathAdd(),
                    new OpcodeStoreExist("$result"),
                    new OpcodePopScope()
                }
            );
            // Block 1
            Assert.AreEqual(new Encapsulation.ScalarIntValue(3), (result[4] as OpcodePush)?.Argument);
            Assert.AreEqual("$a", (result[5] as OpcodePush)?.Argument);
            // Block 2
            Assert.AreEqual(new Encapsulation.ScalarIntValue(-1), (result[8] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[10]);
            //Block 3
            Assert.AreEqual(Encapsulation.ScalarIntValue.One, (result[12] as OpcodePush)?.Argument);
            Assert.AreEqual("$a", (result[13] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[14]);
            //Block 4
            Assert.AreEqual(new Encapsulation.ScalarIntValue(-1), (result[16] as OpcodePush)?.Argument);
            Assert.AreEqual("$a", (result[17] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[18]);
            //Block 5
            Assert.AreEqual(new Encapsulation.ScalarIntValue(3), (result[20] as OpcodePush)?.Argument);
            Assert.AreEqual("$a", (result[21] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[22]);
        }

        [Test]
        public void TestMultiplicationDistribution()
        {
            // A*B + A*C = A*(B+C)
            // A/B + C/B = (A+C)/B
            // A/B - C/B = (A-C)/B
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("random"),
                    new OpcodeStoreLocal("$a"),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("random"),
                    new OpcodeStoreLocal("$b"),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("random"),
                    new OpcodeStoreLocal("$c"),
                    // Block 1
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathMultiply(),
                    new OpcodePush("$a"),
                    new OpcodePush("$c"),
                    new OpcodeMathMultiply(),
                    new OpcodeMathAdd(),
                    new OpcodePop(),
                    // Block 2
                    new OpcodePush("$a"),
                    new OpcodePush("$b"),
                    new OpcodeMathMultiply(),
                    new OpcodePush("$a"),
                    new OpcodePush("$c"),
                    new OpcodeMathMultiply(),
                    new OpcodeMathSubtract(),
                    new OpcodePop(),
                    // Block 3
                    new OpcodePush("$b"),
                    new OpcodePush("$a"),
                    new OpcodeMathDivide(),
                    new OpcodePush("$c"),
                    new OpcodePush("$a"),
                    new OpcodeMathDivide(),
                    new OpcodeMathAdd(),
                    new OpcodePop(),
                    // Block 4
                    new OpcodePush("$b"),
                    new OpcodePush("$a"),
                    new OpcodeMathDivide(),
                    new OpcodePush("$c"),
                    new OpcodePush("$a"),
                    new OpcodeMathDivide(),
                    new OpcodeMathSubtract(),
                    new OpcodePop(),
                    new OpcodePopScope()
                }
            );
            // Block 1
            Assert.IsInstanceOf<OpcodePush>(result[10]);
            Assert.AreEqual("$a", (result[10] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[11]);
            Assert.AreEqual("$b", (result[11] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[12]);
            Assert.AreEqual("$c", (result[12] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[13]);
            Assert.IsInstanceOf<OpcodeMathMultiply>(result[14]);
            // Block 2
            Assert.IsInstanceOf<OpcodePush>(result[16]);
            Assert.AreEqual("$a", (result[16] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[17]);
            Assert.AreEqual("$b", (result[17] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[18]);
            Assert.AreEqual("$c", (result[18] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[19]);
            Assert.IsInstanceOf<OpcodeMathMultiply>(result[20]);
            // Block 3
            Assert.IsInstanceOf<OpcodePush>(result[22]);
            Assert.AreEqual("$b", (result[22] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[23]);
            Assert.AreEqual("$c", (result[23] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[25]);
            Assert.AreEqual("$a", (result[25] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[24]);
            Assert.IsInstanceOf<OpcodeMathDivide>(result[26]);
            // Block 4
            Assert.IsInstanceOf<OpcodePush>(result[28]);
            Assert.AreEqual("$b", (result[28] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[29]);
            Assert.AreEqual("$c", (result[29] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[31]);
            Assert.AreEqual("$a", (result[31] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[30]);
            Assert.IsInstanceOf<OpcodeMathDivide>(result[32]);
        }

        [Test]
        public void TestAdditionSubtraction()
        {
            // -B+A = A+-B = A-B
            // -A+B = B-A
            // A--B=A+B
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("random"),
                    new OpcodeStoreLocal("$a"),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("random"),
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
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[9]);
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[13]);
            Assert.IsInstanceOf<OpcodeMathSubtract>(result[17]);
        }

        [Test]
        public void TestDivisionSubtraction()
        {
            // X^N/X=X^(N-1)
            // X^N/X^M=X^(N-M)
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("random"),
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
                }, OptimizationLevel.Balanced
            );
            Assert.IsInstanceOf<OpcodePush>(result[5]);
            Assert.AreEqual(new Encapsulation.ScalarDoubleValue(1.5), (result[5] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[9]);
            Assert.AreEqual(new Encapsulation.ScalarDoubleValue(-0.5), (result[9] as OpcodePush)?.Argument);
        }

        [Test]
        public void TestPowerAddition()
        {
            // X^N*X=X^(N+1)
            // X^N*X^M=X^(N+M)
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("random"),
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
                }, OptimizationLevel.Balanced
            );
            Assert.IsInstanceOf<OpcodePush>(result[5]);
            Assert.AreEqual(new Encapsulation.ScalarDoubleValue(3.5), (result[5] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[9]);
            Assert.AreEqual(new Encapsulation.ScalarDoubleValue(5.5), (result[9] as OpcodePush)?.Argument);
        }

        [Test]
        public void TestPowerCreation()
        {
            // X*X*X = X^3
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(
                new List<Safe.Compilation.Opcode>()
                {
                    new OpcodePushScope(1, 0),
                    new OpcodePush(new Safe.Execution.KOSArgMarkerType()),
                    new OpcodeCall("random"),
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
                    // TODO: It should reduce this to Encapsulation.One
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
            Assert.IsInstanceOf<OpcodePush>(result[5]);
            Assert.AreEqual(new Encapsulation.ScalarIntValue(3), (result[5] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodeMathPower>(result[6]);
            Assert.IsInstanceOf<OpcodePush>(result[8]);
            Assert.IsInstanceOf<OpcodePop>(result[9]);
            Assert.IsInstanceOf<OpcodeMathMultiply>(result[12]);
            Assert.IsInstanceOf<OpcodePush>(result[14]);
            Assert.AreEqual(Encapsulation.ScalarIntValue.One, (result[14] as OpcodePush)?.Argument);
            Assert.IsInstanceOf<OpcodePush>(result[16]);
            Assert.AreEqual(Encapsulation.ScalarIntValue.One, (result[16] as OpcodePush)?.Argument);
        }
        #endregion

        #region Block Ordering
        [Test]
        public void TestUntilLoopDetection()
        {
            BasicBlock.ResetNextID();
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> _code = CompileCodePart("integration/branching/until.ks");
            ResetOptimizationBases();
            IRCodePart codePart = new IRCodePart(_code[0], new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyLoop(codePart.MainCode[1], null, out Safe.Compilation.Optimization.Passes.BlockOrdering.LoopData loopData);
            Assert.AreEqual(2, loopData.body?.ID);
            Assert.AreEqual(4, loopData.exit?.ID);
            Assert.AreEqual(1, codePart.MainCode.
                Where(b => Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyLoop(b, null, out _)).Count());
            var optimizer = new Safe.Compilation.Optimization.Optimizer(new CompilerOptions() { OptimizationLevel = OptimizationLevel });
            optimizer.Optimize(codePart);
        }
        [Test]
        public void TestForLoopDetection()
        {
            BasicBlock.ResetNextID();
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> _code = CompileCodePart("integration/branching/for.ks");
            IRCodePart codePart = new IRCodePart(_code[0], new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyLoop(codePart.MainCode[2], null, out Safe.Compilation.Optimization.Passes.BlockOrdering.LoopData loopData);
            Assert.AreEqual(3, loopData.body?.ID);
            Assert.AreEqual(6, loopData.exit?.ID);
            Assert.AreEqual(1, codePart.MainCode.
                Where(b => Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyLoop(b, null, out _)).Count());
        }
        [Test]
        public void TestFromLoopDetection()
        {
            BasicBlock.ResetNextID();
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> _code = CompileCodePart("integration/branching/from.ks");
            IRCodePart codePart = new IRCodePart(_code[0], new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyLoop(codePart.MainCode[2], null, out Safe.Compilation.Optimization.Passes.BlockOrdering.LoopData loopData);
            Assert.AreEqual(3, loopData.body?.ID);
            Assert.AreEqual(5, loopData.exit?.ID);
            Assert.AreEqual(1, codePart.MainCode.
                Where(b => Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyLoop(b, null, out _)).Count());
        }
        [Test]
        public void TestIfDetection()
        {
            BasicBlock.ResetNextID();
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> _code = CompileCodePart("integration/branching/if.ks");
            IRCodePart codePart = new IRCodePart(_code[0], new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(codePart.MainCode[0], null, out Safe.Compilation.Optimization.Passes.BlockOrdering.BranchData branchData);
            Assert.AreEqual(1, branchData.ifBlock?.ID);
            Assert.IsNull(branchData.elseBlock);
            Assert.AreEqual(2, branchData.exit?.ID);
            Assert.AreEqual(1, codePart.MainCode.
                Where(b => Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(b, null, out _)).Count());
        }
        [Test]
        public void TestIfElseDetection()
        {
            BasicBlock.ResetNextID();
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> _code = CompileCodePart("integration/branching/ifElse.ks");
            IRCodePart codePart = new IRCodePart(_code[0], new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(codePart.MainCode[0], null, out Safe.Compilation.Optimization.Passes.BlockOrdering.BranchData branchData);
            Assert.AreEqual(1, branchData.ifBlock?.ID);
            Assert.AreEqual(3, branchData.elseBlock?.ID);
            Assert.AreEqual(4, branchData.exit?.ID);
            Assert.AreEqual(1, codePart.MainCode.
                Where(b => Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(b, null, out _)).Count());
        }
        [Test]
        public void TestIfElseIfDetection()
        {
            BasicBlock.ResetNextID();
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> _code = CompileCodePart("integration/branching/ifElseIf.ks");
            IRCodePart codePart = new IRCodePart(_code[0], new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(codePart.MainCode[0], null, out Safe.Compilation.Optimization.Passes.BlockOrdering.BranchData branchData);
            Assert.AreEqual(1, branchData.ifBlock?.ID);
            Assert.AreEqual(3, branchData.elseBlock?.ID);
            Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(branchData.elseBlock, null, out Safe.Compilation.Optimization.Passes.BlockOrdering.BranchData branchData2);
            Assert.AreEqual(4, branchData2.ifBlock?.ID);
            Assert.IsNull(branchData2.elseBlock);
            Assert.AreSame(branchData.exit, branchData2.exit);
            Assert.AreEqual(5, branchData.exit?.ID);
            Assert.AreEqual(2, codePart.MainCode.
                Where(b => Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(b, null, out _)).Count());
        }
        [Test]
        public void TestIfElseIfElseDetection()
        {
            BasicBlock.ResetNextID();
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> _code = CompileCodePart("integration/branching/ifElseIfElse.ks");
            IRCodePart codePart = new IRCodePart(_code[0], new List<Safe.Compilation.KS.UserFunction>(), new List<Safe.Compilation.KS.Trigger>());
            Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(codePart.MainCode[0], null, out Safe.Compilation.Optimization.Passes.BlockOrdering.BranchData branchData);
            Assert.AreEqual(1, branchData.ifBlock?.ID);
            Assert.AreEqual(3, branchData.elseBlock?.ID);
            Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(branchData.elseBlock, null, out Safe.Compilation.Optimization.Passes.BlockOrdering.BranchData branchData2);
            Assert.AreEqual(4, branchData2.ifBlock?.ID);
            Assert.AreEqual(6, branchData2.elseBlock?.ID);
            Assert.AreSame(branchData.exit, branchData2.exit);
            Assert.AreEqual(7, branchData.exit?.ID);
            Assert.AreEqual(2, codePart.MainCode.
                Where(b => Safe.Compilation.Optimization.Passes.BlockOrdering.IdentifyBranch(b, null, out _)).Count());
        }
        #endregion

        #region Loop Condition Reordering
        [Test]
        public void TestUntilLoopCondition()
        {
            EnsureAtLeastLevel(OptimizationLevel.Balanced);
            List<CodePart> _code = CompileCodePart("integration/branching/until.ks");
            List<Safe.Compilation.Opcode> opcodes = _code[0].MainCode;
            Assert.AreEqual(opcodes[4].ToString(), opcodes[10].ToString());
            Assert.IsInstanceOf<OpcodeBranchIfTrue>(opcodes[5]);
            Assert.IsInstanceOf<OpcodeBranchIfFalse>(opcodes[11]);
        }
        [Test]
        public void TestForLoopCondition()
        {
            EnsureAtLeastLevel(OptimizationLevel.Balanced);
            List<CodePart> _code = CompileCodePart("integration/branching/for.ks");
            List<Safe.Compilation.Opcode> opcodes = _code[0].MainCode;
            Assert.AreEqual(opcodes[8].ToString(), opcodes[15].ToString());
            Assert.AreEqual(opcodes[9].ToString(), opcodes[16].ToString());
            Assert.IsInstanceOf<OpcodeBranchIfFalse>(opcodes[10]);
            Assert.IsInstanceOf<OpcodeBranchIfTrue>(opcodes[17]);
        }
        [Test]
        public void TestFromLoopCondition()
        {
            EnsureAtLeastLevel(OptimizationLevel.Balanced);
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Add(typeof(Safe.Compilation.Optimization.Passes.ConstantFolding));
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Add(typeof(Safe.Compilation.Optimization.Passes.SCCPWithTypePropagation));
            List<CodePart> _code = CompileCodePart("integration/branching/from.ks");
            List<Safe.Compilation.Opcode> opcodes = _code[0].MainCode;
            Assert.AreEqual(opcodes[7].ToString(), opcodes[19].ToString());
            Assert.AreEqual(opcodes[8].ToString(), opcodes[20].ToString());
            Assert.AreEqual(opcodes[9].ToString(), opcodes[21].ToString());
            Assert.IsInstanceOf<OpcodeBranchIfTrue>(opcodes[10]);
            Assert.IsInstanceOf<OpcodeBranchIfFalse>(opcodes[22]);
        }
        #endregion

        [Test]
        public void TestCommonExpressionElimination()
        {
            // Test that common expressions are eliminated
            EnsureAtLeastLevel(OptimizationLevel.Balanced);
            RunScript("integration/commonExpressionElimination.ks");
            RunSingleStep();
            AssertOutput(
                "1.72624326996796",
                "1.72624326996796",
                "1.72624326996796",
                "1.72624326996796",
                "1.54356862955893",
                "1.54356862955893",
                "1.54356862955893",
                "Outside",
                "1.54356862955893"
            );

            optimizationLevel = OptimizationLevel.None;
            List<CodePart> codePart = CompileCodePart("integration/commonExpressionElimination.ks");
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(codePart[0].MainCode, OptimizationLevel.Balanced, false);

            Assert.IsInstanceOf<OpcodeStoreLocal>(result[18]);
            Assert.IsInstanceOf<OpcodePush>(result[25]);
            Assert.IsInstanceOf<OpcodeCall>(result[26]);
            Assert.IsInstanceOf<OpcodePush>(result[63]);
            Assert.IsInstanceOf<OpcodePush>(result[64]);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[65]);
        }

        [Test]
        public void TestLoopInvariantCodeMotion()
        {
            // Test that loop-invariant expressions are relocated
            EnsureAtLeastLevel(OptimizationLevel.Balanced);
            RunScript("integration/codeMotion.ks");
            RunSingleStep();
            AssertOutput(
                "1.72624326996796",
                "0",
                "1",
                "2",
                "3",
                "2.72624326996796",
                "0",
                "1",
                "2",
                "3",
                "1.72624326996796"
            );
            BasicBlock.ResetNextID();
            optimizationLevel = OptimizationLevel.None;
            List<CodePart> codePart = CompileCodePart("integration/codeMotion.ks");
            List<Safe.Compilation.Opcode> result = GetOpcodesAfterOptimization(codePart[0].MainCode, OptimizationLevel.Balanced, false);
            Assert.IsInstanceOf<OpcodePush>(result[21]);
            Assert.AreEqual(Encapsulation.ScalarIntValue.Two, ((OpcodePush)result[21]).Argument);
            Assert.IsInstanceOf<OpcodePush>(result[22]);
            Assert.AreEqual("$a", ((OpcodePush)result[22]).Argument);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[23]);
            Assert.IsInstanceOf<OpcodeStoreExist>(result[24]);
            Assert.AreEqual("$b", ((OpcodeStoreExist)result[24]).Identifier);
            Assert.IsInstanceOf<OpcodePush>(result[52]);
            Assert.AreEqual(Encapsulation.ScalarIntValue.One, ((OpcodePush)result[52]).Argument);
            Assert.IsInstanceOf<OpcodePush>(result[53]);
            Assert.AreEqual("$a", ((OpcodePush)result[53]).Argument);
            Assert.IsInstanceOf<OpcodeMathAdd>(result[54]);
            Assert.IsInstanceOf<OpcodeStoreExist>(result[55]);
            Assert.AreEqual("$b", ((OpcodeStoreExist)result[55]).Identifier);
        }

        [Test]
        public void TestFunctionInlining()
        {
            EnsureAtLeastLevel(OptimizationLevel.Aggressive);
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
        public void TestFunctionConstantPropagation()
        {
            Safe.Compilation.Optimization.Optimizer.PassesToSkip.Add(typeof(Safe.Compilation.Optimization.Passes.FunctionInlining));
            EnsureAtLeastLevel(OptimizationLevel.Aggressive);

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
        public void TestForLoopUnrolling()
        {
            EnsureAtLeastLevel(OptimizationLevel.Extreme);
            List<CodePart> _code = CompileCodePart("integration/branching/for_range.ks");
            List<Safe.Compilation.Opcode> opcodes = _code[0].MainCode;
            Assert.IsInstanceOf<OpcodePush>(opcodes[17]);
            Assert.AreEqual(new Encapsulation.ScalarIntValue(3), (opcodes[17] as OpcodePush)?.Argument);

            RunScript("integration/branching/for_range.ks");
            RunSingleStep();
            AssertOutput(
                "beginning",
                "0",
                "1",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7",
                "8",
                "9",
                "trunk"
            );
        }
        [Test]
        public void TestFromLoopUnrolling()
        {
            EnsureAtLeastLevel(OptimizationLevel.Extreme);
            List<CodePart> _code = CompileCodePart("integration/branching/from.ks");
            List<Safe.Compilation.Opcode> opcodes = _code[0].MainCode;
            Assert.IsInstanceOf<OpcodePush>(opcodes[17]);
            Assert.AreEqual(new Encapsulation.ScalarIntValue(3), (opcodes[17] as OpcodePush)?.Argument);

            RunScript("integration/branching/from.ks");
            RunSingleStep();
            AssertOutput(
                "beginning",
                "0",
                "1",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7",
                "8",
                "9",
                "trunk"
            );
        }
        [Test]
        public void TestFromLoopBranchingUnrolling()
        {
            EnsureAtLeastLevel(OptimizationLevel.Extreme);
            List<CodePart> _code = CompileCodePart("integration/branching/fromIntermittent.ks");
            List<Safe.Compilation.Opcode> opcodes = _code[0].MainCode;
            Assert.IsInstanceOf<OpcodePush>(opcodes[17]);
            Assert.AreEqual(new Encapsulation.ScalarIntValue(5), (opcodes[17] as OpcodePush)?.Argument);

            RunScript("integration/branching/fromIntermittent.ks");
            RunSingleStep();
            AssertOutput(
                "beginning",
                "0",
                "1",
                "3",
                "5",
                "7",
                "9",
                "trunk"
            );
        }
    }
}
