using System;
using System.Collections.Generic;
using kOS.Safe.Execution;

namespace kOS.Safe.Utilities
{
    public class CpuUtility
    {
        public static readonly Type ArgMarkerType = typeof (KOSArgMarkerType);

        /// <summary>
        /// Take the topmost arguments down to the ARG_MARKER_STRING, pop them off, and then
        /// put them back again in reversed order so a function can read them in normal order.
        /// Note that if this is an indirect call, it will also consume the thing just under
        /// the ARG_MARKER, since that's expected to be the delegate or KOSDelegate that we already
        /// read and pulled the needed information from.
        /// <param name="cpu">the cpu we are running on, fur stack manipulation purposes</param>
        /// <param name="direct">need to know if this was a direct or indirect call.  If indirect,
        /// then that means it also needs to consume the indirect reference off the stack just under
        /// the args</param>
        /// </summary>
        public static void ReverseStackArgs(ICpu cpu, bool direct)
        {
            List<object> args = new List<object>();
            object arg = cpu.PopValue();
            while (cpu.GetStackSize() > 0 && arg.GetType() != ArgMarkerType)
            {
                args.Add(arg);

                // It's important to dereference with PopValue, not using PopStack, because the function
                // being called might not even be able to see the variable in scope anyway.
                // In other words, if calling a function like so:
                //     declare foo to 3.
                //     myfunc(foo).
                // The code inside myfunc needs to see that as being identical to just saying:
                //     myfunc(3).
                // It has to be unaware of the fact that the name of the argument was 'foo'.  It just needs to
                // see the contents that were inside foo.
                arg = cpu.PopValue();
            }
            if (! direct)
                cpu.PopStack(); // throw away the delegate or KOSDelegate info - we already snarfed it by now.
            // Push the arg marker back on again.
            cpu.PushStack(new KOSArgMarkerType());
            // Push the arguments back on again, which will invert their order:
            foreach (object item in args)
                cpu.PushStack(item);
        }
    }
}
