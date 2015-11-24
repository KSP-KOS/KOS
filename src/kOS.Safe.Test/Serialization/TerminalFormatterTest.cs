using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
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

            lex["key1"] = "value1";
            lex["key2"] = 1;
            lex["key3"] = nested;

            nested["nested1"] = "nested1value";
            nested["nested2"] = "nested2value";

            Assert.AreEqual("LEXICON of 3 items:\n[\"key1\"]= value1\n[\"key2\"]= 1\n[\"key3\"]=" +
                " LEXICON of 2 items:\n  [\"nested1\"]= nested1value\n  [\"nested2\"]= nested2value", Serialize(lex));
        }

        private string Serialize(IDumper o)
        {
            return SerializationMgr.Instance.Serialize(o, TerminalFormatter.Instance, false);
        }
    }
}
