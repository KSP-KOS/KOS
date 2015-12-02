using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;

namespace kOS.Safe.Execution
{
    public class CallbackValue : Structure
    {
        public string Identifier { get; private set; }
        public UserDelegate Delegate { get; private set; }
        private List<object> curriedArgs = new List<object>();

        public CallbackValue(UserDelegate del, string identifier)
        {
            Identifier = identifier;
            Delegate = del;

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("CALL", new VarArgsVoidSuffix<object>(Call));
            AddSuffix("CURRY", new VarArgsSuffix<CallbackValue, object>((args) => Curry(args)));
        }

        public void AddCurriedArg(object arg)
        {
            curriedArgs.Add(arg);
        }

        public void Call(params object[] args)
        {
            List<object> allArgs = new List<object>();
            allArgs.AddRange(curriedArgs);
            allArgs.AddRange(args);
            Delegate.Call(allArgs.ToArray());
        }

        public CallbackValue Curry(params object[] args)
        {
            CallbackValue curried = new CallbackValue(Delegate, Identifier);

            foreach (object arg in args)
            {
                curried.AddCurriedArg(arg);
            }

            return curried;
        }

        public override string ToString()
        {
            return "Callback('" + Identifier + "')";
        }

    }
}
