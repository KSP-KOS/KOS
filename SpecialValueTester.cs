using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class SpecialValueTester : SpecialValue
    {
        CPU cpu;

        public SpecialValueTester(CPU cpu)
        {
            this.cpu = cpu;
        }

        public override string ToString()
        {
            return "3";
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "A") return cpu.SessionTime;

            return base.GetSuffix(suffixName);
        }
    }
}
