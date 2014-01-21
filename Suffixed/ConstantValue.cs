using System;

namespace kOS.Suffixed
{
    public class ConstantValue : SpecialValue
    {
        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "G":
                    return 6.67384 * (10^-11);
                case "E":
                case "e":
                    return Math.E;
                case "PI":
                    return Math.PI;
            }
            return base.GetSuffix(suffixName);
        }
    }
}