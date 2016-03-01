﻿using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using System.Collections.Generic;
using System.Text;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// Any situation where a function of any type is referenced as the
    /// value of a variable or suffix, it will be a variant of this class,
    /// which is the base class underneath references to user functions,
    /// built-in functions, or suffix methods.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Delegate")]
    public abstract class KOSDelegate : Structure
    {
        protected IList<Structure> PreBoundArgs { get; set; }

        protected ICpu Cpu { get; set; }

        protected KOSDelegate(ICpu cpu)
        {
            Cpu = cpu;
            PreBoundArgs = new List<Structure>();
            InitializeSuffixes();
        }

        protected KOSDelegate(KOSDelegate oldCopy)
        {
            Cpu = oldCopy.Cpu;
            PreBoundArgs = new List<Structure>();
            InitializeSuffixes();
            foreach (Structure ca in oldCopy.PreBoundArgs)
                PreBoundArgs.Add(ca);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("CALL", new VarArgsSuffix<Structure, Structure>(Call));
            AddSuffix("BIND", new VarArgsSuffix<KOSDelegate, Structure>(Bind));
        }

        public void AddPreBoundArg(Structure arg)
        {
            PreBoundArgs.Add(arg);
        }

        public Structure Call(params Structure[] args)
        {
            PushUnderArgs();
            Cpu.PushStack(new KOSArgMarkerType());
            foreach (Structure arg in PreBoundArgs)
            {
                Cpu.PushStack(arg);
            }
            foreach (Structure arg in args)
            {
                Cpu.PushStack(arg);
            }
            return Call();
        }

        /// <summary>
        /// Assuming normal args are already on the stack,
        /// then the prebound args would have to be inserted
        /// underneath them, which slightly violates the
        /// stack access rules.  So do it through this method
        /// if it's needed.  (If Call() was used, then it's not needed
        /// because Call() does this for you.  But if Call() was
        /// not used and someone just used bare parentheses like so:
        ///    function y { parameter a,b,c. print a+b+c. }
        ///    set x to y@:bind(1,2).
        ///    x(). // instead of saying x:call().
        /// then the prebound args don't get pushed by doing that, and they
        /// have to get pushed under the top by calling this.)
        /// </summary>
        public void InsertPreBoundArgs()
        {
            Stack<object> aboveArgs = new Stack<object>();
            object arg = ""; // doesn't matter what it is as long as it's non-null for the while check below.
            while (arg != null && !(arg is KOSArgMarkerType))
            {
                arg = Cpu.PopStack();
                if (!(arg is KOSArgMarkerType))
                    aboveArgs.Push(arg);
            }
            if (arg == null)
                throw new KOSException("KOSDelegate.InsertPreBoundArgs: Stack arg bottom missing.\n" +
                                       "Contact the kOS devs.  This message should 'never' happen.");
            // Now re-push the args back, putting the preBound ones at the bottom
            // where they belong:
            Cpu.PushStack(new KOSArgMarkerType());
            foreach (Structure item in PreBoundArgs)
            {
                Cpu.PushStack(item);
            }
            foreach (object item in aboveArgs) // Because this was pushed to a stack, this should show in reverse order.
            {
                Cpu.PushStack(item);
            }
        }

        /// <summary>
        /// Assuming the args have been pushed onto the stack already, with
        /// the argbottom marker under them, do the call of this delegate.
        /// </summary>
        public abstract Structure Call();

        /// <summary>
        /// If the derivative class needs to put anything on the stack underneath the
        /// KOSArgMarkerType and the args, it's given an opportunity to do so here by
        /// overriding this method.
        /// </summary>
        public abstract void PushUnderArgs();

        /// <summary>
        /// Should return a new instance of self, with all fields copied.
        /// </summary>
        /// <returns>Should return whatever the actual derived type is, not a raw KOSDelegate</returns>
        public abstract KOSDelegate Clone();

        /// <summary>
        /// This returns a new variant of the delegate that has the first
        /// parameters hardcoded.  If you call :bind(1,2,3) on a delegate that takes 5 arguments, you get
        /// a variant of the delegate that now only takes the lastmost 2 arguments, with the first two
        /// having been hardcoded to 1,2,3.
        /// This is actually the technique known as "partial function application" in C#'s terms.
        /// </summary>
        /// <param name="args">the arguments to be hardcoded at the front of the list of arguments, going left to right</param>
        /// <returns>a delegate that now takes fewer arguments, just the leftover ones that weren't hardcoded</returns>
        public KOSDelegate Bind(params Structure[] args)
        {
            KOSDelegate preBoundDel = Clone();

            foreach (Structure arg in args)
            {
                preBoundDel.AddPreBoundArg(arg);
            }

            return preBoundDel;
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("KOSDelegate(");
            foreach (Structure arg in PreBoundArgs)
            {
                str.Append("pre-bound arg " + arg + " ");
            }
            str.Append(")");
            return str.ToString();
        }
    }
}