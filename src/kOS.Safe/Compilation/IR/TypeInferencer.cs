using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Compilation.IR
{
    public static class TypeInferencer
    {
        private static readonly Dictionary<Type, Dictionary<string, Type>> suffixDictionaries = new Dictionary<Type, Dictionary<string, Type>>();
        private static IDictionary<Type, IDictionary<string, ISuffix>> structureGlobalSuffixRef;
        private static readonly Dictionary<Type, Type> indexTypes = new Dictionary<Type, Type>();
        private static readonly FieldInfo instanceSuffixesRef;

        static TypeInferencer()
        {
            instanceSuffixesRef = typeof(Structure).GetField("instanceSuffixes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            // Handle abstract classes that cannot be done through reflection.
            // Structure:
            Dictionary<string, Type> structureDict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            AddStructureSuffixes(structureDict);
            suffixDictionaries.Add(typeof(Structure), structureDict);

            // EnumerableValue:
            structureDict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            AddStructureSuffixes(structureDict);
            AddEnumerableSuffixes(structureDict);
            suffixDictionaries.Add(typeof(EnumerableValue<,>), structureDict);

            // CollectionValue:
            structureDict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            AddStructureSuffixes(structureDict);
            AddEnumerableSuffixes(structureDict);
            structureDict.Add("CLEAR", null);
            suffixDictionaries.Add(typeof(CollectionValue<,>), structureDict);

            // KOSDelegate:
            structureDict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            AddStructureSuffixes(structureDict);
            structureDict.Add("CALL", typeof(Structure));
            structureDict.Add("BIND", typeof(KOSDelegate));
            structureDict.Add("ISDEAD", typeof(BooleanValue));
            suffixDictionaries.Add(typeof(KOSDelegate), structureDict);

            // ScalarValue:
            structureDict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            AddStructureSuffixes(structureDict);
            structureDict.Add("ISINTEGER", typeof(BooleanValue));
            structureDict.Add("ISVALID", typeof(BooleanValue));
            suffixDictionaries.Add(typeof(ScalarValue), structureDict);

            void AddStructureSuffixes(Dictionary<string, Type> suffixDict)
            {
                suffixDict.Add("TOSTRING", typeof(StringValue));
                suffixDict.Add("HASSUFFIX", typeof(BooleanValue));
                suffixDict.Add("SUFFIXNAMES", typeof(ListValue));
                suffixDict.Add("ISSERIALIZABLE", typeof(BooleanValue));
                suffixDict.Add("TYPENAME", typeof(StringValue));
                suffixDict.Add("ISTYPE", typeof(BooleanValue));
                suffixDict.Add("INHERITANCE", typeof(StringValue));
            }
            void AddEnumerableSuffixes(Dictionary<string, Type> suffixDict)
            {
                suffixDict.Add("ITERATOR", typeof(Enumerator));
                suffixDict.Add("REVERSEITERATOR", typeof(Enumerator));
                suffixDict.Add("LENGTH", typeof(ScalarValue));
                suffixDict.Add("CONTAINS", typeof(BooleanValue));
                suffixDict.Add("EMPTY", typeof(BooleanValue));
                suffixDict.Add("DUMP", typeof(StringValue));
            }
        }

        public static Type GetTypeForSuffix(Type structureType, string suffix)
        {
            if (!typeof(ISuffixed).IsAssignableFrom(structureType))
                throw new InvalidOperationException($"Tried to get suffixes on a non-suffixed type {structureType}.");

            if (suffixDictionaries.ContainsKey(structureType))
            {
                if (suffixDictionaries[structureType].TryGetValue(suffix, out Type suffixType))
                    return suffixType;
                if (typeof(Lexicon).IsAssignableFrom(structureType))
                    return typeof(Structure);
                //throw new Exceptions.KOSSuffixUseException("get", suffix, structureType.ToString());
                return typeof(Structure);
            }

            // Processing won't work for an abstract type. They should already be added in the constructor.
            // But the constructor only considers the generic type definition.
            if (structureType.IsAbstract && structureType.IsGenericType)
                return GetTypeForSuffix(structureType.GetGenericTypeDefinition(), suffix);

            ProcessTypeForSuffixes(structureType);
            return GetTypeForSuffix(structureType, suffix);
        }

        private static void ProcessTypeForSuffixes(Type type)
        {
            if (!typeof(Structure).IsAssignableFrom(type))
                throw new Exceptions.KOSYouShouldNeverSeeThisException($"Tried to process the suffix types from a non-Structure type {type}.");
            if (type.IsAbstract)
                throw new Exceptions.KOSYouShouldNeverSeeThisException($"Tried to process the suffixes of an abstract type.");

            Dictionary<string, Type> suffixes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            suffixDictionaries.Add(type, suffixes);

            // Create an instance that we can examine.
            Structure instance = (Structure)Activator.CreateInstance(type, true);
            // Call HasSuffix on the instance object to force the lazy initialization of suffixes to act.
            instance.HasSuffix(string.Empty);

            // Grab the instanceSuffixes field and peek at its values
            //var test2 = typeof(Structure).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            //var test = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            //FieldInfo instanceSuffixField = type.GetField("instanceSuffixes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            IDictionary<string, ISuffix> instanceSuffixes = (IDictionary<string, ISuffix>)instanceSuffixesRef.GetValue(instance);

            // We'll also grab the globalSuffixes field
            IEnumerable<KeyValuePair<string, ISuffix>> globalSuffixes;
            if (structureGlobalSuffixRef == null)
            {
                FieldInfo globalSuffixField = typeof(Structure).GetField("globalSuffixes", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                structureGlobalSuffixRef = (IDictionary<Type, IDictionary<string, ISuffix>>)globalSuffixField.GetValue(null);
            }
            if (structureGlobalSuffixRef.ContainsKey(type))
                globalSuffixes = structureGlobalSuffixRef[type];
            else
                globalSuffixes = Enumerable.Empty<KeyValuePair<string, ISuffix>>();

            foreach (KeyValuePair<string, ISuffix> suffixEntry in instanceSuffixes.Union(globalSuffixes))
            {
                string suffixName = suffixEntry.Key;
                ISuffix suffixObj = suffixEntry.Value;
                Type iSuffixType = suffixObj.GetType();

                if (!iSuffixType.IsGenericType)
                {
                    // Being non-generic means that no return type is defined
                    // This covers:
                    //  NoArgsVoidSuffix
                    suffixes.Add(suffixName, null);
                    continue;
                }

                // Strip the generic type arguments to allow comparison
                Type suffixGenericType = iSuffixType.GetGenericTypeDefinition();

                // Check against the generic types where the return type is the first argument:
                if (typeof(NoArgsSuffix<>).IsAssignableFrom(suffixGenericType) ||
                    typeof(OneArgsSuffix<,>).IsAssignableFrom(suffixGenericType) ||
                    typeof(TwoArgsSuffix<,,>).IsAssignableFrom(suffixGenericType) ||
                    typeof(ThreeArgsSuffix<,,,>).IsAssignableFrom(suffixGenericType) ||
                    typeof(VarArgsSuffix<,>).IsAssignableFrom(suffixGenericType) ||
                    typeof(OptionalArgsSuffix<>).IsAssignableFrom(suffixGenericType) ||
                    typeof(Suffix<>).IsAssignableFrom(suffixGenericType))
                {
                    suffixes.Add(suffixName, iSuffixType.GetGenericArguments()[0]);
                    continue;
                }

                // Check against the void return types:
                if (typeof(OneArgsSuffix<>).IsAssignableFrom(suffixGenericType) ||
                    typeof(TwoArgsSuffix<,>).IsAssignableFrom(suffixGenericType) ||
                    typeof(ThreeArgsSuffix<,,>).IsAssignableFrom(suffixGenericType))
                {
                    suffixes.Add(suffixName, null);
                    continue;
                }

#if DEBUG
                throw new NotImplementedException($"Suffix {suffixName} in type {type} is not implemented in TypeInferencer. Probably because {iSuffixType} is not matched in the decision tree.");
#else
                // Fail gently, a null return will disable potential optimizations but won't break anything.
                Utilities.SafeHouse.Logger.LogError($"Suffix {suffixName} in type {type} is not implemented in TypeInferencer. Probably because {iSuffixType} is not matched in the decision tree.");
                suffixes.Add(suffixName, null);
#endif
            }
        }

        public static Type GetTypeForIndex(Type type)
        {
            if (!typeof(IIndexable).IsAssignableFrom(type))
                //throw new InvalidOperationException($"Tried to get the index from a non-indexable type {type}");
                return typeof(Structure);

            if (indexTypes.TryGetValue(type, out Type indexType))
            {
                return indexType;
            }
            indexType = ProcessIndexTypesForType(type);
            indexTypes.Add(type, indexType);
            return indexType;
        }

        private static Type ProcessIndexTypesForType(Type type)
        {
            if (!typeof(Structure).IsAssignableFrom(type))
                throw new Exceptions.KOSYouShouldNeverSeeThisException($"Tried to process the index from a non-Structure type {type}.");

            if (!type.IsGenericType)
            {
                // Covers Lexicon
                return typeof(Structure);
            }

            Type genericType = type.GetGenericTypeDefinition();
            // Covers List, Range, Queue, Stack
            if (typeof(EnumerableValue<,>).IsAssignableFrom(genericType))
                return genericType.GetGenericArguments()[0];

#if DEBUG
            throw new Exceptions.KOSYouShouldNeverSeeThisException($"A kOS developer hasn't implemented indexing correctly in TypeInferencer for {type}");
#else
            // Fail gently, a null return will disable potential optimizations but won't break anything.
            Utilities.SafeHouse.Logger.LogError($"Type {type} is not implemented in TypeInferencer for indexing.");
            return typeof(Structure);
#endif
        }
    }
}
