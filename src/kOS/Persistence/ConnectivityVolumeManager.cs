using kOS.Safe.Exceptions;
using kOS.Safe.Persistence;

namespace kOS.Persistence
{
    public class ConnectivityVolumeManager : VolumeManager
    {
        private readonly SharedObjects shared;

        public ConnectivityVolumeManager(SharedObjects shared)
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
            if (CheckRange(volume))
                return volume;
            throw new KOSVolumeOutOfRangeException();
        }

        // check the range on the current volume without calling GetVolumeWithRangeCheck
        public override bool CheckCurrentVolumeRange()
        {
            return CheckRange(base.CurrentVolume);
        }

        public override bool CheckRange(Volume volume)
        {
            // We don't need to check the connectivity enable settings here, that is handled inside
            // the IConnectivityManager classes.
            var archive = volume as Archive;
            if (archive != null)
            {
                if (shared.Vessel.situation == Vessel.Situations.PRELAUNCH || Communication.ConnectivityManager.HasConnectionToHome(shared.Vessel))
                    return true;
            }
            else
            {
                // Right now, the volume manager only lists volumes that are attached to the
                // the associated vessel, so there is no way to remotely access another volume
                // other than the archive.  If that changes in the future we will need to
                // add logic to see if the harddisk is in range.  This was not checked in
                // previous versions either, and it requires that the volume manager knows the
                // vessel associated with each HardDisk.  Right now there is no way to get vessel
                // information directly from HardDisk but that is managable with a simple subclass.
                return true;
            }
            return false;
        }
    }
}