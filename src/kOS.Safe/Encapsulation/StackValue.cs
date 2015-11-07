using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Encapsulation
{
    public class StackValue<T> : EnumerableValue<T, Stack<T>>
    {
        public StackValue() : this(new Stack<T>())
        {
        }

        public StackValue(IEnumerable<T> stackValue) : base("STACK", new Stack<T>(stackValue))
        {
            StackInitializeSuffixes();
        }

        public override int Count
        {
            get { return collection.Count; }
        }

        public void Push(T val)
        {
            collection.Push(val);
        }

        private void StackInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<StackValue<T>>       (() => new StackValue<T>(this)));
            AddSuffix("LENGTH",   new NoArgsSuffix<int>                 (() => collection.Count));
            AddSuffix("PUSH",     new OneArgsSuffix<T>                  (toPush => collection.Push(toPush)));
            AddSuffix("POP",      new NoArgsSuffix<T>                   (() => collection.Pop()));
            AddSuffix("PEEK",     new NoArgsSuffix<T>                   (() => collection.Peek()));
            AddSuffix("CLEAR",    new NoArgsSuffix                      (() => collection.Clear()));
        }

        public static StackValue<T> CreateStack<TU>(IEnumerable<TU> list)
        {
            return new StackValue<T>(list.Cast<T>());
        }
    }

    public class StackValue : StackValue<object>
    {
        public StackValue()
        {
            InitializeSuffixes();
        }

        public StackValue(IEnumerable<object> toCopy)
            : base(toCopy)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("COPY", new NoArgsSuffix<StackValue>(() => new StackValue(this)));
        }

        public new static StackValue CreateStack<T>(IEnumerable<T> toCopy)
        {
            return new StackValue(toCopy.Cast<object>());
        }
    }
}