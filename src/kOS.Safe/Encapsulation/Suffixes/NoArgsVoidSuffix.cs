namespace kOS.Safe.Encapsulation.Suffixes
{
    /// <summary>
    /// Although we always have a dummy return from every call in the VM,
    /// in the underlying C# a suffix might be backed by a Delegate that
    /// returns void.  Use this construct for suffixes that take no args
    /// and return nothing.  (that are only called for their effect).
    /// </summary>
    public class NoArgsVoidSuffix : SuffixBase
    {
        private readonly Del del;

        public delegate void Del();

        public NoArgsVoidSuffix(Del del, string description = ""):base(description)
        {
            this.del = del;
        }

        public override ISuffixResult Get()
        {
            return new DelegateSuffixResult(del);
        }
    }
}