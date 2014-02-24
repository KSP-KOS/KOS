using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Suffixed
{
    public class VersionInfo : SpecialValue
    {
        public double Major;
        public double Minor;

        public VersionInfo(double major, double minor)
        {
            this.Major = major;
            this.Minor = minor;
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "MAJOR") return Major;
            if (suffixName == "MINOR") return Minor;

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return Major.ToString() + "." + Minor.ToString("0.0");
        }
    }
}
