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

            var lines = new string[] { "LEXICON of 3 items:", "[\"key1\"]= value1", "[\"key2\"]= 1", "[\"key3\"]= LEXICON of 2 items:",
                "  [\"nested1\"]= nested1value", "  [\"nested2\"]= nested2value"};

            Assert.AreEqual(String.Join(Environment.NewLine, lines), Serialize(lex));
        }

        private string Serialize(IDumper o)
        {
            return new SafeSerializationMgr().Serialize(o, TerminalFormatter.Instance, false);
        }
    }
}
