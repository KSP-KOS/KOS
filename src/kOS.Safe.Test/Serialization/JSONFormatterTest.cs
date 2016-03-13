using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using NUnit.Framework;
using kOS.Safe.Utilities;

namespace kOS.Safe.Test.Serialization
{
    [TestFixture]
    public class JSONFormatterTest : FormatterTest
    {
        protected override IFormatReader FormatReader {
            get {
                return JsonFormatter.ReaderInstance;
            }
        }

        protected override IFormatWriter FormatWriter {
            get {
                return JsonFormatter.WriterInstance;
            }
        }

    }
}
