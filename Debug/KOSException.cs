using System;

namespace kOS.Debug
{
    public class KOSException : Exception
    {
        public ExecutionContext Context;
        public ContextRunProgram Program;

        public KOSException(string message) : base(message)
        {
        }

        public KOSException(string message, ExecutionContext context) : this (message)
        {
            LineNumber = context.Line;
            Context = context;
            Program = context.FindClosestParentOfType<ContextRunProgram>();
        }

        public int LineNumber { get; set; }
    }
}
