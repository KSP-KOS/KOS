using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    [KOSNomenclature("Structure")]
    public abstract class Structure : ISuffixed 
    {
        private static readonly IDictionary<Type,IDictionary<string, ISuffix>> globalSuffixes;
        private readonly IDictionary<string, ISuffix> instanceSuffixes;
        private static readonly object globalSuffixLock = new object();

        private bool needToInitializeSuffixes = true;
        private List<Action> initializeSuffixCallbacks = new List<Action>();

        static Structure()
        {
            globalSuffixes = new Dictionary<Type, IDictionary<string, ISuffix>>();
            
        }

        protected Structure()
        {
            instanceSuffixes = new Dictionary<string, ISuffix>(StringComparer.OrdinalIgnoreCase);
            RegisterInitializer(InitializeInstanceSuffixes);
        }

        protected void RegisterInitializer(Action callback)
        {
            if (!initializeSuffixCallbacks.Contains(callback))
            {
                initializeSuffixCallbacks.Add(callback);
            }
        }

        private void callInitializeSuffixes()
        {
            if (needToInitializeSuffixes)
            {
                foreach (var callback in initializeSuffixCallbacks)
                {
                    callback.Invoke();
                }
                needToInitializeSuffixes = false;
            }
        }
        
        public string KOSName { get { return KOSNomenclature.GetKOSName(GetType()); } }


        private void InitializeInstanceSuffixes()
        {
              // Need to choose what sort of naming scheme to return before
              // enabling this one:
              //     AddSuffix("TYPENAME",   new NoArgsSuffix<StringValue>(() => GetType().ToString()));

              AddSuffix("TOSTRING",       new NoArgsSuffix<StringValue>(() => ToString()));
              AddSuffix("HASSUFFIX",      new OneArgsSuffix<BooleanValue, StringValue>(HasSuffix));
              AddSuffix("SUFFIXNAMES",    new NoArgsSuffix<ListValue>(GetSuffixNames));
              AddSuffix("ISSERIALIZABLE", new NoArgsSuffix<BooleanValue>(() => this is SerializableStructure));
              AddSuffix("TYPENAME",       new NoArgsSuffix<StringValue>(() => new StringValue(KOSName)));
              AddSuffix("ISTYPE",         new OneArgsSuffix<BooleanValue,StringValue>(GetKOSIsType));
              AddSuffix("INHERITANCE",    new NoArgsSuffix<StringValue>(GetKOSInheritance));
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
            lock (globalSuffixLock)
            {
                globalSuffixes[type] = typeSuffixes;
            }
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

        /// <summary>
        /// Set a suffix of this structure that has suffixName to the given value.
        /// If failOkay is false then it will throw exception if it fails to find the suffix.
        /// If failOkay is true then it will continue happily if it fails to find the suffix.
        /// </summary>
        /// <param name="suffixName"></param>
        /// <param name="value"></param>
        /// <param name="failOkay"></param>
        /// <returns>false if failOkay was true and it failed to find the suffix</returns>
        public virtual bool SetSuffix(string suffixName, object value, bool failOkay = false)
        {
            callInitializeSuffixes();
            var suffixes = GetStaticSuffixesForType(GetType());

            if (!ProcessSetSuffix(suffixes, suffixName, value, failOkay))
            {
                return ProcessSetSuffix(instanceSuffixes, suffixName, value, failOkay);
            }
            return false;
        }

        private bool ProcessSetSuffix(IDictionary<string, ISuffix> suffixes, string suffixName, object value, bool failOkay = false)
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
                if (failOkay)
                    return false;
                else
                    throw new KOSSuffixUseException("set", suffixName, this);
            }
            return false;
        }

        /// <summary>
        /// Get the suffix with this name, or if it fails to find it, then either
        /// throw exception or merely return null.  (Will return null only if failOkay is true).
        /// </summary>
        /// <param name="suffixName"></param>
        /// <param name="failOkay"></param>
        /// <returns></returns>
        public virtual ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
        {
            callInitializeSuffixes();
            ISuffix suffix;
            if (instanceSuffixes.TryGetValue(suffixName, out suffix))
            {
                return suffix.Get();
            }

            var suffixes = GetStaticSuffixesForType(GetType());

            if (!suffixes.TryGetValue(suffixName, out suffix))
            {
                if (failOkay)
                    return null;
                else
                    throw new KOSSuffixUseException("get",suffixName,this);
            }
            return suffix.Get();
        }
        
        public virtual BooleanValue HasSuffix(StringValue suffixName)
        {
            callInitializeSuffixes();
            if (instanceSuffixes.ContainsKey(suffixName.ToString()))
                return true;
            if (GetStaticSuffixesForType(GetType()).ContainsKey(suffixName.ToString()))
                return true;
            return false;
        }
        
        public virtual ListValue GetSuffixNames()
        {
            callInitializeSuffixes();
            List<StringValue> names = new List<StringValue>();

            foreach (var suffix in instanceSuffixes.Keys) names.Add(suffix);
            foreach (var staticSuffix in GetStaticSuffixesForType(GetType()).Keys) names.Add(staticSuffix);
            
            // Return the list alphabetized by suffix name.  The key lookups above, since they're coming
            // from a hashed dictionary, won't be in any predictable ordering:
            return new ListValue(names.OrderBy(item => item.ToString()));
        }
        
        public virtual BooleanValue GetKOSIsType(StringValue queryTypeName)
        {
            // We can't use Reflection's IsAssignableFrom because of the annoying way Generics work under Reflection.
            
            for (Type t = GetType() ; t != null ; t = t.BaseType)
            {
                // Our KOSNomenclature mapping can't store a Dictionary mapping for all
                // the new generics types that get made on the fly and weren't present when the static constructor was made.
                // So instead we ask Reflection to get the base from which it came so we can look that up instead.
                if (t.IsGenericType)
                    t = t.GetGenericTypeDefinition();
                
                if (KOSNomenclature.HasKOSName(t))
                {
                    string kOSname = KOSNomenclature.GetKOSName(t);
                    if (kOSname == queryTypeName)
                        return true;
                    if (t == typeof(Structure))
                        break; // don't bother walking further up - there won't be any more KOS types above this.
                }
            }
            return false;
        }
        
        public virtual StringValue GetKOSInheritance()
        {
            StringBuilder sb = new StringBuilder();
            
            string prevKosName = "";
            
            for (Type t = GetType() ; t != null ; t = t.BaseType)
            {
                // Our KOSNomenclature mapping can't store a Dictionary mapping for all
                // the new generics types that get made on the fly and weren't present when the static constructor was made.
                // So instead we ask Reflection to get the base from which it came so we can look that up instead.
                if (t.IsGenericType)
                    t = t.GetGenericTypeDefinition();
                
                if (KOSNomenclature.HasKOSName(t))
                {
                    string kOSname = KOSNomenclature.GetKOSName(t);
                    if (kOSname != prevKosName) // skip extra iterations where we mash parent C# types and child C# types into the same KOS type.
                    {
                        if (prevKosName != "")
                            sb.Append(" derived from ");
                        sb.Append(kOSname);
                    }
                    prevKosName = kOSname;
                    if (t == typeof(Structure))
                        break; // don't bother walking further up - there won't be any more KOS types above this.
                }
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return KOSNomenclature.GetKOSName(GetType()) + ": \"\""; // print as the KOSNomenclature string name, will look like: Structure: ""
        }

        /// <summary>
        /// Attempt to convert the given object into a kOS encapsulation type (something
        /// derived from kOS.Safe.Encapsulation.Structure), returning that instead.
        /// This never throws exception or complains in any way if the conversion cannot happen.
        /// Insted in that case it just silently ignores the request and returns the original object
        /// reference unchanged.  Thus it is safe to call it "just in case", even in places where it won't
        /// always be necessary, or have an effect at all.  You should use in anywhere you need to
        /// ensure that a value a user's script might see on the stack or in a script variable is properly
        /// wrapped in a kOS Structure, and not just a raw primitive like int or double.
        /// </summary>
        /// <param name="value">value to convert</param>
        /// <returns>new converted value, or original value if conversion couldn't happen or was unnecesary</returns>
        public static object FromPrimitive(object value)
        {
            if (value == null)
                return value; // If a null exists, let it pass through so it will bomb elsewhere, not here in FromPrimitive() where the exception message would be obtuse.

            if (value is Structure)
                return value; // Conversion is unnecessary - it's already a Structure.

            var convert = value as IConvertible;
            if (convert == null)
                return value; // Conversion isn't even theoretically possible.

            TypeCode code = convert.GetTypeCode();
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
                    return new StringValue(Convert.ToString(convert, CultureInfo.CurrentCulture));
                default:
                    break;
            }
            return value; // Conversion is one this method didn't implement.
        }

        /// <summary>
        /// This is identical to FromPrimitive, except that it WILL throw an exception
        /// if it was unable to guarantee that the result became (or already was) a kOS Structure.
        /// </summary>
        /// <param name="value">value to convert</param>
        /// <returns>value after conversion, or original value if conversion unnecessary</returns>
        public static Structure FromPrimitiveWithAssert(object value)
        {
            object convertedVal = FromPrimitive(value);
            Structure returnValue = convertedVal as Structure;
            if (returnValue == null)
                throw new KOSException(
                    string.Format("Internal Error.  Contact the kOS developers with the phrase 'impossible FromPrimitiveWithAssert({0}) was attempted'.\nAlso include the output log if you can.",
                                  value == null ? "<null>" : value.GetType().ToString()));
            return returnValue;
        }

        public static object ToPrimitive(object value)
        {
            var primitive = value as PrimitiveStructure;
            if (primitive != null)
            {
                return primitive.ToPrimitive();
            }

            return value;
        }
    }
}
