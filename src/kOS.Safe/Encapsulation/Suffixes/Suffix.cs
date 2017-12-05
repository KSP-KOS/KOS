using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class Suffix<TReturn> : SuffixBase where TReturn : Structure
    {
        private readonly SuffixGetDlg<TReturn> getter;

        public Suffix(SuffixGetDlg<TReturn> getter, string description = ""):base(description)
        {
            this.getter = getter;
        }

        public override ISuffixResult Get()
        {
            return new SuffixResult(getter.Invoke());
        }

        protected override object Call(object[] args)
        {
            // We are overriding Get so no need to implement this
            throw new NotImplementedException();
        }

        protected override Delegate Delegate
        {
            get
            {
                // We are override Get so no need to implement
                throw new NotImplementedException();
            }
        }
    }
}