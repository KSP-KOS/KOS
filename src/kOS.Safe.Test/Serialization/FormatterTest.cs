using System;
using kOS.Safe.Encapsulation;
using NUnit.Framework;
using kOS.Safe.Serialization;

namespace kOS.Safe.Test.Serialization
{
    public abstract class FormatterTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CanSerializeLexicons()
        {
            var lex = new Lexicon();
            var nested = new Lexicon();

            lex[new StringValue("key1")] = new StringValue("value1");
            lex[new StringValue("key2")] = new ScalarIntValue(1);
            lex[new StringValue("key3")] = nested;

            nested[new StringValue("nested1")] = new StringValue("nested1value");
            nested[new StringValue("nested2")] = new StringValue("nested2value");

            Lexicon deserialized = Deserialize(Serialize(lex)) as Lexicon;

            Assert.AreEqual(new StringValue("value1"), deserialized[new StringValue("key1")]);
            Assert.AreEqual(new ScalarIntValue(1), deserialized[new StringValue("key2")]);
            Assert.IsTrue(deserialized[new StringValue("key3")] is Lexicon);
            Assert.AreEqual(new StringValue("nested1value"), (deserialized[new StringValue("key3")] as Lexicon)[new StringValue("nested1")]);
        }

        [Test]
        public void CanSerializeLists()
        {
            var list = new ListValue();
            var nested = new ListValue();

            list.Add(new StringValue("item1"));
            list.Add(new ScalarIntValue(2));
            list.Add(nested);

            nested.Add(new StringValue("nested1"));

            string serialized = Serialize(list);

            ListValue deserialized = Deserialize(serialized) as ListValue;

            Assert.AreEqual(new StringValue("item1"), deserialized[0]);
            Assert.AreEqual(new ScalarIntValue(2), deserialized[1]);
            Assert.IsTrue(deserialized[2] is ListValue);
        }

        [Test]
        public void CanSerializeStacks()
        {
            var stack = new StackValue();
            var nested = new StackValue();

            stack.Push(new StringValue("item1"));
            stack.Push(new ScalarIntValue(2));
            stack.Push(nested);

            nested.Push(new StringValue("nested1"));

            StackValue deserialized = Deserialize(Serialize(stack)) as StackValue;

            Assert.AreEqual(new StringValue("nested1"), (deserialized.Pop() as StackValue).Pop());
            Assert.AreEqual(new ScalarIntValue(2), deserialized.Pop());
            Assert.AreEqual(new StringValue("item1"), deserialized.Pop());

        }

        [Test]
        public void CanSerializeQueues()
        {
            var queue = new QueueValue();
            var nested = new QueueValue();

            queue.Push(new StringValue("item1"));
            queue.Push(new ScalarIntValue(2));
            queue.Push(nested);

            nested.Push(new StringValue("nested1"));

            QueueValue deserialized = Deserialize(Serialize(queue)) as QueueValue;

            Assert.AreEqual(new StringValue("item1"), deserialized.Pop());
            Assert.AreEqual(new ScalarIntValue(2), deserialized.Pop());
            Assert.IsTrue(deserialized.Pop() is QueueValue);
        }

        private string Serialize(IDumper o)
        {
            return "";// return new SafeSerializationMgr(null).Serialize(o, FormatWriter);
        }

        private IDumper Deserialize(string s)
        {
            return null;// return new SafeSerializationMgr(null).Deserialize(s, FormatReader);
        }
    }
}

