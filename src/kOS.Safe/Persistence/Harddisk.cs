using System;
using kOS.Safe.Utilities;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Persistence
{
    public sealed class Harddisk : Volume
    {
        public Harddisk(int size)
        {
            Capacity = size;
        }

        public override bool SaveFile(ProgramFile file)
        {
            Debug.Logger.Log("HardDisk: SaveFile: " + file.Filename);
            return IsRoomFor(file) && base.SaveFile(file);
        }

        public override int GetFreeSpace()
        {
            return System.Math.Max(Capacity - GetUsedSpace(), 0);
        }

        public override bool IsRoomFor(ProgramFile newFile)
        {
            int usedSpace = GetUsedSpace();
            ProgramFile existingFile = GetByName(newFile.Filename);

            if (existingFile != null)
            {
                usedSpace -= existingFile.GetSize();
            }

            return ((Capacity - usedSpace) >= newFile.GetSize());
        }

        public override bool KOSEquals(object other)
        {
            // Harddisk attaches to a real filesystem directory.  Not sure how we'd ever have more than one to compare anyway:
            throw new KOSBinaryOperandTypeException(this.GetType(),"=","and",other.GetType());
        } 

   }
}
