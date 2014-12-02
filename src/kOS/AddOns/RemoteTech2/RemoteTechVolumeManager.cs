using kOS.Persistence;
using kOS.Safe.Persistence;
using System;

namespace kOS.AddOns.RemoteTech2
{
    public class RemoteTechVolumeManager : VolumeManager
    {
        private readonly SharedObjects shared;

        public RemoteTechVolumeManager(SharedObjects shared)
        {
            this.shared = shared;
        }

        public override Volume CurrentVolume { get { return GetVolumeWithRangeCheck(base.CurrentVolume); } }

        public override Volume GetVolume(int id)
        {
            return GetVolumeWithRangeCheck(base.GetVolume(id));
        }

        private Volume GetVolumeWithRangeCheck(Volume volume)
        {
            if (volume.CheckRange(shared.Vessel))
            {
                return volume;
            }
            throw new Exception("Volume is out of range");
        }
    }
}