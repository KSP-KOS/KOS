namespace kOS.Safe.Encapsulation.Suffixes
{
    public class OneArgsVoidSuffix<T> : SuffixBase
    {
        private readonly Del<T> del;

        public delegate void Del<TI>(T argOne);

        public OneArgsVoidSuffix(Del<T> del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}