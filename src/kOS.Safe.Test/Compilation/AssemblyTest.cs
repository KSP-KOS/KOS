using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Compilation;
using kOS.Safe.Compilation.KASM;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Test.Compilation
{
    [TestFixture]
    class AssemblyTest
    {
        [TestFixtureSetUp]
        public void Setup()
        {
            kOS.Safe.Compilation.Opcode.InitMachineCodeData();
            CompiledObject.InitTypeData();
            Safe.Utilities.SafeHouse.Logger = new TestLogger();
        }

        [Test]
        public void EmptyProgram()
        {
            var program = new List<kOS.Safe.Compilation.CodePart>();
            var assembly = AssemblyObject.Disassemble(program);
            var result = AssemblyObject.Assemble(assembly);

            Assert.IsTrue(result.SequenceEqual(program));
        }

        [Test]
        public void PushPop()
        {
            var code = new CodePart();

            code.MainCode = new List<Safe.Compilation.Opcode>();
            code.MainCode.Add(new OpcodePush(new StringValue("test")));
            code.MainCode.Add(new OpcodePop());
            code.MainCode[0].Label = "start";

            var program = new List<kOS.Safe.Compilation.CodePart>();
            program.Add(code);

            byte[] original = CompiledObject.Pack(program);

            var assembly = AssemblyObject.Disassemble(program);
            var result = AssemblyObject.Assemble(assembly).ToList();

            byte[] comparison = CompiledObject.Pack(result);
            Assert.IsTrue(original.SequenceEqual(comparison));
        }

        [Test]
        public void TypesTest()
        {
            var code = new CodePart();
            code.MainCode = new List<Safe.Compilation.Opcode>();

            code.MainCode.Add(new OpcodePush(new StringValue("test")));
            code.MainCode.Add(new OpcodePush("\n\t\t\t\t\n\n\ntest\\ \"\" escaped"));
            code.MainCode.Add(new OpcodePush(new ScalarDoubleValue(-0.000000001)));
            code.MainCode.Add(new OpcodePush(new ScalarIntValue(-1233413123)));
            code.MainCode.Add(new OpcodePush(12341d / 125314d));
            code.MainCode.Add(new OpcodePush('ﾟ'));

            var program = new List<kOS.Safe.Compilation.CodePart>();
            program.Add(code);

            var assembly = AssemblyObject.Disassemble(program);
            var reassembled = AssemblyObject.Assemble(assembly).ToList();

            var original = code.MainCode.Cast<OpcodePush>().Select((o) => o.ToString());
            var converted = reassembled.First().MainCode.Cast<OpcodePush>().Select((o) => o.ToString());

            Assert.IsTrue(original.SequenceEqual(converted));
        }
    }
}
