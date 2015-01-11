using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Encapsulation
{
    public class ConstantValue : Structure
    {
        static ConstantValue()
        {
            AddGlobalSuffix<ConstantValue>("G", new StaticSuffix<double>(() => 6.67384*Math.Pow(10,-11)));
            AddGlobalSuffix<ConstantValue>("E", new StaticSuffix<double>(() => Math.E));
            AddGlobalSuffix<ConstantValue>("e", new StaticSuffix<double>(() => Math.E));
            AddGlobalSuffix<ConstantValue>("PI", new StaticSuffix<double>(() => Math.PI));
        }

        public override string ToString()
        {
            return string.Format("{0} Constants", base.ToString());
        }
        
        public override bool KOSEquals(object other)
        {
            throw new KOSBinaryOperandTypeException(this.GetType(),"=","and",other.GetType());
        }
    }

}