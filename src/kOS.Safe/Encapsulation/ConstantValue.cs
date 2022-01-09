using System;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Constant")]
    public class ConstantValue : Structure
    {
        /// <summary>
        /// kiloPascals to atmospheres
        /// </summary>
        public const double KpaToAtm = 0.00986923266716012830002467308167;

        // The following will be the default fallback value to use for G in case we can't
        // access the KSP API for it, such as if we ever implement running kerboscript
        // outside the game:
        private static double gConstFromNIST_in2014 = 6.67408 * Math.Pow(10, -11);
        private static double gravConstBeingUsed = gConstFromNIST_in2014;

        /// <summary>
        /// This is a public property so that it can be overridden by KSP-aware code elsewhere:
        /// (ConstantValue is in kOS.Safe, so we can't see KSP's G value from here.)
        /// </summary>
        public static double GravConst
        {
            get { return gravConstBeingUsed; }
            set { gravConstBeingUsed = value; }
        }

        private static double avogadroFromWIKI = 6.02214076 * Math.Pow(10, 23);
        private static double avogadroBeingUsed = avogadroFromWIKI;

        /// <summary>
        /// This is a public property so that it can be overridden by KSP-aware code elsewhere:
        /// (ConstantValue is in kOS.Safe, so we can't see KSP's G value from here.)
        /// </summary>
        public static double AvogadroConst
        {
            get { return avogadroBeingUsed; }
            set { avogadroBeingUsed = value; }
        }

        private static double boltzmannFromWiki = 1.380649 * Math.Pow(10, -23);
        private static double boltzmannBeingUsed = boltzmannFromWiki;

        /// <summary>
        /// This is a public property so that it can be overridden by KSP-aware code elsewhere:
        /// (ConstantValue is in kOS.Safe, so we can't see KSP's G value from here.)
        /// </summary>
        public static double BoltzmannConst
        {
            get { return boltzmannBeingUsed; }
            set { boltzmannBeingUsed = value; }
        }

        private static double idealGasFromWiki = 8.31446215324;
        private static double idealGasBeingUsed = idealGasFromWiki;

        /// <summary>
        /// This is a public property so that it can be overridden by KSP-aware code elsewhere:
        /// (ConstantValue is in kOS.Safe, so we can't see KSP's G value from here.)
        /// </summary>
        public static double IdealGasConst
        {
            get { return idealGasBeingUsed; }
            set { idealGasBeingUsed = value; }
        }

        private static double g0 = 9.80665; // Typically accepted Earth value. Will override with KSP game value.
        public static double G0
        {
            get { return g0; }
            set { g0 = value; }
        }

        static ConstantValue()
        {
            AddGlobalSuffix<ConstantValue>("G", new StaticSuffix<ScalarValue>(() => GravConst));
            AddGlobalSuffix<ConstantValue>("G0", new StaticSuffix<ScalarValue>(() => G0));
            AddGlobalSuffix<ConstantValue>("E", new StaticSuffix<ScalarValue>(() => Math.E));
            AddGlobalSuffix<ConstantValue>("PI", new StaticSuffix<ScalarValue>(() => Math.PI));
            AddGlobalSuffix<ConstantValue>("C", new StaticSuffix<ScalarValue>(() => 299792458.0, "Speed of light in m/s")); 
            AddGlobalSuffix<ConstantValue>("ATMTOKPA", new StaticSuffix<ScalarValue>(() => 101.325, "atmospheres to kiloPascals" ));
            AddGlobalSuffix<ConstantValue>("KPATOATM", new StaticSuffix<ScalarValue>(() => KpaToAtm, "kiloPascals to atmospheres"));

            // pi/180 :
            AddGlobalSuffix<ConstantValue>("DEGTORAD", new StaticSuffix<ScalarValue>(() => 0.01745329251994329576923690768489, "degrees to radians"));
            
            // 180/pi :
            AddGlobalSuffix<ConstantValue>("RADTODEG", new StaticSuffix<ScalarValue>(() => 57.295779513082320876798154814105, "radians to degrees"));

            AddGlobalSuffix<ConstantValue>("AVOGADRO", new StaticSuffix<ScalarValue>(() => AvogadroConst));
            AddGlobalSuffix<ConstantValue>("BOLTZMANN", new StaticSuffix<ScalarValue>(() => BoltzmannConst));
            AddGlobalSuffix<ConstantValue>("IDEALGAS", new StaticSuffix<ScalarValue>(() => IdealGasConst));
        }
    }

}