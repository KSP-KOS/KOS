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

        public void InitState(ICpu cpu, Type argMarkerType)
        {
            throw new NotImplementedException();
        }
    }
}