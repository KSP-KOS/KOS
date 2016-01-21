namespace kOS.Safe.Encapsulation.Suffixes
{
    public class SuffixResult : ISuffixResult
    {
        private readonly Structure structure;

        public SuffixResult(Structure structure)
        {
            this.structure = structure;
        }

        public Structure Value()
        {
            return structure;
        }

        public bool HasValue
        {
            get { return true; }
        }
    }
}