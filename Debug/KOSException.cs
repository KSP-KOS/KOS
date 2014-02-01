using System;
using kOS.Context;

namespace kOS.Debug
{
    public class KOSException : Exception
    {
        public IExecutionContext Context;
        public IContextRunProgram Program;

        public KOSException(string message) : base(message)
        {
        }

        public KOSException(string message, IExecutionContext context) : this(message)
        {
            LineNumber = context.Line;
            Context = context;
            Program = context.FindClosestParentOfType<IContextRunProgram>();
        }

        public int LineNumber { get; set; }
    }
}