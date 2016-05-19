using System;
using kOS.Safe.Persistence;

namespace kOS.Safe.Execution
{
    public abstract class InternalPath : GlobalPath
    {
        public InternalPath() : base("kOS")
        {

        }

        public abstract string Line(int line);
    }
}
