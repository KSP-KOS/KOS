using kOS.Context;

namespace kOS.Value
{
    public class SpecialValueTester : SpecialValue
    {
        readonly ICPU cpu;

        public SpecialValueTester(ICPU cpu)
        {
            this.cpu = cpu;
        }

        public override string ToString()
        {
            return "3";
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "A":
                    return cpu.SessionTime;
            }

            return base.GetSuffix(suffixName);
        }
    }
}
