using System;
using System.Collections.Generic;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

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
            instanceSuffixes = new Dictionary<string, ISuffix>(StringComparer.OrdinalIgnoreCase);
            AddSuffix("TYPE", new Suffixes.Suffix<string>(() => GetType().ToString()));
        }

        protected void AddSuffix(string suffixName, ISuffix suffixToAdd)
        {
            AddSuffix(new[]{suffixName}, suffixToAdd);
        }

        protected void AddSuffix(IEnumerable<string> suffixNames, ISuffix suffixToAdd)
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

        protected static void AddGlobalSuffix<T>(IEnumerable<string> suffixNames, ISuffix suffixToAdd)
        {
            var type = typeof (T);
            var typeSuffixes = GetStaticSuffixesForType(type);

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

        private static IDictionary<string, ISuffix> GetStaticSuffixesForType(Type currentType)
        {
            lock (globalSuffixLock)
            {
                IDictionary<string, ISuffix> typeSuffixes;
                if (globalSuffixes.TryGetValue(currentType, out typeSuffixes))
                {
                    return typeSuffixes;
                }
                return new Dictionary<string, ISuffix>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public virtual bool SetSuffix(string suffixName, object value)
        {
            var suffixes = GetStaticSuffixesForType(GetType());

            if (!ProcessSetSuffix(suffixes, suffixName, value))
            {
                return ProcessSetSuffix(instanceSuffixes, suffixName, value);
            }
            return false;
        }

        private bool ProcessSetSuffix(IDictionary<string, ISuffix> suffixes, string suffixName, object value)
        {
            ISuffix suffix;
            if (suffixes.TryGetValue(suffixName, out suffix))
            {
                var settable = suffix as ISetSuffix;
                if (settable != null)
                {
                    settable.Set(value);
                    return true;
                }
                throw new KOSSuffixUseException("set", suffixName, this);
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

            var suffixes = GetStaticSuffixesForType(GetType());

            if (!suffixes.TryGetValue(suffixName, out suffix))
            {
                throw new KOSSuffixUseException("get",suffixName,this);
            }
            return suffix.Get();
        }

        public virtual object TryOperation(string op, object other, bool reverseOrder)
        {
            if (op == "==")
            {
                return Equals(other);
            }
            if (op == "<>")
            {
                return !Equals(other);
            }
            if (op == "+")
            {
                return ToString() + other;
            }

            var message = string.Format("Cannot perform the operation: {0} On Structures {1} and {2}", op, GetType(),
                other.GetType());
            SafeHouse.Logger.Log(message);
            throw new InvalidOperationException(message);
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

        public static StringValue operator +(Structure val1, Structure val2)
        {
            return new StringValue(val1.ToString() + val2.ToString());
        }

        public static object FromPrimitive(object value)
        {
            IConvertible convert = value as IConvertible;
            if (convert != null)
            {
                var code = convert.GetTypeCode();
                switch (code)
                {
                    case TypeCode.Boolean:
                        return new BooleanValue(Convert.ToBoolean(convert));
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Single:
                        return ScalarValue.Create(Convert.ToDouble(convert));
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return ScalarValue.Create(Convert.ToInt32(convert));
                    case TypeCode.String:
                        return new StringValue(Convert.ToString(convert));
                    default:
                        break;
                }
            }
            return value;
        }

        public static object ToPrimitive(object value)
        {
            if (value is ScalarValue)
            {
                return ((ScalarValue)value).Value;
            }
            else if (value is BooleanValue)
            {
                return ((BooleanValue)value).Value;
            }
            else if (value is StringValue)
            {
                return ((StringValue)value).ToString();
            }
            return value;
        }
    }
}
