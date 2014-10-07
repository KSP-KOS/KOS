namespace kOS.Safe.Encapsulation.Suffixes
{
    public class TwoArgsVoidSuffix<T,T2> : SuffixBase
    {
        private readonly Del<T, T2> del;

        public delegate void Del<TI,TI2>(T argOne, T2 argTwo);

        public TwoArgsVoidSuffix(Del<T,T2> del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}