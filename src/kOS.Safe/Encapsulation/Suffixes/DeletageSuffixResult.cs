using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class DeletageSuffixResult : ISuffixResult
    {
        private Delegate del;

        public DeletageSuffixResult(Delegate del)
        {
            this.del = del;
        }

        public bool HasValue
        {
            get { return false; }
        }

        public Structure Value()
        {
            //TODO:ERENDRAKE Make custom error
            throw new NullReferenceException();
        }
    }
}