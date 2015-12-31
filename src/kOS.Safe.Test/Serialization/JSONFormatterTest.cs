using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using NUnit.Framework;

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

            list.Add("item1");
            list.Add(2);
            list.Add(nested);

            nested.Add("nested1");

            ListValue deserialized = Deserialize(Serialize(list)) as ListValue;

            Assert.AreEqual(new StringValue("item1"), deserialized[0]);
            Assert.AreEqual(new ScalarIntValue(2), deserialized[1]);
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

            Assert.AreEqual(new StringValue("nested1"), (deserialized.Pop() as StackValue).Pop());
            Assert.AreEqual(new ScalarIntValue(2), deserialized.Pop());
            Assert.AreEqual(new StringValue("item1"), deserialized.Pop());

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

            Assert.AreEqual(new StringValue("item1"), deserialized.Pop());
            Assert.AreEqual(new ScalarIntValue(2), deserialized.Pop());
            Assert.IsTrue(deserialized.Pop() is QueueValue);
        }

        private string Serialize(IDumper o)
        {
            return new SafeSerializationMgr().Serialize(o, JsonFormatter.WriterInstance);
        }

        private object Deserialize(string s)
        {
            return new SafeSerializationMgr().Deserialize(s, JsonFormatter.ReaderInstance);
        }
    }
}
