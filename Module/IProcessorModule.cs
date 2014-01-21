using kOS.Persistance;

namespace kOS.Module
{
    public interface IProcessorModule
    {
        [KSPEvent(guiActive = true, guiName = "Open Terminal")]
        void Activate();

        [KSPEvent(guiActive = true, guiName = "Toggle Power")]
        void TogglePower();

        [KSPAction("Open Terminal", actionGroup = KSPActionGroup.None)]
        void Activate(KSPActionParam param);

        [KSPAction("Close Terminal", actionGroup = KSPActionGroup.None)]
        void Deactivate(KSPActionParam param);

        [KSPAction("Toggle Power", actionGroup = KSPActionGroup.None)]
        void TogglePower(KSPActionParam param);

        [KSPEvent(guiName = "Unit +", guiActive = false, guiActiveEditor = true)]
        void IncrementUnitId();

        [KSPEvent(guiName = "Unit -", guiActive = false, guiActiveEditor = true)]
        void DecrementUnitId();

        Part part { get; set; }
        Vessel vessel { get; }
        IVolume HardDisk { get; }
    }
}