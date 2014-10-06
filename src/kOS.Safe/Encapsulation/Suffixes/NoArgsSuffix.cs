namespace kOS.Safe.Encapsulation.Suffixes
{
    public class NoArgsSuffix<TR> : SuffixBase
    {
        private readonly Del<TR> del;

        public delegate TIR Del<out TIR>();

        public NoArgsSuffix(Del<TR> del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}