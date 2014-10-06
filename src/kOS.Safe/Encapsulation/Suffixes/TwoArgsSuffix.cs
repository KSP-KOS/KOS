namespace kOS.Safe.Encapsulation.Suffixes
{
    public class TwoArgsSuffix<TR,T,T2> : SuffixBase
    {
        private readonly Del<TR,T,T2> del;

        public delegate TIR Del<out TIR, in TI, in TI2>(TI one, TI2 two);

        public TwoArgsSuffix(Del<TR,T,T2> del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}