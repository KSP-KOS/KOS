using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace kOS
{
    public class CompileCache
    {
        private static CompileCache _instance = null;
        private Dictionary<string, List<CodePart>> _cache;

        private CompileCache()
        {
            _cache = new Dictionary<string, List<CodePart>>();
        }

        public static CompileCache GetInstance()
        {
            if (_instance == null) _instance = new CompileCache();
            return _instance;
        }

        public bool ExistsInCache(string scriptText)
        {
            string scriptHash = CalculateMD5Hash(scriptText);
            return _cache.ContainsKey(scriptHash);
        }

        public List<CodePart> GetFromCache(string scriptText)
        {
            string scriptHash = CalculateMD5Hash(scriptText);
            if (_cache.ContainsKey(scriptHash))
            {
                return _cache[scriptHash];
            }
            else
            {
                return null;
            }
        }

        public void AddToCache(string scriptText, List<CodePart> code)
        {
            string scriptHash = CalculateMD5Hash(scriptText);
            if (!_cache.ContainsKey(scriptHash))
            {
                _cache.Add(scriptHash, code);
            }
        }

        private string CalculateMD5Hash(string input)
        {
            // calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
