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
            AddSuffix("NEXT", new NoArgsSuffix<bool>(() =>
            {
                status = enumerator.MoveNext();
                index++;
                return status;
            }));
            AddSuffix("ATEND", new NoArgsSuffix<bool>(() => !status));
            AddSuffix("INDEX", new NoArgsSuffix<int>(() => index));
            AddSuffix("VALUE", new NoArgsSuffix<object>(() => enumerator.Current));
        }

        public override string ToString()
        {
            return string.Format("{0} Iterator", base.ToString());
        }
    }
}