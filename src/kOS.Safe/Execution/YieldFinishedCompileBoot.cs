using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;
using System.Collections.Generic;
using System.Threading;

namespace kOS.Safe.Execution
{
    public class YieldFinishedCompileBoot : YieldFinishedCompile
    {
        protected YieldFinishedCompileBoot(CompileMode mode, GlobalPath scriptPath, int lineNumber, string fileContent, string contextIdentifier, CompilerOptions compilerOptions) :
            base(mode, scriptPath, lineNumber, fileContent, contextIdentifier, compilerOptions)
        { }

        protected override bool ThreadInitialize()
        {
            if (!shared.Processor.CheckCanBoot())
                return false;
            shared.Screen?.Print("Booting ...\n");
            base.ThreadInitialize();
            return true;
        }

        public static new YieldFinishedCompileBoot RunScript(GlobalPath scriptPath, int lineNumber, string fileContent, string contextIdentifier, CompilerOptions compilerOptions) =>
            new YieldFinishedCompileBoot(CompileMode.RUN, scriptPath, lineNumber, fileContent, contextIdentifier, compilerOptions);
    }
}