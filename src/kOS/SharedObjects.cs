using kOS.InterProcessor;
using kOS.Binding;
using kOS.Factories;
using kOS.Screen;

namespace kOS
{
    public class SharedObjects : Safe.SharedObjects
    {
        public Vessel Vessel { get; set; }
        public BindingManager BindingMgr { get; set; }  
        public ProcessorManager ProcessorMgr { get; set; }
        public IFactory Factory { get; set; }
        public Part KSPPart { get; set; }
        public TermWindow Window { get; set; }
        public TransferManager TransferManager { get; set; }

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