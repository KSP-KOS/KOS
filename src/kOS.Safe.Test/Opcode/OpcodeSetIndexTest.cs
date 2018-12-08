using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using NUnit.Framework;

namespace kOS.Safe.Test.Opcode
{
    [TestFixture]
    public class OpcodeSetIndexTest
    {
        private ICpu cpu;

        [SetUp]
        public void Setup()
        {
            cpu = new FakeCpu();
        }

        [Test]
        public void CanSetListIndex()
        {
            var list = new ListValue();
            list.Add(new StringValue("bar"));
            cpu.PushArgumentStack(list);

            const int INDEX = 0;
            cpu.PushArgumentStack(INDEX);

            const string VALUE = "foo";
            cpu.PushArgumentStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, list.Count);
            Assert.AreNotEqual(new StringValue("bar"), list[0]);
            Assert.AreEqual(new StringValue("foo"), list[0]);
        }

        [Test]
        public void CanSetListIndexWithFloat()
        {
            var list = new ListValue();
            list.Add(new StringValue("bar"));
            cpu.PushArgumentStack(list);

            const float INDEX = 0.0f;
            cpu.PushArgumentStack(INDEX);

            const string VALUE = "foo";
            cpu.PushArgumentStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, list.Count);
            Assert.AreNotEqual(new StringValue("bar"), list[0]);
            Assert.AreEqual(new StringValue("foo"), list[0]);
        }

        [Test]
        public void CanSetListIndexWithDouble()
        {
            var list = new ListValue();
            list.Add(new StringValue("bar"));
            cpu.PushArgumentStack(list);

            const double INDEX = 0.0d;
            cpu.PushArgumentStack(INDEX);

            const string VALUE = "foo";
            cpu.PushArgumentStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, list.Count);
            Assert.AreNotEqual(new StringValue("bar"), list[0]);
            Assert.AreEqual(new StringValue("foo"), list[0]);
        }

        [Test]
        [ExpectedException(typeof(KOSException))]
        public void WillThrowOnNonIntListIndex()
        {
            var list = new ListValue();
            list.Add(new StringValue("bar"));
            cpu.PushArgumentStack(list);

            const string INDEX = "fizz";
            cpu.PushArgumentStack(INDEX);

            const string VALUE = "foo";
            cpu.PushArgumentStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);
        }

        [Test]
        public void CanSetLexiconIndex()
        {
            Encapsulation.Structure index = new StringValue("foo");

            var lex = new Lexicon();
            lex.Add(index, new StringValue("bar"));
            cpu.PushArgumentStack(lex);

            cpu.PushArgumentStack(index);

            const string VALUE = "fizz";
            cpu.PushArgumentStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, lex.Count);
            Assert.AreNotEqual("bar", lex[new StringValue("foo")]);
        }

        [Test]
        [ExpectedException(typeof(KOSException))]
        public void WillThrowOnNonListType()
        {
            const string INDEX = "foo";

            var lex = new object();
            cpu.PushArgumentStack(lex);

            cpu.PushArgumentStack(INDEX);

            const string VALUE = "fizz";
            cpu.PushArgumentStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);
        }
    }
}
