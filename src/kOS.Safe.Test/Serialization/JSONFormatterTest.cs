using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using NUnit.Framework;
using kOS.Safe.Serialization;
using System.Collections.Generic;

namespace kOS.Safe.Test.Serialization
{
    [TestFixture]
    public class JSONFormatterTest
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

            lex["key1"] = "value1";
            lex["key2"] = 1;
            lex["key3"] = nested;

            nested["nested1"] = "nested1value";
            nested["nested2"] = "nested2value";

            Lexicon deserialized = Deserialize(Serialize(lex)) as Lexicon;

            Assert.AreEqual("value1", deserialized["key1"]);
            Assert.AreEqual(1, deserialized["key2"]);
            Assert.IsTrue(deserialized["key3"] is Lexicon);
            Assert.AreEqual("nested1value", (deserialized["key3"] as Lexicon)["nested1"]);
        }

        [Test]
        public void CanSerializeLists()
        {
            var list = new ListValue();
            var nested = new ListValue();

            list.Add("item1");
            list.Add(2);
            list.Add(nested);

            nested.Add("nested1");

            ListValue deserialized = Deserialize(Serialize(list)) as ListValue;

            Assert.AreEqual("item1", deserialized[0]);
            Assert.AreEqual(2, deserialized[1]);
            Assert.IsTrue(deserialized[2] is ListValue);
        }

        [Test]
        public void CanSerializeStacks()
        {
            var stack = new StackValue();
            var nested = new StackValue();

            stack.Push("item1");
            stack.Push(2);
            stack.Push(nested);

            nested.Push("nested1");

            StackValue deserialized = Deserialize(Serialize(stack)) as StackValue;

            Assert.AreEqual("nested1", (deserialized.Pop() as StackValue).Pop());
            Assert.AreEqual(2, deserialized.Pop());
            Assert.AreEqual("item1", deserialized.Pop());

        }

        [Test]
        public void CanSerializeQueues()
        {
            var queue = new QueueValue();
            var nested = new QueueValue();

            queue.Push("item1");
            queue.Push(2);
            queue.Push(nested);

            nested.Push("nested1");

            QueueValue deserialized = Deserialize(Serialize(queue)) as QueueValue;

            Assert.AreEqual("item1", deserialized.Pop());
            Assert.AreEqual(2, deserialized.Pop());
            Assert.IsTrue(deserialized.Pop() is QueueValue);
        }

        private string Serialize(IDumper o)
        {
            return new SafeSerializationMgr().Serialize(o, JSONFormatter.Instance);
        }

        private object Deserialize(string s)
        {
            return new SafeSerializationMgr().Deserialize(s, JSONFormatter.Instance);
        }
    }
}
