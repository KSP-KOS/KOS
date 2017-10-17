using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Compilation.KS;
using NUnit.Framework;

namespace kOS.Safe.Test.KS
{
    [TestFixture]
    public class ParserTest
    {
        Scanner scanner;
        Parser parser;

        [SetUp]
        public void Setup()
        {
          scanner = new Scanner();
          parser = new Parser(scanner);
        }

        [Test, Timeout(2000)]
        public void ParsesLargeNumbers()
        {
          parser.Parse("012345678901234567890123456789");
          parser.Parse("// 012345678901234567890123456789");
        }
    }
}
