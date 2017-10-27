using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class OneArgsSuffix<TParam> : SuffixBase where TParam : Structure
    {
        private readonly Del<TParam> del;

        public delegate void Del<in TInnerParam>(TInnerParam argOne) where TInnerParam : Structure;

        public OneArgsSuffix(Del<TParam> del, string description = ""):base(description)
        {
            this.del = del;
        }

        protected override object Call(object[] args)
        {
            del((TParam)args[0]);
            return null;
        }

        protected override Delegate Delegate
        {
            get
            {
                return del;
            }
        }
    }
}