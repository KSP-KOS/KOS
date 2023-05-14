using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;
using System.Collections.Generic;
using System.Threading;

namespace kOS.Safe.Execution
{
    public class YieldFinishedCompile : YieldFinishedThreadedDetector
    {
        protected enum CompileMode
        {
            RUN = 0,
            LOAD = 1,
            FILE = 2
        }

        private CompileMode compileMode;

        private List<CodePart> codeParts;
        private GlobalPath path;
        private int startLineNum;
        private string content;
        private string contextId;
        private CompilerOptions options;
        private Volume volume;
        private GlobalPath outPath;

        private IProgramContext programContext;

        protected YieldFinishedCompile(CompileMode mode, GlobalPath scriptPath, int lineNumber, string fileContent, string contextIdentifier, CompilerOptions compilerOptions)
        {
            compileMode = mode;
            path = scriptPath;
            startLineNum = lineNumber;
            content = fileContent;
            contextId = contextIdentifier;
            options = compilerOptions;
        }

        protected override bool ThreadInitialize()
        {
            if (compileMode != CompileMode.FILE)
                programContext = shared.Cpu.SwitchToProgramContext(); // only switch the context if executing
            codeParts = new List<CodePart>();
            return true;
        }

        protected override void ThreadExecute()
        {
            codeParts = shared.ScriptHandler.Compile(path, startLineNum, content, contextId, options);
        }

        protected override void ThreadFinish()
        {
            switch (compileMode)
            {
                case CompileMode.RUN:
                    programContext.AddParts(codeParts);
                    break;
                case CompileMode.LOAD:
                    int programAddress = programContext.AddObjectParts(codeParts, path.ToString());
                    // push the entry point address of the new program onto the stack
                    shared.Cpu.PushArgumentStack(programAddress);
                    shared.Cpu.PushArgumentStack(BooleanValue.False);
                    break;
                case CompileMode.FILE:
                    VolumeFile written = volume.SaveFile(outPath, new FileContent(codeParts));
                    if (written == null)
                    {
                        throw new KOSFileException("Can't save compiled file: not enough space or access forbidden");
                    }
                    break;
                default:
                    break;
            }
            shared.Cpu.StopCompileStopwatch();
        }

        public static YieldFinishedCompile RunScript(GlobalPath scriptPath, int lineNumber, string fileContent, string contextIdentifier, CompilerOptions compilerOptions) =>
            new YieldFinishedCompile(CompileMode.RUN, scriptPath, lineNumber, fileContent, contextIdentifier, compilerOptions);

        public static YieldFinishedCompile LoadScript(GlobalPath scriptPath, int lineNumber, string fileContent, string contextIdentifier, CompilerOptions compilerOptions) =>
            new YieldFinishedCompile(CompileMode.LOAD, scriptPath, lineNumber, fileContent, contextIdentifier, compilerOptions);

        public static YieldFinishedCompile CompileScriptToFile(GlobalPath scriptPath, int lineNumber, string fileContent, CompilerOptions compilerOptions, Volume storageVolume, GlobalPath storagePath) =>
            new YieldFinishedCompile(CompileMode.FILE, scriptPath, lineNumber, fileContent, string.Empty, compilerOptions)
            {
                volume = storageVolume,
                outPath = storagePath
            };
    }
}