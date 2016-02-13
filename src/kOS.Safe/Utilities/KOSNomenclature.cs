using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Utilities
{
    /// <summary>
    /// A class for the purpose of holding the mapping information that goes
    /// back and forth between C# type names and thier KOS type name equivalents.
    /// For the sake of this we are using unqualified C# names (without the namespace
    /// prefixes).
    /// 
    /// Any time you add a new class derived from kOS.Safe.Encapsulated.Structure, you
    /// should check to see if you should add a mapping to this class to go with it.
    /// 
    /// This class is intended to be used "statically" without an actual instance of it
    /// being necessary.  There should be only one copy of its data, globally, across
    /// the entire process.
    /// </summary>
    public class KosNomenclature
    {
        private static Dictionary<string,string> kosToCSharpMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string,string> cSharpToKosMap = new Dictionary<string, string>(StringComparer.Ordinal);

        private static string NagHeader =
            "***********************************************\n" +
            "* The following C# types need an AddMapping() *\n" +
            "* call in the KosNomenclatureMapper class.    *\n" +
            "* If you are seeing this, that means a kOS    *\n" +
            "* developer forgot something.  If you _are_   *\n" +
            "* a kOS developer and see this after you      *\n" +
            "* just made a new change to the code.  Then   *\n" +
            "* read the explanatory comments in the code   *\n" +
            "* for KosNomenclatureManager.NagCheck().      *\n" +
            "***********************************************\n";

        // Neither public nor private because a static constructore can't be called explicitly anyway.
        static KosNomenclature()
        {
            PopulateMapping();
            NagCheck();
        }
        
        public static void GuaranteeStaticConstruction()
        {
            Console.WriteLine("eraseme: Proof that GuaranteeStaticConstruction got called.\n");
            // C# will only run the static constructor for a class once some code tries to "touch" it
            // in some way.  Before then the static construction doesn't happen yet.  Calling a static
            // method ensures the system will execute the static constructor now if it hasn't once already.
            //
            // That's why this method has no code in its body.  The mere act of attempting to call it causes
            // the desired effect.
            //
            // The reason for wanting to guarantee the static constructor happens even during program runs
            // where this class never gets used is to ensure the NagCheck() method below always happens
            // even when the developer who just compiled the code and is testing it isn't attemping to use
            // the :TYPENAME or :ISTYPE suffixes during that test.
        }

        private static void PopulateMapping()
        {
            // It would be better if this could be handled by having a static name member of Structure that
            // everyone overrides, but C# doesn't let you override static members, so we do this instead:
            
            //
            // Everything you see in this list without a kOS name given is the same name
            // for kOS as it is for C#
            //
            //          C# name                        kOS name(s)
            //          ------------------------       ---------------------------------------
            AddMapping("Structure",                   "Structure");
            AddOneWayMapping("SerializableStructure", "Structure");
            AddMapping("ActiveResource");
            AddMapping("Addon");
            AddMapping("AddonList");
            AddMapping("AggregateResourceValue");
            AddMapping("BodyAtmosphere",              "BodyAtmosphere");
            AddMapping("Career");
            AddMapping("Config");
            AddMapping("ConfigKey"); // Why derived from Structure?
            AddMapping("ConstantValue",               "Constant");
            AddMapping("CrewMember");
            AddMapping("Direction",                   "Direction", "Rotation");
            AddMapping("ElementValue",                "Element");
            AddMapping("Enumerator",                  "Iterator");
            AddMapping("EnumerableValue`2",           "Enumerable"); // will kOS users ever see this?  It's the abstract base of collections.
            AddMapping("FileInfo",                    "File");
            AddMapping("FlightControl",               "Control");
            AddMapping("KOSDelegate",                 "Delegate");
            AddMapping("BuiltinDelegate",             "BuiltinDelegate");
            AddMapping("BooleanValue",                "Boolean");
            AddMapping("Lexicon");
            AddMapping("ListValue",                   "List");
            AddOneWayMapping("ListValue`1",           "List"); // The "`1" means "generic taking 1 type". This is coming from ListValue<T>
            AddMapping("PIDLoop");
            AddMapping("ScalarValue",                 "Scalar");
            AddOneWayMapping("ScalarDoubleValue",     "Scalar");
            AddOneWayMapping("ScalarIntValue",        "Scalar");
            AddMapping("StringValue",                 "String");
            AddMapping("TerminalStruct",              "Terminal");
            AddMapping("UserDelegate",                "UserFunctionDelegate");
            AddMapping("VersionInfo",                 "Version");
            AddMapping("KOSPassThruReturn",           "YouShouldNeverSeeThis_KOSPassThruReturn");
            AddMapping("Volume",                      "Volume");
            AddOneWayMapping("Archive",               "Volume");
            AddOneWayMapping("Harddisk",              "Volume");
            AddMapping("QueueValue",                  "Queue");
            AddOneWayMapping("QueueValue`1",          "Queue"); // The "`1" means "generic taking 1 type".  this is coming from QueueValue<T>
            AddMapping("StackValue",                  "Stack");
            AddOneWayMapping("StackValue`1",          "Stack"); // The "`1" means "generic taking 1 type".  this is coming from StackValue<T>
        }
        
        /// <summary>
        /// Report nag message on terminal if there is a C# type derived from kOS.Safe.Encapsulated.Structure
        /// which was not mentioned in the PopulateMapping() method.  That's a no-no.  ALL C# types derived from Structure
        /// MUST get an AddMapping() call in PopulateMapping().  Note for this each type needs its own entry, even
        /// if derived from a base class that alreay has an entry.  (Otherwise everything would pass the check merely
        /// by virtue of being derived from Structure, which isn't what we want.)
        /// 
        /// (This is really a developer problem that we'd like to catch at compile time, but we can't enforce this with the compiler.)
        /// 
        /// </summary>
        private static void NagCheck()
        {
            Console.WriteLine("eraseme: Proof that NagCheck got called.\n");
            StringBuilder message = new StringBuilder();

            // This technique is a bit slow, but it only needs to be performed exactly once in the life of the mod.
            // Reflection contains no information pointing from parent types down to derived types.  Therefore the only way
            // to get this is to walk all the types in the assembly like this:
            IEnumerable<Type> structureTypes = typeof(KosNomenclature).Assembly.GetTypes().Where( t => t.IsSubclassOf(typeof(Structure)) );

            foreach (Type t in structureTypes)
            {
                if (! cSharpToKosMap.ContainsKey(t.Name))
                {
                    if (message.Length == 0)
                        message.Append(NagHeader);
                    else
                        message.Append(", ");

                    message.Append(t.Name);
                }
            }
            
            if (message.Length > 0)
            {
                SafeHouse.Logger.Log(message.ToString());
                Debug.AddNagMessage(Debug.NagType.NAGFOREVER, message.ToString());
            }
        }
        
        /// <summary>
        /// Add a mapping from one CSharp type name to and from one or more kOS type names.
        /// </summary>
        /// <param name="cSharpName">the C# name of the type (without the namespace prefixes)</param>
        /// <param name="kosNames">(varying args) zero or more kOS names.<br/>
        /// If zero names are given, then the kOS name and the C# name are assumed to be equal.<br/>
        /// If one or more names are given, the first is canonical and the rest are aliases.</param>
        private static void AddMapping(string cSharpName, params string[] kosNames)
        {
            if (kosNames.Length == 0)
            {
                // C# and kOS names match:
                cSharpToKosMap.Add(cSharpName, cSharpName);
                kosToCSharpMap.Add(cSharpName, cSharpName);
            }
            else
            {
                cSharpToKosMap.Add(cSharpName, kosNames[0]); // the canonical name is the one reported when going from C# name to kOS name.
                foreach (string kosName in kosNames)
                    kosToCSharpMap.Add(kosName, cSharpName); // Any of the alias names are equally good when going from kOS name to C# name.
            }
        }
        
        /// <summary>
        /// Add a mapping that gives a kOS name from a C# name, but does not allow the other way.
        /// Useful for cases where the difference between some C# types is being deliberately
        /// obfuscated from the kerboscript code and you want multple C# types to all give the
        /// same answer when asked for their KOS type.
        /// </summary>
        /// <param name="cSharpName"></param>
        /// <param name="kosName"></param>
        private static void AddOneWayMapping(string cSharpName, string kosName)
        {
            cSharpToKosMap.Add(cSharpName, kosName);
        }
        
        /// <summary>
        /// Return the kOS type name corresponding to a C# type name.  Never bombs out, instead
        /// returning the original name as-is if it wasn't found in the lookup.  This is a
        /// case-sensitive lookup because C# type names are case sensitive and there could
        /// hypothetically be two different type names that differ by case only.
        /// </summary>
        /// <param name="cSharpName"></param>
        /// <returns></returns>
        public static string GetKOSName(string cSharpName)
        {
            string kosName;
            
            if (cSharpToKosMap.TryGetValue(cSharpName,out kosName))
                return kosName;
            else
                return cSharpName;
        }
        /// <summary>
        /// Return the C# type name corresponding to a kOS type name.  Never bombs out, instead
        /// returning the original name as-is if it wasn't found in the lookup.  This is a
        /// case-insensitive lookup because kOS types are case insensitive.
        /// </summary>
        /// <param name="kosName"></param>
        /// <returns></returns>
        public static string GetCSharpName(string kosName)
        {
            string cSharpName;
            
            if (kosToCSharpMap.TryGetValue(kosName,out cSharpName))
                return cSharpName;
            else
                return kosName;
        }
    }
}
