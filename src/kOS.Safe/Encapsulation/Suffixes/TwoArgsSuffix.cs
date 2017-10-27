using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class TwoArgsSuffix<TReturn, TParam, TParam2> : SuffixBase where TReturn : Structure where TParam : Structure where TParam2 : Structure
    {
        private readonly Del<TReturn, TParam, TParam2> del;

        public delegate TInnerReturn Del<out TInnerReturn, in TInnerParam, in TInnerParam2>(TInnerParam one, TInnerParam2 two);

        public TwoArgsSuffix(Del<TReturn, TParam, TParam2> del, string description = "")
            : base(description)
        {
            this.del = del;
        }

        protected override object Call(object[] args)
        {
            return (TReturn)del((TParam)args[0], (TParam2)args[1]);
        }

        protected override Delegate Delegate
        {
            get
            {
                return del;
            }
        }
    }

    public class TwoArgsSuffix<TParam, TParam2> : SuffixBase where TParam : Structure where TParam2 : Structure
    {
        private readonly Del<TParam, TParam2> del;

        public delegate void Del<in TInnerParam, in TInnerParam2>(TInnerParam one, TInnerParam2 two);

        public TwoArgsSuffix(Del<TParam, TParam2> del, string description = "")
            : base(description)
        {
            this.del = del;
        }

        protected override object Call(object[] args)
        {
            del((TParam)args[0], (TParam2)args[1]);
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