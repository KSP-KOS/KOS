namespace kOS.Safe.Encapsulation
{
    public class VersionInfo : Structure
    {
        public double Major;
        public double Minor;

        public VersionInfo(double major, double minor)
        {
            Major = major;
            Minor = minor;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "MAJOR":
                    return Major;
                case "MINOR":
                    return Minor;
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return Major + "." + Minor.ToString("0.0");
        }
    }
}
