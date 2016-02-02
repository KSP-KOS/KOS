using System.Collections;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
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
            AddSuffix("RESET", new NoArgsSuffix(() =>
            {
                index = -1;
                status = false;
                enumerator.Reset();
            }));
            AddSuffix("NEXT", new NoArgsSuffix<BooleanValue>(() =>
            {
                status = enumerator.MoveNext();
                index++;
                return status;
            }));
            AddSuffix("ATEND", new NoArgsSuffix<BooleanValue>(() => !status));
            AddSuffix("INDEX", new NoArgsSuffix<ScalarIntValue>(() => index));
            AddSuffix("VALUE", new NoArgsSuffix<Structure>(() => FromPrimitiveWithAssert(enumerator.Current)));
        }

        public override string ToString()
        {
            return string.Format("{0} Iterator", base.ToString());
        }
    }
}