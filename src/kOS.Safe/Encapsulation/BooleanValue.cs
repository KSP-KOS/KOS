using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Reflection;
using System.Globalization;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Boolean")]
    public class BooleanValue : PrimitiveStructure, IConvertible
    {
        private readonly bool internalValue;
        public bool Value { get { return internalValue; } }

        public BooleanValue(bool value)
        {
            internalValue = value;
            InitializeSuffixes();
        }

        private BooleanValue()
        {
            internalValue = false;
            InitializeSuffixes();
        }

        public void InitializeSuffixes()
        {
            // TODO: Add suffixes as needed
        }

        public override object ToPrimitive()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Type compareType = typeof(BooleanValue);
            if (compareType.IsInstanceOfType(obj))
            {
                var val = obj as BooleanValue;
                if (Value == val.Value) return true;
            }
            else
            {
                BindingFlags flags = BindingFlags.ExactBinding | BindingFlags.Static | BindingFlags.Public;
                MethodInfo converter = typeof(BooleanValue).GetMethod("op_Implicit", flags, null, new[] { obj.GetType() }, null);
                if (converter != null)
                {
                    var val = (BooleanValue)converter.Invoke(null, new[] { obj });
                    if (Value == val.Value) return true;
                }
            }
            return false;
        }

        public static BooleanValue True
        {
            get { return new BooleanValue(true);}
        }

        public static BooleanValue False
        {
            get { return new BooleanValue(false);}
        }

        public override int GetHashCode()
        {
            return internalValue.GetHashCode();
        }

        public static BooleanValue operator !(BooleanValue val)
        {
            return new BooleanValue(!val.Value);
        }

        public static bool operator ==(BooleanValue val1, BooleanValue val2)
        {
            Type compareType = typeof(BooleanValue);
            if (compareType.IsInstanceOfType(val1))
            {
                return val1.Equals(val2);
            }
            return !compareType.IsInstanceOfType(val2);
        }

        public static bool operator ==(BooleanValue val1, bool val2)
        {
            return val1 == new BooleanValue(val2);
        }

        public static bool operator ==(bool val1, BooleanValue val2)
        {
            return new BooleanValue(val1) == val2;
        }

        public static bool operator ==(BooleanValue val1, Structure val2)
        {
            return val1 == new BooleanValue(Convert.ToBoolean(val2));
        }

        public static bool operator ==(Structure val1, BooleanValue val2)
        {
            return new BooleanValue(Convert.ToBoolean(val1)) == val2;
        }

        public static bool operator !=(BooleanValue val1, BooleanValue val2)
        {
            return !(val1 == val2);
        }

        public static bool operator !=(BooleanValue val1, bool val2)
        {
            return !(val1 == val2);
        }

        public static bool operator !=(bool val1, BooleanValue val2)
        {
            return !(val1 == val2);
        }

        public static bool operator !=(BooleanValue val1, Structure val2)
        {
            return !(val1 == val2);
        }

        public static bool operator !=(Structure val1, BooleanValue val2)
        {
            return !(val1 == val2);
        }

        public static bool operator &(BooleanValue val1, BooleanValue val2)
        {
            return val1.Value && val2.Value;
        }

        public static bool operator |(BooleanValue val1, BooleanValue val2)
        {
            return val1.Value || val2.Value;
        }

        public static implicit operator bool(BooleanValue val)
        {
            return val.Value;
        }

        public static implicit operator BooleanValue(bool val)
        {
            return new BooleanValue(val);
        }

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return internalValue;
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(byte));
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(char));
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(DateTime));
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(decimal));
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(double));
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(short));
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(int));
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(long));
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(sbyte));
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(float));
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == GetType())
                return this;
            else if (conversionType.IsSubclassOf(typeof(Structure)))
                throw new KOSCastException(typeof(BooleanValue), conversionType);
            return Convert.ChangeType(internalValue, conversionType);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(ushort));
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(uint));
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(ulong));
        }

        public override Dump Dump(DumperState s)
        {
            DumpDictionary dump = new DumpDictionary(typeof(BooleanValue));
            
            dump.Add("value", internalValue);

            return dump;
        }

        [DumpDeserializer(typeof(DumpDictionary))]
        public static BooleanValue CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            return new BooleanValue(d.GetBool("value"));
        }

        [DumpPrinter(typeof(DumpDictionary))]
        public static void Print(DumpDictionary d, IndentedStringBuilder sb)
        {
            sb.Append(d.GetBool("value").ToString(CultureInfo.InvariantCulture));
        }
    }
}