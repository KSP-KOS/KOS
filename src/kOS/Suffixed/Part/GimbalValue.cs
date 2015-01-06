using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.Part
{
    public class GimbalValue : PartValue
    {
        private readonly ModuleGimbal gimbal;

        public GimbalValue(ModuleGimbal gimbal, SharedObjects sharedObj):base(gimbal.part, sharedObj)
        {
            this.gimbal = gimbal;
            InitializeGimbalSuffixes();
        }

        private void InitializeGimbalSuffixes()
        {
            AddSuffix("LOCK", new SetSuffix<bool>(() => gimbal.gimbalLock, value =>
            {
                if (value)
                {
                    gimbal.LockGimbal();
                }
                else
                {
                    gimbal.FreeGimbal();
                }
            }));
            AddSuffix("RANGE", new Suffix<float>(() => gimbal.gimbalRange ));
            AddSuffix("RESPONSESPEED", new Suffix<float>(() => gimbal.gimbalResponseSpeed ));
            AddSuffix("PITCHANGLE", new Suffix<float>(() => gimbal.gimbalAnglePitch ));
            AddSuffix("YAWANGLE", new Suffix<float>(() => gimbal.gimbalAngleYaw ));
            AddSuffix("ROLLANGLE", new Suffix<float>(() => gimbal.gimbalAngleRoll ));
        }
    }
}
