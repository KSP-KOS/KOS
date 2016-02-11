using System;
using kOS.Safe.Encapsulation;
using NUnit.Framework;
using kOS.Safe.Serialization;

namespace kOS.Safe.Test.Serialization
{
    [TestFixture]
    public class TerminalFormatterTest
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
            lex[new StringValue("2")]    = new ScalarIntValue(10);
            lex[new ScalarIntValue(2)]   = new ScalarIntValue(11); // make sure int 2 is different than string "2"
            lex[new StringValue("key3")] = nested;

            nested[new StringValue("nested1")] = new StringValue("nested1value");
            nested[new StringValue("nested2")] = new StringValue("nested2value");

            var lines = new string[] { "LEXICON of 4 items:", "[\"key1\"] = \"value1\"", "[\"2\"] = 10", "[2] = 11", "[\"key3\"] = LEXICON of 2 items:",
                "  [\"nested1\"] = \"nested1value\"", "  [\"nested2\"] = \"nested2value\""};

            Assert.AreEqual(string.Join(Environment.NewLine, lines), Serialize(lex));
        }

        private string Serialize(SerializableStructure o)
        {
            return new SafeSerializationMgr().Serialize(o, TerminalFormatter.Instance, false);
        }
    }
}
