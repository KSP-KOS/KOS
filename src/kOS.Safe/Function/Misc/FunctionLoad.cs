using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;
using System.Collections.Generic;

namespace kOS.Safe.Function.Misc
{
    [FunctionAttribute("load")]
    public class FunctionLoad : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            bool defaultOutput = false;
            bool justCompiling = false; // is this load() happening to compile, or to run?
            string fileNameOut = null;
            object topStack = PopValueAssert(shared, true); // null if there's no output file (output file means compile, not run).
            if (topStack != null)
            {
                justCompiling = true;
                string outputArg = topStack.ToString();
                if (outputArg.Equals("-default-compile-out-"))
                    defaultOutput = true;
                else
                    fileNameOut = PersistenceUtilities.CookedFilename(outputArg, Volume.KOS_MACHINELANGUAGE_EXTENSION);
            }

            string fileName = null;
            topStack = PopValueAssert(shared, true);
            if (topStack != null)
                fileName = topStack.ToString();

            AssertArgBottomAndConsume(shared);

            if (fileName == null)
                throw new KOSFileException("No filename to load was given.");

            VolumeFile file = shared.VolumeMgr.CurrentVolume.Open(fileName, !justCompiling); // if running, look for KSM first.  If compiling look for KS first.
            if (file == null) throw new KOSFileException(string.Format("Can't find file '{0}'.", fileName));
            fileName = file.Name; // just in case GetByName picked an extension that changed it.
            FileContent fileContent = file.ReadAll();

            // filename is now guaranteed to have an extension.  To make default output name, replace the extension with KSM:
            if (defaultOutput)
                fileNameOut = fileName.Substring(0, fileName.LastIndexOf('.')) + "." + Volume.KOS_MACHINELANGUAGE_EXTENSION;

            if (fileNameOut != null && fileName == fileNameOut)
                throw new KOSFileException("Input and output filenames must differ.");

            if (shared.VolumeMgr == null) return;
            if (shared.VolumeMgr.CurrentVolume == null) throw new KOSFileException("Volume not found");

            if (shared.ScriptHandler != null)
            {
                shared.Cpu.StartCompileStopwatch();
                var options = new CompilerOptions { LoadProgramsInSameAddressSpace = true, FuncManager = shared.FunctionManager };
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + fileName;
                // add this program to the address space of the parent program,
                // or to a file to save:
                if (justCompiling)
                {
                    List<CodePart> compileParts = shared.ScriptHandler.Compile(filePath, 1, fileContent.String, string.Empty, options);
                    VolumeFile volumeFile = shared.VolumeMgr.CurrentVolume.Save(fileNameOut, new FileContent(compileParts));
                    if (volumeFile == null)
                    {
                        throw new KOSFileException("Can't save compiled file: not enough space or access forbidden");
                    }
                }
                else
                {
                    var programContext = shared.Cpu.SwitchToProgramContext();
                    List<CodePart> parts;
                    if (fileContent.Category == FileCategory.KSM)
                    {
                        string prefix = programContext.Program.Count.ToString();
                        parts = fileContent.AsParts(filePath, prefix);
                    }
                    else
                    {
                        parts = shared.ScriptHandler.Compile(filePath, 1, fileContent.String, "program", options);
                    }
                    int programAddress = programContext.AddObjectParts(parts);
                    // push the entry point address of the new program onto the stack
                    shared.Cpu.PushStack(programAddress);
                }
                shared.Cpu.StopCompileStopwatch();
            }
        }
    }
}