using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public interface ISuffixed
    {
        bool SetSuffix(string suffixName, object value);
        ISuffixResult GetSuffix(string suffixName);
    }
}