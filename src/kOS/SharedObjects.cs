using kOS.InterProcessor;
using kOS.Binding;
using kOS.Persistence;
using kOS.Screen;
using kOS.Factories;

namespace kOS
{
    public class SharedObjects : Safe.SharedObjects
    {
        public Vessel Vessel { get; set; }
        public BindingManager BindingMgr { get; set; }  
        public VolumeManager VolumeMgr { get; set; }
        public TermWindow Window { get; set; }
        public ProcessorManager ProcessorMgr { get; set; }
        public IFactory Factory { get; set; }
        public Part KSPPart { get; set; }

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