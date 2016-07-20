using System;
using kOS.Safe.Persistence;

namespace kOS.Safe.Execution
{
    public abstract class InternalPath : GlobalPath
    {
        public InternalPath() : base("kOS")
        {

        }
        
        public InternalPath(string volumeId) : base(volumeId)
        {

        }

        public abstract string Line(int line);
    }
}
