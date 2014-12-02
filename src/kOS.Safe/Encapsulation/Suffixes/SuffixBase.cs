namespace kOS.Safe.Encapsulation.Suffixes
{
    public abstract class SuffixBase : ISuffix
    {
        protected SuffixBase(string description)
        {
            Description = description;
        }
        public abstract object Get();

        public string Description { get; private set; }
    }
}
