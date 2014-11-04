namespace kOS.Safe.Encapsulation.Suffixes
{
    public class NoArgsSuffix<TReturn> : SuffixBase
    {
        private readonly Del<TReturn> del;

        public delegate TInnerReturn Del<out TInnerReturn>();

        public NoArgsSuffix(Del<TReturn> del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}