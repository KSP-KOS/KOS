using System.Collections.Generic;

namespace kOS.Safe.Compilation
{
    public abstract class Script
    {
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
    }
}