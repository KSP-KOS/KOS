using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace kOS.Compilation
{
    public class CompileCache
    {
        private static CompileCache instance;
        private readonly Dictionary<string, List<CodePart>> cache;

        private CompileCache()
        {
            cache = new Dictionary<string, List<CodePart>>();
        }

        public static CompileCache GetInstance()
        {
            return instance ?? (instance = new CompileCache());
        }

        public bool ExistsInCache(string scriptText)
        {
            string scriptHash = CalculateMD5Hash(scriptText);
            return cache.ContainsKey(scriptHash);
        }

        public List<CodePart> GetFromCache(string scriptText)
        {
            string scriptHash = CalculateMD5Hash(scriptText);
            return cache.ContainsKey(scriptHash) ? cache[scriptHash] : null;
        }

        public void AddToCache(string scriptText, List<CodePart> code)
        {
            string scriptHash = CalculateMD5Hash(scriptText);
            if (!cache.ContainsKey(scriptHash))
            {
                cache.Add(scriptHash, code);
            }
        }

        private string CalculateMD5Hash(string input)
        {
            // calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // convert byte array to hex string
            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
