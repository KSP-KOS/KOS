using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Encapsulation
{
    public class BooleanValue : Structure, IConvertible, ISerializableValue
    {
        private bool internalValue;
        
        public bool Value { get { return internalValue; } }

        public BooleanValue(bool value)
            : base()
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
            BooleanValue val = obj as BooleanValue;
            if (val != null)
            {
                if (Value == val.Value) return true;
            }
            else
            {
                var converter = typeof(BooleanValue).GetMethod("op_Implicit", new[] { obj.GetType() });
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
            BooleanValue val1 = obj1 as BooleanValue;
            if (val1 != null)
            {
                return val1.Equals(obj2);
            }
            BooleanValue val2 = obj1 as BooleanValue;
            if (val2 != null)
            {
                return val2.Equals(obj1);
            }
            return val1.Equals(obj2);
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
            val2 = new BooleanValue(Convert.ToBoolean(val1));
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
            val2 = new BooleanValue(Convert.ToBoolean(val1));
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
            throw new KOSCastException(typeof(BooleanValue), typeof(Int16));
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(int));
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(Int64));
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(sbyte));
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(Single));
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
            throw new KOSCastException(typeof(BooleanValue), typeof(UInt16));
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(uint));
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(BooleanValue), typeof(UInt64));
        }
    }
}
