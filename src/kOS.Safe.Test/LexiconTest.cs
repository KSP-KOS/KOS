using System;
using kOS.Safe.Encapsulation;
using NUnit.Framework;

namespace kOS.Safe.Test
{
    [TestFixture]
    public class LexiconTest
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void CanConstruct()
        {
            var lex = new Lexicon<object, object>();
            
            lex.Add("foo", "bar");

            Assert.AreEqual("bar", lex["foo"]);
        }

        [Test]
        public void IsCaseInsensitive()
        {
            var lex = new Lexicon<object, object>();
            
            lex.Add("foo", "bar");
            lex.Add("FOO", "BAR");

            Assert.AreEqual("bar", lex["foo"]);
        }


    }
}
