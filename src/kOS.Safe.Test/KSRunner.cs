using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kOS.Safe.Binding;
using kOS.Safe.Compilation;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using kOS.Safe.Function;
using kOS.Safe.Persistence;
using kOS.Safe.Test.Execution;
using kOS.Safe.Utilities;

namespace kOS.Safe.Test
{
    class KSRunner
    {
        [AssemblyWalk(AttributeType = typeof(BindingAttribute), InherritedType = typeof(SafeBindingBase), StaticRegisterMethod = "RegisterMethod")]
        class BindingManager : BaseBindingManager
        {
            public BindingManager(SafeSharedObjects s) : base(s) { }
        }

        public string KerboScript { get; private set; }
        public Harddisk Volume { get; private set; }

        public KSRunner(string kerboscript)
        {
            KerboScript = kerboscript;
            Run();
        }

        private string WrappedScript
        {
            get
            {
                return "print \"======DELIMITER======\".\n" + 
                    "local _f to { " + KerboScript + " }.\n" + 
                    "set TestResult to _f().";
            }
        }

        public string Output
        {
            get
            {
                return string.Join("======DELIMITER======", _screen.ToString().Split(new string[] { "======DELIMITER======" }, StringSplitOptions.None).Skip(1));
            }
        }

        public Encapsulation.Structure Result
        {
            get
            {
                return _result as Encapsulation.Structure;
            }
        }

        private void Run()
        {
            if (!_hasWalked)
            {
                if (SafeHouse.Logger == null)
                    SafeHouse.Logger = new NoopLogger();
                if (SafeHouse.Config == null)
                    SafeHouse.Init(new Config(), new VersionInfo(0, 0, 0, 0), "https://example.org", Environment.NewLine == "\r\n", "/dev/null");
                AssemblyWalkAttribute.Walk();
                _hasWalked = true;
            }

            var shared = new SafeSharedObjects();
            shared.BindingMgr = new BindingManager(shared);
            shared.FunctionManager = new FunctionManager(shared);
            shared.GameEventDispatchManager = new NoopGameEventDispatchManager();
            shared.Processor = new NoopProcessor();
            shared.ScriptHandler = new KSScript();
            _screen = new Screen();
            shared.Screen = _screen;
            shared.UpdateHandler = new UpdateHandler();
            shared.VolumeMgr = new VolumeManager();

            shared.FunctionManager.Load();

            Volume = new Harddisk(640*1024); // 640K ought to be enough for anybody
            shared.VolumeMgr.Add(Volume);
            shared.VolumeMgr.SwitchTo(Volume);

            var cpu = new CPU(shared);
            shared.Cpu = cpu;
            cpu.ShouldSwallowExceptions = false;

            shared.Cpu.Boot();

            bool finished = false;
            shared.BindingMgr.AddSetter("TESTRESULT", (s) => {
                finished = true;
                _result = s;
            });
            
            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject("0:/test");
            var compiled = shared.ScriptHandler.Compile(path, 1, WrappedScript, "test", new CompilerOptions()
            {
                LoadProgramsInSameAddressSpace = false,
                IsCalledFromRun = false,
                FuncManager = shared.FunctionManager
            });
            shared.Cpu.GetCurrentContext().AddParts(compiled);

            for(int i = 0; i < 100; i++)
            {
                shared.UpdateHandler.UpdateFixedObservers(0.01);

                if (finished)
                    return;
            }

            throw new TimeoutException("Execution did not finish in time.\n\nScreen output:" + Output);
        }

        private static bool _hasWalked = false;
        private object _result = null;
        private Screen _screen = null;
    }
}
