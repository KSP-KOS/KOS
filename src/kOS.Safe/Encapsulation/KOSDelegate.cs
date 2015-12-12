using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using System.Text;

namespace kOS.Safe.Execution
{
    /// <summary>
    /// Any situation where a function of any type is referenced as the
    /// value of a variable or suffix, it will be a variant of this class,
    /// which is the base class underneath references to user functions,
    /// built-in functions, or suffix methods.
    /// </summary>
    public abstract class KOSDelegate : Structure
    {
        private List<object> curriedArgs = new List<object>();
        protected readonly ICpu cpu;
        
        public KOSDelegate(ICpu cpu)
        {
            this.cpu = cpu;
            InitializeSuffixes();
        }
        
        public KOSDelegate(KOSDelegate oldCopy)
        {
            cpu = oldCopy.cpu;
            InitializeSuffixes();
            foreach (object ca in oldCopy.curriedArgs)
                curriedArgs.Add(ca);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("CALL", new VarArgsSuffix<object, object>(Call));
            AddSuffix("CURRY", new VarArgsSuffix<KOSDelegate, object>(Curry));
        }

        public void AddCurriedArg(object arg)
        {
            curriedArgs.Add(arg);
        }

        public object Call(params object[] args)
        {
            PushUnderArgs();
            cpu.PushStack(new KOSArgMarkerType());
            foreach (object arg in curriedArgs)
            {
                cpu.PushStack(arg);
            }
            foreach (object arg in args)
            {
                cpu.PushStack(arg);
            }
            return Call();
        }
        
        /// <summary>
        /// Assuming the args have been pushed onto the stack already, with
        /// the argbottom marker under them, do the call of this delegate.
        /// </summary>
        public abstract object Call();
        
        /// <summary>
        /// If the derivative class needs to put anything on the stack underneath the
        /// KOSArgMarkerType and the args, it's given an opportunity to do so here by
        /// overriding this method.
        /// </summary>
        public abstract void PushUnderArgs();
        
        /// <summary>
        /// Should return a new instance of self, with all fields copied.
        /// </summary>
        /// <param name="KOSDelegate">Should be of type whatever the actual derived type is, not a raw KOSDelegate</param>
        /// <returns>Should return whatever the actual derived type is, not a raw KOSDelegate</returns>
        public abstract KOSDelegate Clone();

        /// <summary>
        /// This returns a new variant of the delegate that has the first 
        /// parameters hardcoded.  If you call :curry(1,2,3) on a delegate that takes 5 arguments, you get
        /// a variant of the delegate that now only takes the lastmost 2 arguments, with the first two
        /// having been hardcoded to 1,2,3.
        /// This is actually the technique known as "partial function application", but there's no nice way
        /// to abbreviate that, so it's being called "curry" even though the term "curry" really only applies
        /// to the case where you only shave off one parameter at a time in a chain.
        /// </summary>
        /// <param name="args">the arguments to be hardcoded at the front of the list of arguments, going left to right</param>
        /// <returns>a delegate that now takes fewer arguments, just the leftover ones that weren't hardcoded</returns>
        public KOSDelegate Curry(params object[] args)
        {
            KOSDelegate curried = Clone();

            foreach (object arg in args)
            {
                curried.AddCurriedArg(arg);
            }

            return curried;
        }
        
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("KOSDelegate(");
            foreach (object arg in curriedArgs)
            {
                str.Append("curried arg "+arg+" ");
            }
            str.Append(")");
            return str.ToString();
        }
    }
}
