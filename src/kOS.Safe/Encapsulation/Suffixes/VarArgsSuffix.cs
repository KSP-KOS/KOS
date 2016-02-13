namespace kOS.Safe.Encapsulation.Suffixes
{
    public class VarArgsSuffix<TReturn, TParam> : SuffixBase where TReturn : Structure where TParam : Structure
    {
        private readonly Del<TReturn, TParam> del;

        public delegate TInnerReturn Del<out TInnerReturn, in TInnerParam>(params TInnerParam[] arguments);

        public VarArgsSuffix(Del<TReturn, TParam> del, string description = "") : base(description)
        {
            this.del = del;
        }

        public override ISuffixResult Get()
        {
            return new DelegateSuffixResult(del);
        }
    }
}