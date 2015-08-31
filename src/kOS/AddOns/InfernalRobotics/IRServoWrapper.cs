using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using System;
using System.Linq;

namespace kOS.AddOns.InfernalRobotics
{
    public class IRServoWrapper : Structure
    {
        private readonly IRWrapper.IServo servo;
        private readonly SharedObjects shared;

        public IRServoWrapper(IRWrapper.IServo init, SharedObjects shared)
        {
            servo = init;
            this.shared = shared;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new SetSuffix<string>(() => servo.Name, value => servo.Name = value));
            AddSuffix("UID", new Suffix<uint>(() => servo.UID));
            AddSuffix("HIGHLIGHT", new SetSuffix<bool>(() => true, value => servo.Highlight = value));

            AddSuffix("POSITION", new Suffix<float>(() => servo.Position));
            AddSuffix("MINCFGPOSITION", new Suffix<float>(() => servo.MinConfigPosition));
            AddSuffix("MAXCFGPOSITION", new Suffix<float>(() => servo.MaxConfigPosition));
            AddSuffix("MINPOSITION", new SetSuffix<float>(() => servo.MinPosition, value => servo.MinPosition = value));
            AddSuffix("MAXPOSITION", new SetSuffix<float>(() => servo.MaxPosition, value => servo.MaxPosition = value));
            AddSuffix("CONFIGSPEED", new Suffix<float>(() => servo.ConfigSpeed));
            AddSuffix("CURRENTSPEED", new SetSuffix<float>(() => servo.CurrentSpeed, value => servo.CurrentSpeed = value));
            AddSuffix("SPEED", new SetSuffix<float>(() => servo.Speed, value => servo.Speed = value));
            AddSuffix("ACCELERATION", new SetSuffix<float>(() => servo.Acceleration, value => servo.Acceleration = value));

            AddSuffix("ISMOVING", new Suffix<bool>(() => servo.IsMoving));
            AddSuffix("ISFREEMOVING", new Suffix<bool>(() => servo.IsFreeMoving));
            AddSuffix("LOCKED", new SetSuffix<bool>(() => servo.IsLocked, value => servo.IsLocked = value));
            AddSuffix("INVERTED", new SetSuffix<bool>(() => servo.IsAxisInverted, value => servo.IsAxisInverted = value));

            AddSuffix("MOVERIGHT", new NoArgsSuffix(MoveRight));
            AddSuffix("MOVELEFT", new NoArgsSuffix(MoveLeft));
            AddSuffix("MOVECENTER", new NoArgsSuffix(MoveCenter));
            AddSuffix("MOVENEXTPRESET", new NoArgsSuffix(MoveNextPreset));
            AddSuffix("MOVEPREVPRESET", new NoArgsSuffix(MovePrevPreset));
            AddSuffix("STOP", new NoArgsSuffix(Stop));

            AddSuffix("MOVETO", new TwoArgsSuffix<float, float>(MoveTo));

            AddSuffix("PART", new Suffix<kOS.Suffixed.Part.PartValue>(GetPart));
        }

        
        public void MoveRight()
        {
            servo.MoveRight();
        }

        public void MoveLeft()
        {
            servo.MoveLeft();
        }

        public void MoveCenter()
        {
            servo.MoveCenter();
        }

        public void MoveNextPreset()
        {
            servo.MoveNextPreset();
        }

        public void MovePrevPreset()
        {
            servo.MovePrevPreset();
        }

        public void Stop()
        {
            servo.Stop();
        }

        public void MoveTo(float position, float speed)
        {
            servo.MoveTo(position, speed);
        }

        public kOS.Suffixed.Part.PartValue GetPart()
        {
            var v = shared.Vessel;

            var p = v.Parts.Find (s => s.craftID == servo.UID);

            if (p != null)
                return new kOS.Suffixed.Part.PartValue (p, shared);
            else
                return null;
        }
    }
}