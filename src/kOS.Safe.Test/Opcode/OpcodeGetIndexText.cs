using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using NUnit.Framework;

namespace kOS.Safe.Test.Opcode
{
    [TestFixture]
    public class OpcodeGetIndexText
    {
        private ICpu cpu;

        [SetUp]
        public void Setup()
        {
            cpu = new FakeCpu();
        }

        [Test]
        public void CanGetListIndex()
        {
            var list = new ListValue();
            list.Add(new StringValue("bar"));
            cpu.PushStack(list);

            const int INDEX = 0;
            cpu.PushStack(INDEX);

            var opcode = new OpcodeGetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, list.Count());
            Assert.AreEqual("bar", cpu.PopStack());
        }

        [Test]
        public void CanGetCorrectListIndex()
        {
            var list = new ListValue();
            list.Add(new StringValue("bar"));
            list.Add(new StringValue("foo"));
            list.Add(new StringValue("fizz"));
            cpu.PushStack(list);

            const int INDEX = 1;
            cpu.PushStack(INDEX);

            var opcode = new OpcodeGetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(3, list.Count());
            Assert.AreEqual("foo", cpu.PopStack());
        }

        [Test]
        public void CanGetDoubleIndex()
        {
            var list = new ListValue();
            list.Add(new StringValue("bar"));
            list.Add(new StringValue("foo"));
            list.Add(new StringValue("fizz"));
            cpu.PushStack(list);

            const double INDEX = 2.5;
            cpu.PushStack(INDEX);

            var opcode = new OpcodeGetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(3, list.Count());
            Assert.AreEqual("fizz", cpu.PopStack());
        }

        [Test]
        public void CanGetLexiconIndex()
        {
            var list = new Lexicon();
            list.Add(new StringValue("foo"), new StringValue("bar"));
            cpu.PushStack(list);

            const string INDEX = "foo";
            cpu.PushStack(INDEX);

            var opcode = new OpcodeGetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("bar", cpu.PopStack());
        }

        [Test]
        public void CanGetCorrectLexiconIndex()
        {
            var list = new Lexicon();
            list.Add(new StringValue("foo"), new StringValue("bar"));
            list.Add(new StringValue("fizz"), new StringValue("bang"));
            cpu.PushStack(list);

            const string INDEX = "fizz";
            cpu.PushStack(INDEX);

            var opcode = new OpcodeGetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("bang", cpu.PopStack());
        }
    }
}
