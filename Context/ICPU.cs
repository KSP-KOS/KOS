using System.Collections.Generic;
using kOS.Persistance;

namespace kOS.Context
{
    public interface ICPU : IExecutionContext
    {
        IVolume Archive { get; }
        float SessionTime { get; }
        CPUMode Mode { get; set; }
        double TestFunction(double x, double y);
        void RegisterkOSExternalFunction(object[] parameters);
        void RegisterkOSExternalFunction(string name, object externalParent, string methodName, int parameterCount);
        void Boot();
        bool IsAlive();
        void AttachVolume(IVolume hardDisk);
        void UpdateUnitId(int unitID);
        void ProcessElectricity(Part part, float time);
        void UpdateVolumeMounts(IList<IVolume> attachedVolumes);
    }
}