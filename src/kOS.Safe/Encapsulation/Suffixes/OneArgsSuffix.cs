using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class OneArgsSuffix<TReturn,TParam> : SuffixBase where TReturn : Structure where TParam : Structure
    {
        private readonly Del<TReturn,TParam> del;

        public delegate TInnerReturn Del<out TInnerReturn, in TInnerParam>(TInnerParam one) where TInnerReturn : Structure;

        public OneArgsSuffix(Del<TReturn,TParam> del,string description = "") :base(description)
        {
            this.del = del;
        }

        protected override object Call(object[] args)
        {
            return (TReturn)del((TParam)args[0]);
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