using System.Collections.Generic;

namespace kOS.Persistance
{
    public interface IVolume
    {
        IList<File> Files { get; set; }
        string Name { get; set; }
        int Capacity { get; set; }
        bool Renameable { get; set; }
        File GetByName(string name);
        void AppendToFile(string name, string str);
        void DeleteByName(string name);
        bool SaveFile(File file);
        int GetFreeSpace();
        bool IsRoomFor(File newFile);
        void LoadPrograms(IList<File> programsToLoad);
        ConfigNode Save(string nodeName);
        IList<FileInfo> GetFileList();
        bool CheckRange();
        float RequiredPower();
    }
}