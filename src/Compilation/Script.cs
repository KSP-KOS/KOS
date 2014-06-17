using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace kOS.Compilation
{
    public class Script
    {
        private readonly Dictionary<string, string> identifierReplacements = new Dictionary<string, string> {    { "alt:radar", "alt_radar" },
                                                                                                                 { "alt:apoapsis", "alt_apoapsis" },
                                                                                                                 { "alt:periapsis", "alt_periapsis" },
                                                                                                                 { "eta:apoapsis", "eta_apoapsis" },
                                                                                                                 { "eta:periapsis", "eta_periapsis" },
                                                                                                                 { "eta:transition", "eta_transition" }};
        protected CompileCache Cache { get; set; }

        public Script()
        {
            Cache = CompileCache.GetInstance();
        }

        public virtual List<CodePart> Compile(string scriptText)
        {
            return Compile(scriptText, string.Empty);
        }

        public virtual List<CodePart> Compile(string scriptText, string contextId)
        {
            return Compile(scriptText, contextId, new CompilerOptions());
        }

        public virtual List<CodePart> Compile(string scriptText, string contextId, CompilerOptions options)
        {
            return new List<CodePart>();
        }
        
        public virtual void ClearContext(string contextId) { }

        public virtual bool IsCommandComplete(string command)
        {
            return true;
        }

        protected virtual string MakeLowerCase(string scriptText)
        {
            Dictionary<string, string> stringsLiterals = ExtractStrings(scriptText);
            string modifiedScriptText = scriptText;

            if (stringsLiterals.Count > 0)
            {
                // replace strings with tokens
                modifiedScriptText = stringsLiterals.Aggregate(modifiedScriptText, (current, kvp) => current.Replace(kvp.Value, kvp.Key));

                // make lowercase
                modifiedScriptText = modifiedScriptText.ToLower();

                // restore strings
                modifiedScriptText = stringsLiterals.Aggregate(modifiedScriptText, (current, kvp) => current.Replace(kvp.Key, kvp.Value));
            }
            else
            {
                // make lowercase
                modifiedScriptText = modifiedScriptText.ToLower();
            }

            return modifiedScriptText;
        }

        private Dictionary<string, string> ExtractStrings(string scriptText)
        {
            var stringsLiterals = new Dictionary<string, string>();
            int stringIndex = 0;
            
            var regex = new Regex("\".+?\"");
            MatchCollection matches = regex.Matches(scriptText);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string token = string.Format("[s{0}]", ++stringIndex);
                    stringsLiterals.Add(token, match.Value);
                }
            }

            return stringsLiterals;
        }

        protected virtual void RaiseParseException(string scriptText, int line, int absolutePosition)
        {
            const int LINE_SIZE = 50;
            int minStartIndex = Math.Max(absolutePosition - 40, 0);
            int maxEndIndex = scriptText.Length - 1;

            int startIndex = scriptText.LastIndexOf('\n', Math.Max(absolutePosition - 1, 0)) + 1;
            if (startIndex < minStartIndex) startIndex = minStartIndex;
            int endIndex = scriptText.IndexOf('\n', absolutePosition);
            if (endIndex == -1 || endIndex - startIndex > LINE_SIZE) endIndex = startIndex + LINE_SIZE;
            if (endIndex > maxEndIndex) endIndex = maxEndIndex;
            string errorScript = scriptText.Substring(startIndex, (endIndex - startIndex) + 1);

            var parseMessage = new StringBuilder();
            parseMessage.AppendLine(string.Format("Syntax error at line {0}", line));
            parseMessage.AppendLine(errorScript);
            parseMessage.AppendLine(new string(' ', absolutePosition - startIndex) + "^");

            throw new Exception(parseMessage.ToString());
        }

        protected virtual string ReplaceIdentifiers(string scriptText)
        {
            return identifierReplacements.Aggregate(scriptText, (current, kvp) => current.Replace(kvp.Key, kvp.Value));
        }
    }
}
