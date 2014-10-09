namespace kOS.Safe.Encapsulation.Suffixes
{
    public class NoArgsSuffix : SuffixBase
    {
        private readonly Del del;

        public delegate void Del();

        public NoArgsSuffix(Del del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}