using System;
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
            list.Add("bar");
            cpu.PushStack(list);

            const int INDEX = 0;
            cpu.PushStack(INDEX);

            const string VALUE = "foo";
            cpu.PushStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, list.Count);
            Assert.AreNotEqual("bar", list[0]);
            Assert.AreEqual("foo", list[0]);
        }

        [Test]
        public void CanSetListIndexWithFloat()
        {
            var list = new ListValue();
            list.Add("bar");
            cpu.PushStack(list);

            const float INDEX = 0.0f;
            cpu.PushStack(INDEX);

            const string VALUE = "foo";
            cpu.PushStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, list.Count);
            Assert.AreNotEqual("bar", list[0]);
            Assert.AreEqual("foo", list[0]);
        }

        [Test]
        public void CanSetListIndexWithDouble()
        {
            var list = new ListValue();
            list.Add("bar");
            cpu.PushStack(list);

            const double INDEX = 0.0d;
            cpu.PushStack(INDEX);

            const string VALUE = "foo";
            cpu.PushStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, list.Count);
            Assert.AreNotEqual("bar", list[0]);
            Assert.AreEqual("foo", list[0]);
        }

        [Test]
        [ExpectedException(typeof(KOSException))]
        public void WillThrowOnNonIntListIndex()
        {
            var list = new ListValue();
            list.Add("bar");
            cpu.PushStack(list);

            const string INDEX = "fizz";
            cpu.PushStack(INDEX);

            const string VALUE = "foo";
            cpu.PushStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);
        }

        [Test]
        public void CanSetLexiconIndex()
        {
            const string INDEX = "foo";

            var lex = new Lexicon<object,object>();
            lex.Add(INDEX, "bar");
            cpu.PushStack(lex);

            cpu.PushStack(INDEX);

            const string VALUE = "fizz";
            cpu.PushStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, lex.Count);
            Assert.AreNotEqual("bar", lex["foo"]);
        }

        [Test]
        [ExpectedException(typeof(KOSException))]
        public void WillThrowOnNonListType()
        {
            const string INDEX = "foo";

            var lex = new object();
            cpu.PushStack(lex);

            cpu.PushStack(INDEX);

            const string VALUE = "fizz";
            cpu.PushStack(VALUE);
            
            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);
        }
    }
}
