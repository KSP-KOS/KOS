using kOS.InterProcessor;
using kOS.Binding;
using kOS.Persistence;
using kOS.Safe;
using kOS.Safe.Compilation;
using kOS.Safe.Execution;
using kOS.Safe.Module;
using kOS.Safe.Screen;
using kOS.Screen;
using kOS.Factories;

namespace kOS
{
    public class SharedObjects
    {
        public Vessel Vessel { get; set; }
        public ICpu Cpu { get; set; }
        public BindingManager BindingMgr { get; set; }  
        public ScreenBuffer Screen { get; set; }
        public Interpreter Interpreter { get; set; }
        public Script ScriptHandler { get; set; }
        public ILogger Logger { get; set; }
        public VolumeManager VolumeMgr { get; set; }
        public TermWindow Window { get; set; }
        public IProcessor Processor { get; set; }
        public ProcessorManager ProcessorMgr { get; set; }
        public UpdateHandler UpdateHandler { get; set; }
        public IFactory Factory { get; set; }
        

        public SharedObjects()
        {
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        }

        private void OnVesselDestroy(Vessel data)
        {
            if (data.id == Vessel.id)
            {
                BindingMgr.Dispose();
            }
        }
    }
}
