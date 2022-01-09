using System.Collections;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Iterator")]
    [kOS.Safe.Utilities.KOSNomenclature("Enumerator", CSharpToKOS = false)] // one-way mapping makes it just another alias, not canonical.
    public class Enumerator : Structure
    {
        private readonly IEnumerator enumerator;
        private int index = -1;
        private bool status;

        public Enumerator(IEnumerator enumerator)
        {
            this.enumerator = enumerator;
            EnumeratorInitializeSuffixes();
        }

        private void EnumeratorInitializeSuffixes()
        {
            AddSuffix("NEXT", new NoArgsSuffix<BooleanValue>(() =>
            {
                status = enumerator.MoveNext();
                index++;
                return status;
            }));
            AddSuffix("ATEND", new NoArgsSuffix<BooleanValue>(() => !status));
            AddSuffix("INDEX", new NoArgsSuffix<ScalarValue>(() => index));
            AddSuffix("VALUE", new NoArgsSuffix<Structure>(() => FromPrimitiveWithAssert(enumerator.Current)));
        }
    }
}