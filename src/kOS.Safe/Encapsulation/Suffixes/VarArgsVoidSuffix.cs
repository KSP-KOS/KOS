namespace kOS.Safe.Encapsulation.Suffixes
{
    public class VarArgsVoidSuffix<TParam> : SuffixBase
    {
        private readonly Del<TParam> del;

        public delegate void Del<in TInnerParam>(params TInnerParam[] arguments);

        public VarArgsVoidSuffix(Del<TParam> del, string description = "") : base(description)
        {
            this.del = del;
        }

        public override object Get()
        {
            return del;
        }
    }
}