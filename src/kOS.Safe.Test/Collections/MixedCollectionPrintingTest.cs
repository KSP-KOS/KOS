using System;
using kOS.Safe.Encapsulation;
using NUnit.Framework;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class MixedCollectionPrintingTest
    {
        [Test]
        public void CanShallowPrintListInLexicon()
        {
            var list = new ListValue();
            list.Add("First In List");
            list.Add("Second In List");
            list.Add("Last In List");

            var lexicon = new Lexicon<object, object>();
            lexicon.Add("list", list);
            lexicon.Add("not list", 2);

            var result = lexicon.ToString();

            Assert.IsTrue(result.Contains("LEXICON of 2 items"));
            Assert.IsTrue(result.Contains("  [\"list\"]= LIST of 3 items"));
            Assert.IsFalse(result.Contains("Last In List"));
        }

        [Test]
        public void CanDeepPrintListInLexicon()
        {
            var list = new ListValue
            {
                "First In List", 
                "Second In List", 
                "Last In List"
            };

            var lexicon = new Lexicon<object, object>
            {
                {"list", list}, 
                {"not list", 2}
            };

            var result = (string)InvokeDelegate(lexicon, "DUMP");

            Assert.IsTrue(result.Contains("LEXICON of 2 items"));
            Assert.IsTrue(result.Contains("  [\"list\"]= LIST of 3 items"));
            Assert.IsTrue(result.Contains("Last In List"));
        }

        [Test]
        public void DoesNotContainInvalidToString()
        {
            var list = new ListValue
            {
                "First In List", 
                "Second In List", 
                "Last In List"
            };

            var lexicon = new Lexicon<object, object>
            {
                {"list", list}, 
                {"not list", 2}
            };

            var result = (string)InvokeDelegate(lexicon, "DUMP");

            Assert.IsFalse(result.Contains("System"));
            Assert.IsFalse(result.Contains("string[]"));
        }

        private object InvokeDelegate(IDumper list, string suffixName, params object[] parameters)
        {
            var lengthObj = list.GetSuffix(suffixName);
            Assert.IsNotNull(lengthObj);
            var lengthDelegate = lengthObj as Delegate;
            Assert.IsNotNull(lengthDelegate);
            var length = lengthDelegate.DynamicInvoke(parameters);
            return length;
        }
    }
}
