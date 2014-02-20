using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class VolumeManager
    {
        private List<Volume> _volumes;
        private Volume _currentVolume;
        private SharedObjects _shared;
        private int _lastId = 0;

        public List<Volume> Volumes { get { return _volumes; } }
        public Volume CurrentVolume { get { return GetVolumeWithRangeCheck(_currentVolume); } }

        public VolumeManager(SharedObjects shared)
        {
            _volumes = new List<Volume>();
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
            foreach (Volume volume in _volumes)
            {
                if (volume.Name.ToLower() == name)
                {
                    volumeId = _volumes.IndexOf(volume);
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
            if (id >= 0 && id < _volumes.Count)
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
            if (!_volumes.Contains(volume))
            {
                volume.Id = _lastId++;
                _volumes.Add(volume);

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
                _volumes.Remove(volume);

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
            foreach (Volume volume in new List<Volume>(_volumes))
            {
                if (!(volume is Archive) && !attachedVolumes.Contains(volume))
                {
                    _volumes.Remove(volume);
                }
            }

            // Add volumes that have become attached
            foreach (Volume volume in attachedVolumes)
            {
                if (!_volumes.Contains(volume))
                {
                    _volumes.Add(volume);
                }
            }
        }

    }
}
