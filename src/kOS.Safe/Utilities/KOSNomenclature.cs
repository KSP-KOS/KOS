using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;

namespace kOS.Safe.Utilities
{
    /// <summary>
    /// A class for the purpose of holding the mapping information that goes
    /// back and forth between C# type names and thier KOS type name equivalents.
    /// 
    /// Any time you add a new class derived from kOS.Safe.Encapsulated.Structure, you
    /// should check to see if you should add a mapping to this class to go with it.
    /// 
    /// This class is intended to be used "statically" without an actual instance of it
    /// being necessary.  There should be only one copy of its data, globally, across
    /// the entire process.
    /// </summary>
    [AssemblyWalk(InherritedType = typeof(Structure), StaticRegisterMethod = "PopulateType")]
    public class KOSNomenclature
    {
        private static Dictionary<string,Type> kosToCSharpMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<Type,string> cSharpToKosMap = new Dictionary<Type, string>();

        private static string NagHeader =
            "***********************************************\n" +
            "* The following C# types need a KOS name      *\n" +
            "* mapping defined in the C# code by use of a  *\n" +
            "* KOSNomenclatureAttribute.  If you see this  *\n" +
            "* message it means a KOS developer added a    *\n" +
            "* new type without finishing the job.         *\n" +
            "* If you _ARE_ a kOS developer and see this   *\n" +
            "* message after you've just added a new class *\n" +
            "* then you need to fix this.                  *\n" +
            "***********************************************\n";

        private static bool ShowNagHeader = true;

        // Neither public nor private because a static constructore can't be called explicitly anyway.
        static KOSNomenclature()
        {
        }

        /// <summary>
        /// Populate the nomenclature dictionaries based on the given type, which inherits
        /// from Structure.  This method is automatically called by the AssemblyWalkAttribute
        /// </summary>
        /// <param name="t">A type inheriting from Structure</param>
        public static void PopulateType(Type t)
        {
            object[] attribs = t.GetCustomAttributes(typeof(KOSNomenclatureAttribute), false);
            foreach (object obj in attribs)
            {
                KOSNomenclatureAttribute attrib = obj as KOSNomenclatureAttribute;
                if (attrib == null)
                    continue; // hypothetically impossible since GetCustomAttributes explicitly asked for only attributes of this type.

                try
                {
                    if (attrib.CSharpToKOS)
                        cSharpToKosMap.Add(t, attrib.KOSName);
                }
                catch (ArgumentException)
                {
                    // There can be a many-to-one map (given two different C# types, they both return the same KOS type), but
                    // not a one-to-many map (given one C# type, it has two kOS types it tries to return).
                    string msg = "kOS developer error: name clash in KOSNomenclature: two mappings from C# class " + t.FullName + " found.";
                    Debug.AddNagMessage(Debug.NagType.NAGFOREVER, msg);
                }

                try
                {
                    if (attrib.KOSToCSharp)
                        kosToCSharpMap.Add(attrib.KOSName, t);
                }
                catch (ArgumentException)
                {
                    // There can be a many-to-one map (given two different kos types, they both return the same C# type), but
                    // not a one-to-many map (given one kos type, it has two C# types it tries to return).
                    string msg = "kOS developer error: name clash in KOSNomenclature: two mappings from KOS name " + attrib.KOSName + " found.";
                    Debug.AddNagMessage(Debug.NagType.NAGFOREVER, msg);
                }
            }
            NagCheck(t);
        }

        /// <summary>
        /// Report nag message on terminal if there is a C# type derived from kOS.Safe.Encapsulated.Structure
        /// which was not given a KOSNomenclatureAttribute to work from.  All Structure derivatives will need
        /// to be given at least one KOS name by being given such an attribute.
        /// </summary>
        private static void NagCheck(Type t)
        {
            StringBuilder message = new StringBuilder();
            if (!cSharpToKosMap.ContainsKey(t))
            {
                if (ShowNagHeader)
                {
                    // show the nag header only once, with the first type.
                    message.Append(NagHeader);
                    ShowNagHeader = false;
                }
                else
                    message.Append("\n");

                message.Append("\"" + t.FullName + "\"");
            }
            if (message.Length > 0)
            {
                SafeHouse.Logger.Log(message.ToString());
                Debug.AddNagMessage(Debug.NagType.NAGFOREVER, message.ToString());
            }
        }

        /// <summary>
        /// Return the kOS type name corresponding to a C# type.  Never bombs out, instead
        /// returning the original type's name as-is if it wasn't found in the lookup.  This is a
        /// case-sensitive lookup because C# type names are case sensitive and there could
        /// hypothetically be two different type names that differ by case only.
        /// </summary>
        /// <param name="cSharpType"></param>
        /// <returns></returns>
        public static string GetKOSName(Type cSharpType)
        {
            string kosName;
            
            if (cSharpToKosMap.TryGetValue(cSharpType,out kosName))
                return kosName;
            else
                return cSharpType.Name;
        }

        /// <summary>
        /// RReturns true if the cSharp type has a mapping in the table.
        /// </summary>
        /// <param name="cSharpType"></param>
        /// <returns></returns>
        public static bool HasKOSName(Type cSharpType)
        {
            if (cSharpToKosMap.ContainsKey(cSharpType))
                return true;
            else
                return false;
        }
        
        /// <summary>
        /// Return the C# type corresponding to a kOS type name.
        /// </summary>
        /// <param name="kosName"></param>
        /// <returns></returns>
        public static Type GetCSharpName(string kosName)
        {
            Type cSharpType;
            
            if (kosToCSharpMap.TryGetValue(kosName,out cSharpType))
                return cSharpType;
            else
                throw new KOSException("Not a known kos type name: " + kosName);
        }
    }
}
