namespace kOS.Safe.Encapsulation.Suffixes
{
    public class OneArgsSuffix<TParam> : SuffixBase
    {
        private readonly Del<TParam> del;

        public delegate void Del<in TInnerParam>(TInnerParam argOne);

        public OneArgsSuffix(Del<TParam> del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}