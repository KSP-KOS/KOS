using System;
using NUnit.Framework;
using kOS.Safe.Communication;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using kOS.Safe.Utilities;

namespace kOS.Safe.Test.Communication
{
    [TestFixture]
    public class MessageQueueTest
    {
        private GenericMessageQueue<BaseMessage> queue;
        private FakeCurrentTimeProvider timeProvider;

        [SetUp]
        public void Setup()
        {
            timeProvider = new FakeCurrentTimeProvider();
            timeProvider.FakeTime = 0;

            queue = new GenericMessageQueue<BaseMessage>(timeProvider);
        }

        [Test]
        public void CanHandleNoMessages()
        {
            Assert.AreEqual(0, queue.Count());
            Assert.AreEqual(0, queue.ReceivedCount());
            Assert.Throws<KOSException>(delegate { queue.Pop(); });
            Assert.Throws<KOSException>(delegate { queue.Peek(); });
        }

        [Test]
        public void CanHandleMultipleMessagesInOrder()
        {
            queue.Push(new BaseMessage(new StringValue("content1"), 1, 10));
            queue.Push(new BaseMessage(new StringValue("content2"), 2, 10));
            queue.Push(new BaseMessage(new StringValue("content3"), 3, 11));
            queue.Push(new BaseMessage(new StringValue("content4"), 4, 12));
            queue.Push(new BaseMessage(new StringValue("content5"), 5, 13));

            timeProvider.FakeTime = 5;

            // time now is 5, no messages should be received

            Assert.AreEqual(5, queue.Count());
            Assert.AreEqual(0, queue.ReceivedCount());
            Assert.Throws<KOSException>(delegate { queue.Pop(); });
            Assert.Throws<KOSException>(delegate { queue.Peek(); });

            timeProvider.FakeTime = 10;

            // time now is 10, there should be 2 messages
            Assert.AreEqual(5, queue.Count());
            Assert.AreEqual(2, queue.ReceivedCount());
            BaseMessage received = queue.Pop();

            Assert.AreEqual(new StringValue("content1"), received.Content);
            Assert.AreEqual(10, received.ReceivedAt);
            Assert.AreEqual(1, received.SentAt);

            timeProvider.FakeTime = 13;

            // time now is 13, there should be 4 messages
            Assert.AreEqual(4, queue.Count());
            Assert.AreEqual(4, queue.ReceivedCount());
            Assert.AreEqual(new StringValue("content2"), queue.Pop().Content);
            Assert.AreEqual(new StringValue("content3"), queue.Pop().Content);
            Assert.AreEqual(new StringValue("content4"), queue.Pop().Content);
            Assert.AreEqual(new StringValue("content5"), queue.Pop().Content);
        }

        [Test]
        public void CanHandleMultipleRandomMessages()
        {
            queue.Push(new BaseMessage(new StringValue("content1"), 1, 13));
            queue.Push(new BaseMessage(new StringValue("content2"), 2, 3));

            timeProvider.FakeTime = 3;

            Assert.AreEqual(1, queue.ReceivedCount());
            Assert.AreEqual(new StringValue("content2"), queue.Pop().Content);

            queue.Push(new BaseMessage(new StringValue("content3"), 3, 9));
            queue.Push(new BaseMessage(new StringValue("content4"), 3, 14));
            queue.Push(new BaseMessage(new StringValue("content5"), 3, 5));

            timeProvider.FakeTime = 9;

            Assert.AreEqual(2, queue.ReceivedCount());
            Assert.AreEqual(new StringValue("content5"), queue.Pop().Content);
            Assert.AreEqual(new StringValue("content3"), queue.Pop().Content);

            timeProvider.FakeTime = 14;

            Assert.AreEqual(2, queue.ReceivedCount());
            Assert.AreEqual(new StringValue("content1"), queue.Pop().Content);
            Assert.AreEqual(new StringValue("content4"), queue.Pop().Content);

        }

        [Test]
        public void CanClear()
        {
            queue.Push(new BaseMessage(new StringValue("content1"), 1, 10));
            queue.Push(new BaseMessage(new StringValue("content2"), 2, 10));
            queue.Push(new BaseMessage(new StringValue("content3"), 3, 11));
            queue.Push(new BaseMessage(new StringValue("content4"), 4, 12));
            queue.Push(new BaseMessage(new StringValue("content5"), 5, 13));

            // this should remove nothing
            queue.Clear();

            Assert.AreEqual(5, queue.Count());
            Assert.AreEqual(0, queue.ReceivedCount());


            timeProvider.FakeTime = 10;

            // this should remove two messages
            queue.Clear();

            Assert.AreEqual(3, queue.Count());
            Assert.AreEqual(0, queue.ReceivedCount());

            timeProvider.FakeTime = 13;
            Assert.AreEqual(3, queue.Count());
            Assert.AreEqual(3, queue.ReceivedCount());

            // this should remove the rest
            queue.Clear();

            Assert.AreEqual(0, queue.ReceivedCount());
            Assert.AreEqual(0, queue.Count());
        }

        [Test]
        public void CanHandleSerializableStructures()
        {
            Lexicon lex = new Lexicon();
            lex.Add(new StringValue("key1"), new StringValue("value1"));
            queue.Push(new BaseMessage(new SafeSerializationMgr().Dump(lex), 0, 0));

            Lexicon read = new SafeSerializationMgr().CreateFromDump(queue.Pop().Content as Dump) as Lexicon;
            Assert.AreEqual(new StringValue("value1"), read[new StringValue("key1")]);
        }

        [Test]
        public void CanDump()
        {
            queue.Push(new BaseMessage(new StringValue("content1"), 1, 10));
            queue.Push(new BaseMessage(new StringValue("content2"), 2, 10));
            queue.Push(new BaseMessage(new StringValue("content3"), 3, 11));
            queue.Push(new BaseMessage(new StringValue("content4"), 4, 12));
            queue.Push(new BaseMessage(new StringValue("content5"), 5, 13));

            GenericMessageQueue<BaseMessage> newQueue = new GenericMessageQueue<BaseMessage>(timeProvider);

            newQueue.LoadDump(queue.Dump());

            timeProvider.FakeTime = 11;

            // time now is 13, there should be 4 messages
            Assert.AreEqual(5, newQueue.Count());
            Assert.AreEqual(3, newQueue.ReceivedCount());
            BaseMessage received = newQueue.Pop();
            Assert.AreEqual(1, received.SentAt);
            Assert.AreEqual(10, received.ReceivedAt);
            Assert.AreEqual(new StringValue("content1"), received.Content);
            Assert.AreEqual(new StringValue("content2"), newQueue.Pop().Content);
            Assert.AreEqual(new StringValue("content3"), newQueue.Pop().Content);
        }
    }
}
