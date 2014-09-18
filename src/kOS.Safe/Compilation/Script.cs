using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace kOS.Safe.Compilation
{
    public abstract class Script
    {
        private readonly Dictionary<string, string> identifierReplacements = new Dictionary<string, string> {    { "alt:radar", "alt_radar" },
                                                                                                                 { "alt:apoapsis", "alt_apoapsis" },
                                                                                                                 { "alt:periapsis", "alt_periapsis" },
                                                                                                                 { "eta:apoapsis", "eta_apoapsis" },
                                                                                                                 { "eta:periapsis", "eta_periapsis" },
                                                                                                                 { "eta:transition", "eta_transition" }};

        protected CompileCache Cache { get; set; }

        protected Script()
        {
            Cache = CompileCache.GetInstance();
        }

        /// <summary>
        /// Compile source text into compiled codeparts.
        /// </summary>
        /// <param name="filePath">The name that should get reported to the user on
        /// runtime errors in this compiled code. Even if the text is not from an
        /// actual file this should still be a pseudo-filename for reporting, for
        /// example "(commandline)" or "(socket stream)"
        /// </param>
        /// <param name="startLineNum">Assuming scriptText is a subset of some bigger buffer, line 1 of scripttext
        /// corresponds to line (what) of the more global something, for reporting numbers on errors.</param>
        /// <param name="scriptText">The text to be compiled.</param>
        /// <returns>The CodeParts made from the scriptText</returns>
        public virtual List<CodePart> Compile(string filePath, int startLineNum, string scriptText)
        {
            return Compile(filePath, startLineNum, scriptText, string.Empty);
        }

        /// <summary>
        /// Compile source text into compiled codeparts.
        /// </summary>
        /// <param name="filePath">The name that should get reported to the user on
        /// runtime errors in this compiled code. Even if the text is not from an
        /// actual file this should still be a pseudo-filename for reporting, for
        /// example "(commandline)" or "(socket stream)"
        /// </param>
        /// <param name="startLineNum">Assuming scriptText is a subset of some bigger buffer, line 1 of scripttext
        /// corresponds to line (what) of the more global something, for reporting numbers on errors.</param>
        /// <param name="scriptText">The text to be compiled.</param>
        /// <param name="contextId">The name of the runtime context (i.e. "interpreter").</param>
        /// <returns>The CodeParts made from the scriptText</returns>
        public virtual List<CodePart> Compile(string filePath, int startLineNum, string scriptText, string contextId)
        {
            return Compile(filePath, startLineNum, scriptText, contextId, new CompilerOptions());
        }

        /// <summary>
        /// Compile source text into compiled codeparts.
        /// </summary>
        /// <param name="filePath">The name that should get reported to the user on
        /// runtime errors in this compiled code. Even if the text is not from an
        /// actual file this should still be a pseudo-filename for reporting, for
        /// example "(commandline)" or "(socket stream)"
        /// </param>
        /// <param name="startLineNum">Assuming scriptText is a subset of some bigger buffer, line 1 of scripttext
        /// corresponds to line (what) of the more global something, for reporting numbers on errors.</param>
        /// <param name="scriptText">The text to be compiled.</param>
        /// <param name="contextId">The name of the runtime context (i.e. "interpreter").</param>
        /// <param name="options">settings for the compile</param>
        /// <returns>The CodeParts made from the scriptText</returns>
        public abstract List<CodePart> Compile(string filePath, int startLineNum, string scriptText, string contextId, CompilerOptions options);

        public abstract void ClearContext(string contextId);

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

    }
}