using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Persistence
{
    public class VolumeManager
    {
        private Dictionary<int, Volume> _volumes;
        private Volume _currentVolume;
        private SharedObjects _shared;
        private int _lastId = 0;

        public Dictionary<int, Volume> Volumes { get { return _volumes; } }
        public Volume CurrentVolume { get { return GetVolumeWithRangeCheck(_currentVolume); } }

        public VolumeManager(SharedObjects shared)
        {
            _volumes = new Dictionary<int, Volume>();
            _currentVolume = null;
            _shared = shared;
        }

        private Volume GetVolumeWithRangeCheck(Volume volume)
        {
            if (volume.CheckRange(_shared.Vessel))
            {
                return volume;
            }
            else
            {
                throw new Exception("Volume is out of range");
            }
        }

        public bool VolumeIsCurrent(Volume volume)
        {
            return volume == _currentVolume;
        }

        private int GetVolumeId(string name)
        {
            int volumeId = -1;
            
            name = name.ToLower();
            foreach (KeyValuePair<int, Volume> kvp in _volumes)
            {
                if (kvp.Value.Name.ToLower() == name)
                {
                    volumeId = kvp.Key;
                    break;
                }
            }

            return volumeId;
        }

        private int GetVolumeId(Volume volume)
        {
            int volumeId = -1;

            foreach (KeyValuePair<int, Volume> kvp in _volumes)
            {
                if (kvp.Value == volume)
                {
                    volumeId = kvp.Key;
                    break;
                }
            }

            return volumeId;
        }

        public Volume GetVolume(object volumeId)
        {
            if (volumeId is int)
            {
                return GetVolume((int)volumeId);
            }
            else
            {
                return GetVolume(volumeId.ToString());
            }
        }
        
        public Volume GetVolume(string name)
        {
            return GetVolume(GetVolumeId(name));
        }

        public Volume GetVolume(int id)
        {
            if (_volumes.ContainsKey(id))
            {
                return GetVolumeWithRangeCheck(_volumes[id]);
            }
            else
            {
                return null;
            }
        }

        public void Add(Volume volume)
        {
            if (!_volumes.ContainsValue(volume))
            {
                _volumes.Add(_lastId++, volume);

                if (_currentVolume == null)
                {
                    _currentVolume = _volumes[0];
                }
            }
        }

        public void Remove(string name)
        {
            Remove(GetVolumeId(name));
        }

        public void Remove(int id)
        {
            Volume volume = GetVolume(id);

            if (volume != null)
            {
                _volumes.Remove(id);

                if (_currentVolume == volume)
                {
                    if (_volumes.Count > 0)
                    {
                        _currentVolume = _volumes[0];
                    }
                    else
                    {
                        _currentVolume = null;
                    }
                }
            }
        }

        public void SwitchTo(Volume volume)
        {
            if (volume != null)
            {
                _currentVolume = volume;
            }
        }

        public void UpdateVolumes(List<Volume> attachedVolumes)
        {
            // Remove volumes that are no longer attached
            List<int> removals = new List<int>();
            foreach (KeyValuePair<int, Volume> kvp in _volumes)
            {
                if (!(kvp.Value is Archive) && !attachedVolumes.Contains(kvp.Value))
                {
                    removals.Add(kvp.Key);
                }
            }

            foreach (int id in removals)
            {
                _volumes.Remove(id);
            }

            // Add volumes that have become attached
            foreach (Volume volume in attachedVolumes)
            {
                if (volume != null && !_volumes.ContainsValue(volume))
                {
                    Add(volume);
                }
            }
        }

        public string GetVolumeBestIdentifier(Volume volume)
        {
            int id = GetVolumeId(volume);
            if (!string.IsNullOrEmpty(volume.Name)) return string.Format("#{0}: \"{1}\"", id, volume.Name);
            else return "#" + id;
        }

    }
}
