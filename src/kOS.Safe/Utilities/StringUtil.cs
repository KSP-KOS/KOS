using System;
namespace kOS.Safe
{
    /// <summary>
    /// String utility functions that avoid the culture-aware overhead of the builtin string functions.
    /// </summary>
    public static class StringUtil
    {
        public static bool EndsWith(string str, string suffix)
        {
            int strLen = str.Length;
            int suffixLen = suffix.Length;

            if (strLen < suffixLen)
            {
                return false;
            }

            for (int i = 0; i < suffixLen; i++)
            {
                if (str[strLen - i - 1] != suffix[suffixLen - i - 1])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool StartsWith(string str, string prefix)
        {
            int strLen = str.Length;
            int prefixLen = prefix.Length;

            if (strLen < prefixLen)
            {
                return false;
            }

            for (int i = 0; i < prefixLen; i++)
            {
                if (str[i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
