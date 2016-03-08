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

        public override ISuffixResult Get()
        {
            return new DelegateSuffixResult(del);
        }
    }
}