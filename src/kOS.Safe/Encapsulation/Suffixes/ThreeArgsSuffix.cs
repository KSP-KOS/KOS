using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class ThreeArgsSuffix<TReturn, TParam, TParam2, TParam3> : SuffixBase
        where TReturn : Structure where TParam : Structure where TParam2 : Structure where TParam3 : Structure
    {
        private readonly Del<TReturn, TParam, TParam2, TParam3> del;

        public delegate TInnerReturn Del<out TInnerReturn, in TInnerParam, in TInnerParam2, in TInnerParam3>(TInnerParam one, TInnerParam2 two, TInnerParam3 three);

        public ThreeArgsSuffix(Del<TReturn, TParam, TParam2, TParam3> del, string description = "")
            : base(description)
        {
            this.del = del;
        }

        protected override object Call(object[] args)
        {
            return (TReturn)del((TParam)args[0], (TParam2)args[1], (TParam3)args[2]);
        }

        protected override Delegate Delegate
        {
            get
            {
                return del;
            }
        }
    }

    public class ThreeArgsSuffix<TParam, TParam2, TParam3> : SuffixBase where TParam : Structure where TParam2 : Structure where TParam3: Structure
    {
        private readonly Del<TParam, TParam2, TParam3> del;

        public delegate void Del<in TInnerParam, in TInnerParam2, in TInnerParam3>(TInnerParam one, TInnerParam2 two, TInnerParam3 three);

        public ThreeArgsSuffix(Del<TParam, TParam2, TParam3> del, string description = "")
            : base(description)
        {
            this.del = del;
        }

        protected override object Call(object[] args)
        {
            del((TParam)args[0], (TParam2)args[1], (TParam3)args[2]);
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