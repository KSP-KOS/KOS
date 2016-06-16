using System;
using kOS.Persistence;
using kOS.Safe.Persistence;
using kOS.Safe.Exceptions;

namespace kOS.AddOns.RemoteTech
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
            throw new Safe.Exceptions.KOSVolumeOutOfRangeException();
        }

        // check the range on the current volume without calling GetVolumeWithRangeCheck
        public bool CheckCurrentVolumeRange(Vessel vessel)
        {
            return base.CurrentVolume.CheckRange(vessel);
        }
    }
}