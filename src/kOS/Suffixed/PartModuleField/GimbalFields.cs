using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.PartModuleField
{
    [kOS.Safe.Utilities.KOSNomenclature("Gimbal")]
    public class GimbalFields : PartModuleFields
    {
        private readonly ModuleGimbal gimbal;

        public GimbalFields(ModuleGimbal gimbal, SharedObjects sharedObj):base(gimbal, sharedObj)
        {
            this.gimbal = gimbal;
            InitializeGimbalSuffixes();
        }

        private void InitializeGimbalSuffixes()
        {
            AddSuffix("LOCK", new SetSuffix<BooleanValue>(() => gimbal.gimbalLock, value =>
            {
                gimbal.gimbalLock = value;
            }, "Is the Gimbal free to travel?"));
            AddSuffix("RANGE", new Suffix<ScalarValue>(() => gimbal.gimbalRange ,"The Gimbal's Possible Range of movement"));
            AddSuffix("RESPONSESPEED", new Suffix<ScalarValue>(() => gimbal.gimbalResponseSpeed, "The Gimbal's Possible Rate of travel"));
            AddSuffix("PITCHANGLE", new Suffix<ScalarValue>(() =>  gimbal.gimbalLock ? 0 : gimbal.gimbalAnglePitch, "Current Gimbal Pitch"));
            AddSuffix("YAWANGLE", new Suffix<ScalarValue>(() =>  gimbal.gimbalLock ? 0 : gimbal.gimbalAngleYaw, "Current Gimbal Yaw" ));
            AddSuffix("ROLLANGLE", new Suffix<ScalarValue>(() => gimbal.gimbalLock ? 0 : gimbal.gimbalAngleRoll, "Current Gimbal Roll"));
        }
    }
}
