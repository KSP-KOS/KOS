using System;
using System.Collections.Generic;

namespace kOS.Safe.Encapsulation
{
    public abstract class Structure : ISuffixed, IOperable 
    {
        private static readonly IDictionary<Type,IDictionary<string, ISuffix>> globalSuffixes;
        private readonly IDictionary<string, ISuffix> instanceSuffixes;
        private static readonly object globalSuffixLock = new object();

        static Structure()
        {
            globalSuffixes = new Dictionary<Type, IDictionary<string, ISuffix>>();
        }

        protected Structure()
        {
            instanceSuffixes = new Dictionary<string, ISuffix>();
        }

        protected void AddSuffix(string suffixName, ISuffix suffixToAdd)
        {
            AddSuffix(new[]{suffixName}, suffixToAdd);
        }

        protected void AddSuffix(string[] suffixNames, ISuffix suffixToAdd)
        {
            foreach (var suffixName in suffixNames)
            {
                if (instanceSuffixes.ContainsKey(suffixName))
                {
                    instanceSuffixes[suffixName] = suffixToAdd;
                }
                else
                {
                    instanceSuffixes.Add(suffixName, suffixToAdd);
                }
            }
        }

        protected static void AddGlobalSuffix<T>(string suffixName, ISuffix suffixToAdd)
        {
            AddGlobalSuffix<T>(new[]{suffixName}, suffixToAdd);
        }

        protected static void AddGlobalSuffix<T>(string[] suffixNames, ISuffix suffixToAdd)
        {
            var type = typeof (T);
            var typeSuffixes = GetSuffixesForType(type);

            foreach (var suffixName in suffixNames)
            {
                if (typeSuffixes.ContainsKey(suffixName))
                {
                    typeSuffixes[suffixName] = suffixToAdd;
                }
                else
                {
                    typeSuffixes.Add(suffixName, suffixToAdd);
                }
            }
            globalSuffixes[type] = typeSuffixes;
        }

        private static IDictionary<string, ISuffix> GetSuffixesForType(Type currentType)
        {
            lock (globalSuffixLock)
            {
                IDictionary<string, ISuffix> typeSuffixes;
                if (!globalSuffixes.TryGetValue(currentType, out typeSuffixes))
                {
                    typeSuffixes = new Dictionary<string, ISuffix>();
                }
                return typeSuffixes;
            }
        }

        public virtual bool SetSuffix(string suffixName, object value)
        {
            var suffixes = GetSuffixesForType(GetType());

            ISuffix suffix;
            if (suffixes.TryGetValue(suffixName, out suffix))
            {
                var settable = suffix as ISetSuffix;
                if (settable != null)
                {
                    settable.Set(value);
                    return true;
                }
            }
            return false;
        }

        public virtual object GetSuffix(string suffixName)
        {
            ISuffix suffix;
            if (instanceSuffixes.TryGetValue(suffixName, out suffix))
            {
                return suffix.Get();
            }

            var suffixes = GetSuffixesForType(GetType());

            if (!suffixes.TryGetValue(suffixName, out suffix))
            {
                return null;
            }
            return suffix.Get();
        }

        public virtual object TryOperation(string op, object other, bool reverseOrder)
        {
            return null;
        }

        protected object ConvertToDoubleIfNeeded(object value)
        {
            if (!(value is Structure) && !(value is double))
            {
                value = Convert.ToDouble(value);
            }

            return value;
        }

        public override string ToString()
        {
            return "Structure ";
        }
    }
}
