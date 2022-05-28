using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public interface ISuffixed
    {
        bool SetSuffix(string suffixName, object value, bool failOkay = false);
        ISuffixResult GetSuffix(string suffixName, bool failOkay = false);
    }
}