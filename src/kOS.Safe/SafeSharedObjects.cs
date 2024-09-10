using kOS.Safe.Binding;
using kOS.Safe.Callback;
using kOS.Safe.Compilation;
using kOS.Safe.Execution;
using kOS.Safe.Function;
using kOS.Safe.Lua;
using kOS.Safe.Module;
using kOS.Safe.Persistence;
using kOS.Safe.Screen;
using kOS.Safe.Sound;

namespace kOS.Safe
{
    public class SafeSharedObjects
    {
        public ICpu Cpu { get; set; }
        public IInterpreterLink InterpreterLink { get; set; }
        public IScreenBuffer Screen { get; set; }
        public IInterpreter Interpreter { get; set; }
        public IBindingManager BindingMgr { get; set; }
        public Script ScriptHandler { get; set; }
        public ILogger Logger { get; set; }
        public IProcessor Processor { get; set; }
        public UpdateHandler UpdateHandler { get; set; }
        public IFunctionManager FunctionManager { get; set; }
        public IVolumeManager VolumeMgr { get; set; }
        public ISoundMaker SoundMaker { get; set; }
        public IGameEventDispatchManager GameEventDispatchManager { get; set; }
    }
}