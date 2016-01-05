using System;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public class ConstantValue : Structure
    {
        /// <summary>
        /// kiloPascals to atmospheres
        /// </summary>
        public const double KpaToAtm = 0.00986923266716012830002467308167;

        static ConstantValue()
        {
            AddGlobalSuffix<ConstantValue>("G", new StaticSuffix<double>(() => 6.67384*Math.Pow(10,-11)));
            AddGlobalSuffix<ConstantValue>("E", new StaticSuffix<double>(() => Math.E));
            AddGlobalSuffix<ConstantValue>("PI", new StaticSuffix<double>(() => Math.PI));
            AddGlobalSuffix<ConstantValue>("C", new StaticSuffix<double>(() => 299792458.0, "Speed of light in m/s")); 
            AddGlobalSuffix<ConstantValue>("ATMTOKPA", new StaticSuffix<double>(() => 101.325, "atmospheres to kiloPascals" ));
            AddGlobalSuffix<ConstantValue>("KPATOATM", new StaticSuffix<double>(() => KpaToAtm, "kiloPascals to atmospheres"));

            // pi/180 :
            AddGlobalSuffix<ConstantValue>("DEGTORAD", new StaticSuffix<double>(() => 0.01745329251994329576923690768489, "degrees to radians"));
            
            // 180/pi :
            AddGlobalSuffix<ConstantValue>("RADTODEG", new StaticSuffix<double>(() => 57.295779513082320876798154814105, "radians to degrees"));
        }

        public override string ToString()
        {
            return string.Format("{0} Constants", base.ToString());
        }
    }

}