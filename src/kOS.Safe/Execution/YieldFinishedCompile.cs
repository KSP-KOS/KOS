using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;
using System.Collections.Generic;
using System.Threading;

namespace kOS.Safe.Execution
{
    public class YieldFinishedCompile : YiedFinishedThreadedDetector
    {
        private enum CompileMode
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

        private YieldFinishedCompile(GlobalPath scriptPath, int lineNumber, string fileContent, string contextIdentifier, CompilerOptions compilerOptions)
        {
            compileMode = CompileMode.RUN;
            path = scriptPath;
            startLineNum = lineNumber;
            content = fileContent;
            contextId = contextIdentifier;
            options = compilerOptions;
        }

        public override void ThreadInitialize(SafeSharedObjects shared)
        {
            if (compileMode != CompileMode.FILE)
                programContext = shared.Cpu.SwitchToProgramContext(); // only switch the context if executing
            codeParts = new List<CodePart>();
        }

        public override void ThreadExecute()
        {
            codeParts = shared.ScriptHandler.Compile(path, startLineNum, content, contextId, options);
        }

        public override void ThreadFinish()
        {
            switch (compileMode)
            {
                case CompileMode.RUN:
                    programContext.AddParts(codeParts);
                    shared.Cpu.StopCompileStopwatch();
                    break;
                case CompileMode.LOAD:
                    int programAddress = programContext.AddObjectParts(codeParts, path.ToString());
                    // push the entry point address of the new program onto the stack
                    shared.Cpu.PushStack(programAddress);
                    shared.Cpu.PushStack(BooleanValue.False);
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
        }

        public static YieldFinishedCompile RunScript(GlobalPath scriptPath, int lineNumber, string fileContent, string contextIdentifier, CompilerOptions compilerOptions)
        {
            var ret = new YieldFinishedCompile(scriptPath, lineNumber, fileContent, contextIdentifier, compilerOptions);
            ret.compileMode = CompileMode.RUN;
            return ret;
        }

        public static YieldFinishedCompile LoadScript(GlobalPath scriptPath, int lineNumber, string fileContent, string contextIdentifier, CompilerOptions compilerOptions)
        {
            var ret = new YieldFinishedCompile(scriptPath, lineNumber, fileContent, contextIdentifier, compilerOptions);
            ret.compileMode = CompileMode.LOAD;
            return ret;
        }

        public static YieldFinishedCompile CompileScriptToFile(GlobalPath scriptPath, int lineNumber, string fileContent, CompilerOptions compilerOptions, Volume storageVolume, GlobalPath storagePath)
        {
            var ret = new YieldFinishedCompile(scriptPath, lineNumber, fileContent, string.Empty, compilerOptions);
            ret.compileMode = CompileMode.FILE;
            ret.volume = storageVolume;
            ret.outPath = storagePath;
            return ret;
        }
    }
}