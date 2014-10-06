namespace kOS.Safe.Encapsulation.Suffixes
{
    public class NoArgsVoidSuffix : SuffixBase
    {
        private readonly Del del;

        public delegate void Del();

        public NoArgsVoidSuffix(Del del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}