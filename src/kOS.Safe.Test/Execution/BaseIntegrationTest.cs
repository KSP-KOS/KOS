using kOS.Safe.Compilation;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using kOS.Safe.Function;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using NUnit.Framework;
using System;
using System.IO;

namespace kOS.Safe.Test.Execution
{
    [SetUpFixture]
    public class StaticSetup
    {
        [SetUp]
        public void Setup()
        {
            SafeHouse.Init(new Config(), new VersionInfo(0, 0, 0, 0), "", false, "./");
            SafeHouse.Logger = new NoopLogger();

            try
            {
                AssemblyWalkAttribute.Walk();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }
    }

    public abstract class BaseIntegrationTest
    {
        private ICpu cpu;
        private SafeSharedObjects shared;
        private Screen screen;
        private string baseDir;

        private string FindKerboscriptTests()
        {
            var currentDir = Directory.GetCurrentDirectory();
            while (!Directory.Exists(Path.Combine(currentDir, "kerboscript_tests")))
            {
                currentDir = Directory.GetParent(currentDir).FullName;
            }

            return Path.Combine(currentDir, "kerboscript_tests");
        }

        [SetUp]
        public void Setup()
        {
            baseDir = FindKerboscriptTests();

            screen = new Screen();

            shared = new SafeSharedObjects();
            shared.FunctionManager = new FunctionManager(shared);
            shared.GameEventDispatchManager = new NoopGameEventDispatchManager();
            shared.Processor = new NoopProcessor();
            shared.ScriptHandler = new KSScript();
            shared.Screen = screen;
            shared.UpdateHandler = new UpdateHandler();
            shared.VolumeMgr = new VolumeManager();

            shared.FunctionManager.Load();

            Archive archive = new Archive(baseDir);
            shared.VolumeMgr.Add(archive);
            shared.VolumeMgr.SwitchTo(archive);

            cpu = new CPU(shared);
        }

        protected void RunScript(string fileName)
        {
            string contents = File.ReadAllText(Path.Combine(baseDir, fileName));
            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject("0:/" + fileName);
            var compiled = shared.ScriptHandler.Compile(path, 1, contents, "test", new CompilerOptions()
            {
                LoadProgramsInSameAddressSpace = false,
                IsCalledFromRun = false,
                FuncManager = shared.FunctionManager,
                BindManager = shared.BindingMgr,
                AllowClobberBuiltins = SafeHouse.Config.AllowClobberBuiltIns
            });
            cpu.Boot();

            screen.ClearOutput();

            cpu.GetCurrentContext().AddParts(compiled);
        }

        protected void RunSingleStep()
        {
            shared.UpdateHandler.UpdateFixedObservers(0.01);

            if (cpu.InstructionsThisUpdate == SafeHouse.Config.InstructionsPerUpdate)
            {
                throw new Exception("Script did not finish");
            }
        }

        protected void AssertOutput(params string[] expected)
        {
            screen.AssertOutput(expected);
        }
    }
}