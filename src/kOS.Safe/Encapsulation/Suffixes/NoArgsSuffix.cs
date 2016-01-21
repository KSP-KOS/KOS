using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class NoArgsSuffix<TReturn> : SuffixBase where TReturn : Structure
    {
        private readonly Del<TReturn> del;

        public delegate TInnerReturn Del<out TInnerReturn>() where TInnerReturn : Structure;

        public NoArgsSuffix(Del<TReturn> del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override ISuffixResult Get()
        {
            return new DeletageSuffixResult<TReturn>(del);
        }
    }

    public class DeletageSuffixResult<TReturn> : ISuffixResult where TReturn : Structure
    {
        private Delegate del;

        public DeletageSuffixResult(Delegate del)
        {
            this.del = del;
        }

        public Structure Value()
        {
            //TODO:ERENDRAKE Make custom error
            throw new System.NotImplementedException();
        }

        public bool HasValue
        {
            get { return true; }
        }
    }
}