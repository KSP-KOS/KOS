using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.PartModuleField
{
    [Safe.Utilities.KOSNomenclature("Gimbal")]
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
            AddSuffix("PITCH", new SetSuffix<BooleanValue>(() => gimbal.enablePitch, value =>
            {
                gimbal.enablePitch = value;
            }, "Does the Gimbal respond to pitch controls?"));
            AddSuffix("YAW", new SetSuffix<BooleanValue>(() => gimbal.enableYaw, value =>
            {
                gimbal.enableYaw = value;
            }, "Does the Gimbal respond to yaw controls?"));
            AddSuffix("ROLL", new SetSuffix<BooleanValue>(() => gimbal.enableRoll, value =>
            {
                gimbal.enableRoll = value;
            }, "Does the Gimbal respond to roll controls?"));
            AddSuffix("LIMIT", new ClampSetSuffix<ScalarValue>(() => gimbal.gimbalLimiter,
                                              value => gimbal.gimbalLimiter = value,
                                              0f, 100f, 1f,
                                              "Gimbal range limit percentage"));
            AddSuffix("RANGE", new Suffix<ScalarValue>(() => gimbal.gimbalRange ,"The Gimbal's Possible Range of movement"));
            AddSuffix("RESPONSESPEED", new Suffix<ScalarValue>(() => gimbal.gimbalResponseSpeed, "The Gimbal's Possible Rate of travel"));
            AddSuffix("PITCHANGLE", new Suffix<ScalarValue>(() =>  gimbal.gimbalLock ? 0 : gimbal.actuation.x, "Current Gimbal Pitch"));
            AddSuffix("YAWANGLE", new Suffix<ScalarValue>(() =>  gimbal.gimbalLock ? 0 : gimbal.actuation.z, "Current Gimbal Yaw" ));
            AddSuffix("ROLLANGLE", new Suffix<ScalarValue>(() => gimbal.gimbalLock ? 0 : gimbal.actuation.y, "Current Gimbal Roll"));
        }
    }
}
