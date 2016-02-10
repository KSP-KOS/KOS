using System;
using kOS.Safe.Encapsulation;
using NUnit.Framework;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class MixedCollectionPrintingTest : CollectionValueTest
    {

        [Test]
        public void CanPrintListInLexicon()
        {
            var list = new ListValue
            {
                new StringValue("First In List"), 
                new StringValue("Second In List"), 
                new StringValue("Last In List")
            };

            var lexicon = new Lexicon
            {
                {new StringValue("list"), list}, 
                {new StringValue("not list"), new ScalarIntValue(2)}
            };

            var result = (StringValue)InvokeDelegate(lexicon, "DUMP");

            Assert.IsTrue(result.Contains("LEXICON of 2 items"));
            Assert.IsTrue(result.Contains("[\"list\"] = LIST of 3 items"));
            Assert.IsTrue(result.Contains("Last In List"));
        }

        [Test]
        public void DoesNotContainInvalidToString()
        {
            var list = new ListValue
            {
                new StringValue("First In List"), 
                new StringValue("Second In List"), 
                new StringValue("Last In List")
            };

            var lexicon = new Lexicon
            {
                {new StringValue("list"), list}, 
                {new StringValue("not list"), new ScalarIntValue(2)}
            };

            var result = (StringValue)InvokeDelegate(lexicon, "DUMP");

            Assert.IsFalse(result.Contains("System"));
            Assert.IsFalse(result.Contains("string[]"));
        }
    }
}
