using System;
using System.Text.RegularExpressions;

namespace kOS.Safe
{
    /// <summary>
    /// String utility functions that avoid the culture-aware overhead of the builtin string functions.
    /// </summary>
    public static class StringUtil
    {
        // The IDENTIFIER Regex Pattern is taken directly from kRISC.tpg - if it changes there, it should change here too.
        // (It's messy to actually use the pattern directly from Scanner.cs because that requires an instance
        // of SharedObjects to get an instance of the compiler.)
        private static Regex identifierPattern = new Regex(@"\G(?:[_\p{L}]\w*)");

        public static bool EndsWith(string str, string suffix)
        {
            int strLen = str.Length;
            int suffixLen = suffix.Length;

            if (strLen < suffixLen)
            {
                return false;
            }

            int iStr = strLen - suffixLen;
            int iSuffix = 0;
            while (iSuffix < suffixLen)
            {
                if (str[iStr] != suffix[iSuffix])
                    return false;
                ++iStr;
                ++iSuffix;
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

        public static bool IsValidIdentifier(string str)
        {
            Match match = identifierPattern.Match(str);

            // Only counts as a valid identifier if the entire string matched without
            // any leftover characters at the end of it:
            if (match.Success && match.Length == str.Length)
                return true;
            return false;
        }
    }
}
