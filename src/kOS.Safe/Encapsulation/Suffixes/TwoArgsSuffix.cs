namespace kOS.Safe.Encapsulation.Suffixes
{
    public class TwoArgsSuffix<TReturn, TParam, TParam2> : SuffixBase
    {
        private readonly Del<TReturn, TParam, TParam2> del;

        public delegate TInnerReturn Del<out TInnerReturn, in TInnerParam, in TInnerParam2>(TInnerParam one, TInnerParam2 two);

        public TwoArgsSuffix(Del<TReturn, TParam, TParam2> del, string description = "")
            : base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }

    public class TwoArgsSuffix<TParam, TParam2> : SuffixBase
    {
        private readonly Del<TParam, TParam2> del;

        public delegate void Del<in TInnerParam, in TInnerParam2>(TInnerParam one, TInnerParam2 two);

        public TwoArgsSuffix(Del<TParam, TParam2> del, string description = "")
            : base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}