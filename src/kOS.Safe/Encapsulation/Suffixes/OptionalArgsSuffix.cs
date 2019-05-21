using System;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class OptionalArgsSuffix<TReturn> : SuffixBase where TReturn : Structure
    {
        private readonly Del<TReturn, Structure> del;
        private readonly Structure[] defaults;

        public delegate TInnerReturn Del<out TInnerReturn, in TInnerParam>(params TInnerParam[] arguments);

        public OptionalArgsSuffix(Del<TReturn, Structure> del, Structure[] defaults, string description = "") : base(description)
        {
            this.del = del;
            this.defaults = defaults;
        }

        protected override object Call(object[] args)
        {
            Structure [] argsPassed = (Structure []) args [0];
            if (argsPassed.Length > this.defaults.Length)
            {
                throw new KOSArgumentMismatchException (this.defaults.Length, argsPassed.Length, "Too many arguments.");
            }
            Structure [] argsClean = new Structure [this.defaults.Length];

            for (int i = 0; i < argsPassed.Length; i++)
            {
                argsClean[i] = (Structure) argsPassed[i];
            }
            for (int i = argsPassed.Length; i < this.defaults.Length; i++)
            {
                if(this.defaults[i] == null)
                {
                    throw new KOSArgumentMismatchException(this.defaults.Length, argsPassed.Length, "Missing required argument.");
                }
                argsClean[i] = this.defaults[i];
            }

            return (TReturn)del(argsClean);
        }

        protected override Delegate Delegate
        {
            get
            {
                return del;
            }
        }
    }
}
