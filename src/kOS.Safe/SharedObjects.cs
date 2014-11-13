using kOS.Safe.Compilation;
using kOS.Safe.Execution;
using kOS.Safe.Function;
using kOS.Safe.Module;
using kOS.Safe.Persistence;
using kOS.Safe.Screen;

namespace kOS.Safe
{
    public class SharedObjects
    {
        public ICpu Cpu { get; set; }
        public IScreenBuffer Screen { get; set; }
        public IInterpreter Interpreter { get; set; }
        public Script ScriptHandler { get; set; }
        public ILogger Logger { get; set; }
        public IProcessor Processor { get; set; }
        public UpdateHandler UpdateHandler { get; set; }
        public IFunctionManager FunctionManager { get; set; }
        public VolumeManager VolumeMgr { get; set; }
    }
}