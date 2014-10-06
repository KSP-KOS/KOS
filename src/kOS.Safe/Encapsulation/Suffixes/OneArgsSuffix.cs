namespace kOS.Safe.Encapsulation.Suffixes
{
    public class OneArgsSuffix<TR,T> : SuffixBase
    {
        private readonly Del<TR,T> del;

        public delegate TIR Del<out TIR, in TI>(TI one);

        public OneArgsSuffix(Del<TR,T> del,string description = "") :base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}