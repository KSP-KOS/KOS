using System;
using kOS.Safe.Execution;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class SuffixResult : ISuffixResult
    {
        private readonly Structure structure;

        public SuffixResult(Structure structure)
        {
            this.structure = structure;
        }

        public Structure Value
        {
            get
            {
                return structure;
            }
        }

        public bool HasValue
        {
            get { return true; }
        }

        public void Invoke(ICpu cpu)
        {
            throw new NotImplementedException();
        }
        
        // Not something the user should ever see, but still useful for our debugging when we dump the stack:
        public override string ToString()
        {
            return string.Format("[SuffixResult Structure={0}]", (HasValue ? structure.ToString() : "<null>") );
        }
 
    }
}