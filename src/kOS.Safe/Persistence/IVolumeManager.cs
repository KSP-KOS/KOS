using System.Collections.Generic;

namespace kOS.Safe.Persistence
{
    public interface IVolumeManager
    {
        Dictionary<int, Volume> Volumes { get; }
        Volume CurrentVolume { get; }
        float CurrentRequiredPower { get; }
        bool VolumeIsCurrent(Volume volume);
        int GetVolumeId(Volume volume);
        Volume GetVolume(object volumeId);
        Volume GetVolume(string name);
        Volume GetVolume(int id);
        void Add(Volume volume);
        void Remove(string name);
        void Remove(int id);
        void SwitchTo(Volume volume);
        void UpdateVolumes(List<Volume> attachedVolumes);
        string GetVolumeBestIdentifier(Volume volume);

        /// <summary>
        /// Like GetVolumeBestIdentifier, but without the extra string formatting.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns>The Volume's Identifier without pretty formatting</returns>
        string GetVolumeRawIdentifier(Volume volume);
    }
}