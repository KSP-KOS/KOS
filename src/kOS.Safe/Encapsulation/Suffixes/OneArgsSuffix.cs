namespace kOS.Safe.Encapsulation.Suffixes
{
    public class OneArgsSuffix<TReturn,TParam> : SuffixBase
    {
        private readonly Del<TReturn,TParam> del;

        public delegate TInnerReturn Del<out TInnerReturn, in TInnerParam>(TInnerParam one);

        public OneArgsSuffix(Del<TReturn,TParam> del,string description = "") :base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}