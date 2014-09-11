using kOS.InterProcessor;
using kOS.Binding;
using kOS.Module;
using kOS.Persistence;
using kOS.Compilation;
using kOS.Execution;
using kOS.Screen;
using kOS.Factories;

namespace kOS
{
    public class SharedObjects
    {
        public Vessel Vessel { get; set; }
        public CPU Cpu { get; set; }
        public BindingManager BindingMgr { get; set; }  
        public ScreenBuffer Screen { get; set; }
        public Interpreter Interpreter { get; set; }
        public Script ScriptHandler { get; set; }
        public Logger Logger { get; set; }
        public VolumeManager VolumeMgr { get; set; }
        public TermWindow Window { get; set; }
        public kOSProcessor Processor { get; set; }
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
