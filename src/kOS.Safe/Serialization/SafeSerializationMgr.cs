using System;
using System.Text;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using System.Linq;
using System.Reflection;
using kOS.Safe.Utilities;

namespace kOS.Safe.Serialization
{
    public class SafeSerializationMgr
    {
        public static string TYPE_KEY = "$type";
        private static HashSet<string> assemblies = new HashSet<string>();

        private readonly SafeSharedObjects safeSharedObjects;

        public SafeSerializationMgr(SafeSharedObjects sharedObjects)
        {
            this.safeSharedObjects = sharedObjects;
        }

        public static void AddAssembly(string assembly)
        {
            assemblies.Add(assembly);
        }

        public static bool IsSerializablePrimitive(object serialized)
        {
            return serialized.GetType().IsPrimitive || serialized is string || IsPrimitiveStructure(serialized);
        }

        public static bool IsPrimitiveStructure(object serialized)
        {
            return serialized is PrimitiveStructure;
        }

        private object DumpValue(object value, bool includeType)
        {
            var valueDumper = value as IDumper;

            if (valueDumper != null) {
                return Dump(valueDumper, includeType);
            } else if (value is Dump) {
                return value;
            } else if (value is List<object>) {
                return (value as List<object>).Select((v) => DumpValue(v, includeType)).ToList();
            } else if (IsSerializablePrimitive(value)) {
                return Structure.ToPrimitive(value);
            } else {
                return value.ToString();
            }
        }

        public Dump Dump(IDumper dumper, bool includeType = true)
        {
            var dump = dumper.Dump();

            List<object> keys = new List<object>(dump.Keys);

            foreach (object key in keys)
            {
                dump[key] = DumpValue(dump[key], includeType);
            }

            if (includeType)
            {
                dump.Add(TYPE_KEY, dumper.GetType().Namespace + "." + dumper.GetType().Name);
            }

            return dump;
        }

        public string Serialize(IDumper serialized, IFormatWriter formatter, bool includeType = true)
        {
            return formatter.Write(Dump(serialized, includeType));
        }

        public object CreateValue(object value)
        {
            var objects = value as Dump;
            if (objects != null)
            {
                return CreateFromDump(objects);
            } else if (value is List<object>)
            {
                return (value as List<object>).Select(item => CreateValue(item)).ToList();
            }

            return value;
        }

        public IDumper CreateFromDump(Dump dump)
        {
            var data = new Dump();

            foreach (KeyValuePair<object, object> entry in dump)
            {
                if (entry.Key.Equals(TYPE_KEY))
                {
                    continue;
                }

                data[entry.Key] = CreateValue(entry.Value);
            }

            if (!dump.ContainsKey(TYPE_KEY))
            {
                throw new KOSSerializationException("Type information missing");
            }

            string typeFullName = dump[TYPE_KEY] as string;

            return CreateAndLoad(typeFullName, data);
        }

        public virtual IDumper CreateAndLoad(string typeFullName, Dump data)
        {
            Type loadedType = GetTypeFromFullname(typeFullName);
            Type[] paramSignature = new Type[] { typeof(SafeSharedObjects), typeof(Dump) };
            MethodInfo method = loadedType.GetMethod("CreateFromDump", BindingFlags.Public | BindingFlags.Static, null, paramSignature, null);
            IDumper instance;
            try
            {
                instance = (IDumper)method.Invoke(null, new object[] { safeSharedObjects, data });
            }
            catch (TargetInvocationException reflectiveCallException)
            {
                // When you call a method via reflection with MethodInfo.Invoke(),
                // it hides any exceptions that method tried to throw inside its own
                // wrapper called TargetInvocationException.  That would mask our
                // intended error messages to the user if we didn't re-throw the actual 
                // exception the method wanted to generate like so:
                throw reflectiveCallException.InnerException;
            }
            return instance;
        }

        protected virtual Type GetTypeFromFullname(string typeFullName)
        {
            var deserializedType = Type.GetType(typeFullName);

            if (deserializedType == null)
            {
                foreach (string assembly in assemblies)
                {
                    deserializedType = Type.GetType(typeFullName + ", " + assembly);
                    if (deserializedType != null)
                    {
                        break;
                    }
                }
            }
            return deserializedType;
        }

        static bool staticsAlreadyChecked= false;
        /// <summary>
        /// Ensure all classes implementing IDumper have the required static method in
        /// them, which is something the compiler cannot enforce itself because an Interface
        /// can't contain static things.
        /// </summary>
        /// Since interfaces don't enforce static things, this enforcement
        /// is being done via this Reflection check upon loading that will make nag messages
        /// if the CreateFromDump is missing on one of the classes.
        /// Note, if we ever need this check elsewhere for another "static" thing in an interface,
        /// It should be possible to make this more generic and make a library method here that
        /// takes any interface name and any method name and checks to see if it exists.
        public static void CheckIDumperStatics()
        {
            if (staticsAlreadyChecked)
                return;

            StringBuilder message = new StringBuilder(1000);

            Type dumperInterface = typeof(IDumper);

            // It's ugly to be checking ALL KSP mods here, but just in case someone
            // extends kOS in another mod, it's not safe to check ONLY kOS.dll and kOS.Safe.dll:
            Assembly[] allKSPAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            // All the classes in which the class is derived from IDumper, and instances of the class are
            // actually constructable because it isn't Abstract:
            IEnumerable<Type> iDumperClasses = allKSPAssemblies.SelectMany(a => ReflectUtil.GetLoadedTypes(a)).Where(
                b => dumperInterface.IsAssignableFrom(b) && b.IsClass && !b.IsAbstract);

            List<string> offendingClasses = new List<string>();
            Type[] paramSignature  = new Type[] { typeof(SafeSharedObjects), typeof(Dump) };
            foreach (Type t in iDumperClasses)
            {
                if (t.GetMethod("CreateFromDump", BindingFlags.Public | BindingFlags.Static, null, paramSignature, null) == null)
                    offendingClasses.Add(t.FullName);
            }

            if (offendingClasses.Count() > 0)
            {
                message.Append(
                    "kOS DEV TEAM ERROR.\n " +
                    "  This is a kOS source problem that the compiler\n" +
                    "  cannot check for because of limitations in C#.\n" +
                    "  (So we check it at runtime and give this error\n" +
                    "  if it's detected.)\n" +
                    "  \n" +
                    "  The following class(es) implement IDumper, but\n" +
                    "  lack the required CreateFromDump() static method\n" +
                    "  that we need all IDumper's to have:\n" +
                    "    ");
                message.Append(string.Join(", ", offendingClasses.ToArray()));
                Debug.AddNagMessage(Debug.NagType.NAGFOREVER, message.ToString());
            }
            staticsAlreadyChecked = true;
        }

        public IDumper Deserialize(string input, IFormatReader formatter)
        {
            Dump dump = formatter.Read(input);

            return dump == null ? null : CreateFromDump(dump);
        }

        public string ToString(IDumper dumper)
        {
            return Serialize(dumper, TerminalFormatter.Instance, false);
        }
    }
}

