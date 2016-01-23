namespace kOS.Safe.Encapsulation.Suffixes
{
    public class NoArgsSuffix<TReturn> : SuffixBase where TReturn : Structure
    {
        private readonly Del<TReturn> del;

        public delegate TInnerReturn Del<out TInnerReturn>() where TInnerReturn : Structure;

        public NoArgsSuffix(Del<TReturn> del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override ISuffixResult Get()
        {
            return new DeletageSuffixResult<TReturn>(del);
        }
    }

}