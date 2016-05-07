using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace kOS.Safe.Persistence
{
    public class VolumeManager : IVolumeManager
    {
        private readonly Dictionary<int, Volume> volumes;
        public virtual Volume CurrentVolume { get { return CurrentDirectory != null ? CurrentDirectory.Volume : null; } }
        public VolumeDirectory CurrentDirectory { get; set; }
        private int lastId;

        public Dictionary<int, Volume> Volumes { get { return volumes; } }
        public float CurrentRequiredPower { get; private set; }

        public VolumeManager()
        {
            volumes = new Dictionary<int, Volume>();
            CurrentDirectory = null;
        }


        public bool VolumeIsCurrent(Volume volume)
        {
            return volume == CurrentVolume;
        }

        private int GetVolumeId(string name)
        {
            int volumeId = -1;

            foreach (KeyValuePair<int, Volume> kvp in volumes)
            {
                if (string.Equals(kvp.Value.Name, name, StringComparison.CurrentCultureIgnoreCase))
                {
                    volumeId = kvp.Key;
                    break;
                }
            }

            return volumeId;
        }

        public int GetVolumeId(Volume volume)
        {
            int volumeId = -1;

            foreach (KeyValuePair<int, Volume> kvp in volumes)
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
            if (volumeId is string || volumeId is StringValue) return GetVolume(volumeId.ToString());
            // Convert to int instead of cast in case the identifier is stored
            // as an encapsulated ScalarValue, preventing an unboxing collision.
            try
            {
                return GetVolume(Convert.ToInt32(volumeId));
            }
            catch (InvalidCastException)
            {
                int id = GetVolumeId(volumeId.ToString());
                if (id >= 0)
                {
                    return GetVolume(id);
                }
                throw new kOS.Safe.Exceptions.KOSCastException(volumeId.GetType().Name, "Scalar|String|Volume");
            }
        }

        public Volume GetVolume(string name)
        {
            return GetVolume(GetVolumeId(name));
        }

        public virtual Volume GetVolume(int id)
        {
            if (volumes.ContainsKey(id))
            {
                return volumes[id];
            }
            return null;
        }

        public void Add(Volume volume)
        {
            if (!volumes.ContainsValue(volume))
            {
                volumes.Add(lastId++, volume);

                if (CurrentDirectory == null)
                {
                    CurrentDirectory = volumes[0].Root;
                    UpdateRequiredPower();
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
                volumes.Remove(id);

                if (CurrentVolume == volume)
                {
                    if (volumes.Count > 0)
                    {
                        CurrentDirectory = volumes[0].Root;
                        UpdateRequiredPower();
                    }
                    else
                    {
                        CurrentDirectory = null;
                    }
                }
            }
        }

        public void SwitchTo(Volume volume)
        {
            CurrentDirectory = volume.Root;
            UpdateRequiredPower();
        }

        public void UpdateVolumes(List<Volume> attachedVolumes)
        {
            // Remove volumes that are no longer attached
            var removals = new List<int>();
            foreach (var kvp in volumes)
            {
                if (!(kvp.Value is Archive) && !attachedVolumes.Contains(kvp.Value))
                {
                    removals.Add(kvp.Key);
                }
            }

            foreach (int id in removals)
            {
                volumes.Remove(id);
            }

            // Add volumes that have become attached
            foreach (Volume volume in attachedVolumes)
            {
                if (volume != null && !volumes.ContainsValue(volume))
                {
                    Add(volume);
                }
            }
        }

        public string GetVolumeBestIdentifier(Volume volume)
        {
            int id = GetVolumeId(volume);
            if (!string.IsNullOrEmpty(volume.Name)) return string.Format("#{0}: \"{1}\"", id, volume.Name);
            return "#" + id;
        }

        /// <summary>
        /// Like GetVolumeBestIdentifier, but without the extra string formatting.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns>The Volume's Identifier without pretty formatting</returns>
        public string GetVolumeRawIdentifier(Volume volume)
        {
            int id = GetVolumeId(volume);
            return !string.IsNullOrEmpty(volume.Name) ? volume.Name : id.ToString();
        }

        // Volumes, VolumeItems and strings
        public GlobalPath GlobalPathFromObject(object pathObject)
        {
            if (pathObject is Volume)
            {
                GlobalPath p = GlobalPath.FromVolumePath(VolumePath.EMPTY, GetVolumeRawIdentifier(pathObject as Volume));
                SafeHouse.Logger.Log("Path from volume: " + p);
                return p;
            } else if (pathObject is VolumeItem)
            {
                VolumeItem volumeItem = pathObject as VolumeItem;
                return GlobalPath.FromVolumePath(volumeItem.Path, GetVolumeRawIdentifier(volumeItem.Volume));
            } else
            {
                return GlobalPathFromString(pathObject.ToString());
            }

        }

        // Handles global, absolute and relative paths
        private GlobalPath GlobalPathFromString(string pathString)
        {
            if (GlobalPath.HasVolumeId(pathString))
            {
                return GlobalPath.FromString(pathString);
            } else
            {
                if (GlobalPath.IsAbsolute(pathString))
                {
                    return GlobalPath.FromVolumePath(VolumePath.FromString(pathString),
                        GetVolumeRawIdentifier(CurrentVolume));
                } else
                {
                    return GlobalPath.FromStringAndBase(pathString, GlobalPath.FromVolumePath(CurrentDirectory.Path,
                        GetVolumeRawIdentifier(CurrentVolume)));
                }
            }

        }

        public Volume GetVolumeFromPath(GlobalPath path)
        {
            Volume volume = GetVolume(path.VolumeId);

            if (volume == null)
            {
                throw new KOSPersistenceException("Volume not found: " + path.VolumeId);
            }

            return volume;
        }

        private void UpdateRequiredPower()
        {
            CurrentRequiredPower = (float)Math.Round(CurrentVolume.RequiredPower(), 4);
        }

        public bool Copy(GlobalPath sourcePath, GlobalPath destinationPath, bool verifyFreeSpace = true)
        {
            Volume sourceVolume = GetVolumeFromPath(sourcePath);
            Volume destinationVolume = GetVolumeFromPath(destinationPath);

            VolumeItem source = sourceVolume.Open(sourcePath);
            VolumeItem destination = destinationVolume.Open(destinationPath);

            if (source == null)
            {
                throw new KOSPersistenceException("Path does not exist: " + sourcePath);
            }

            if (source is VolumeDirectory)
            {
                if (destination is VolumeFile)
                {
                    throw new KOSPersistenceException("Can't copy directory into a file");
                }

                if (destination == null)
                {
                    destination = destinationVolume.CreateDirectory(destinationPath);
                } else if (!sourcePath.IsRoot)
                {
                    destinationPath = destinationPath.Combine(sourcePath.Name);
                    destination = destinationVolume.OpenOrCreateDirectory(destinationPath);
                }

                if (destination == null)
                {
                    throw new KOSException("Path was expected to point to a directory: " + destinationPath);
                }

                return CopyDirectory(sourcePath, destinationPath, verifyFreeSpace);
            } else
            {
                if (destination is VolumeFile || destination == null)
                {
                    Volume targetVolume = GetVolumeFromPath(destinationPath);
                    return CopyFile(source as VolumeFile, destinationPath, targetVolume, verifyFreeSpace);
                } else
                {
                    return CopyFileToDirectory(source as VolumeFile, destination as VolumeDirectory, verifyFreeSpace);
                }
            }
        }

        protected bool CopyDirectory(GlobalPath sourcePath, GlobalPath destinationPath, bool verifyFreeSpace)
        {
            if (sourcePath.IsParent(destinationPath))
            {
                throw new KOSPersistenceException("Can't copy directory to a subdirectory of itself: " + destinationPath);
            }

            Volume sourceVolume = GetVolumeFromPath(sourcePath);
            Volume destinationVolume = GetVolumeFromPath(destinationPath);

            VolumeDirectory source = sourceVolume.Open(sourcePath) as VolumeDirectory;

            VolumeItem destinationItem = destinationVolume.Open(destinationPath);

            if (destinationItem is VolumeFile)
            {
                throw new KOSPersistenceException("Can't copy directory into a file");
            }

            VolumeDirectory destination = destinationItem as VolumeDirectory;

            if (destination == null)
            {
                destination = destinationVolume.CreateDirectory(destinationPath);
            }

            var l = source.List();

            foreach (KeyValuePair<string, VolumeItem> pair in l)
            {
                if (pair.Value is VolumeDirectory)
                {
                    if (!CopyDirectory(sourcePath.Combine(pair.Key), destinationPath.Combine(pair.Key), verifyFreeSpace))
                    {
                        return false;
                    }
                } else
                {
                    if (!CopyFileToDirectory(pair.Value as VolumeFile, destination, verifyFreeSpace))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected bool CopyFile(VolumeFile volumeFile, GlobalPath destinationPath, Volume targetVolume,
            bool verifyFreeSpace)
        {
            return targetVolume.SaveFile(destinationPath, volumeFile.ReadAll(), verifyFreeSpace) != null;
        }

        protected bool CopyFileToDirectory(VolumeFile volumeFile, VolumeDirectory volumeDirectory,
            bool verifyFreeSpace)
        {
            return volumeDirectory.Volume.SaveFile(volumeDirectory.Path.Combine(volumeFile.Name), volumeFile.ReadAll(),
                verifyFreeSpace) != null;
        }

        public bool Move(GlobalPath sourcePath, GlobalPath destinationPath)
        {
            if (sourcePath.IsRoot)
            {
                throw new KOSPersistenceException("Can't move root directory: " + sourcePath);
            }

            if (sourcePath.IsParent(destinationPath))
            {
                throw new KOSPersistenceException("Can't move directory to a subdirectory of itself: " + destinationPath);
            }

            Volume sourceVolume = GetVolumeFromPath(sourcePath);
            Volume destinationVolume = GetVolumeFromPath(destinationPath);

            bool verifyFreeSpace = sourceVolume != destinationVolume;

            if (!Copy(sourcePath, destinationPath, verifyFreeSpace))
            {
                return false;
            }

            if (!sourceVolume.Delete(sourcePath))
            {
                throw new KOSPersistenceException("Can't remove: " + sourcePath);
            }

            return true;
        }
    }
}
