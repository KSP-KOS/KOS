using kOS.Safe.Exceptions;
using System;
using System.Reflection;

namespace kOS.Safe.Encapsulation
{
    public class BooleanValue : Structure, IConvertible, ISerializableValue
    {
        private readonly bool internalValue;

        public bool Value { get { return internalValue; } }

        public BooleanValue(bool value)
        {
            internalValue = value;
            InitializeSuffixes();
        }

        public void InitializeSuffixes()
        {
            // TODO: Add suffixes as needed
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var val = obj as BooleanValue;
            if (val != null)
            {
                if (Value == val.Value) return true;
            }
            else
            {
                BindingFlags flags = BindingFlags.ExactBinding | BindingFlags.Static | BindingFlags.Public;
                MethodInfo converter = typeof(BooleanValue).GetMethod("op_Implicit", flags, null, new[] { obj.GetType() }, null);
                if (converter != null)
                {
                    val = (BooleanValue)converter.Invoke(null, new[] { obj });
                    if (Value == val.Value) return true;
                }
            }
            return false;
        }

        public static bool NullSafeEquals(object obj1, object obj2)
        {
            if (obj1 == null)
            {
                if (obj2 == null) return true;
                return false;
            }
            if (obj2 == null) return false;
            var val1 = obj1 as BooleanValue;
            if (val1 != null)
            {
                return val1.Equals(obj2);
            }
            var val2 = obj2 as BooleanValue;
            if (val2 != null)
            {
                return val2.Equals(obj1);
            }
            return false;
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
            return NullSafeEquals(val1, val2);
        }

        public static bool operator ==(BooleanValue val1, bool val2)
        {
            return NullSafeEquals(val1, new BooleanValue(val2));
        }

        public static bool operator ==(bool val1, BooleanValue val2)
        {
            return NullSafeEquals(new BooleanValue(val1), val2);
        }

        public static bool operator ==(BooleanValue val1, Structure val2)
        {
            val2 = new BooleanValue(Convert.ToBoolean(val2));
            return NullSafeEquals(val1, val2);
        }

        public static bool operator ==(Structure val1, BooleanValue val2)
        {
            val1 = new BooleanValue(Convert.ToBoolean(val1));
            return NullSafeEquals(val1, val2);
        }

        public static bool operator !=(BooleanValue val1, BooleanValue val2)
        {
            return !NullSafeEquals(val1, val2);
        }

        public static bool operator !=(BooleanValue val1, bool val2)
        {
            return !NullSafeEquals(val1, new BooleanValue(val2));
        }

        public static bool operator !=(bool val1, BooleanValue val2)
        {
            return !NullSafeEquals(new BooleanValue(val1), val2);
        }

        public static bool operator !=(BooleanValue val1, Structure val2)
        {
            val2 = new BooleanValue(Convert.ToBoolean(val2));
            return !NullSafeEquals(val1, val2);
        }

        public static bool operator !=(Structure val1, BooleanValue val2)
        {
            if (val2 == null) throw new ArgumentNullException("val2");
            val1 = new BooleanValue(Convert.ToBoolean(val1));
            return !NullSafeEquals(val1, val2);
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
    }
}