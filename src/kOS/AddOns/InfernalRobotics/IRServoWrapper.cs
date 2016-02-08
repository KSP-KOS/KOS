using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

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
            AddSuffix("NAME", new SetSuffix<StringValue>(() => servo.Name, value => servo.Name = value));
            AddSuffix("UID", new Suffix<ScalarValue>(() => ScalarValue.Create((int)servo.UID)));
            AddSuffix("HIGHLIGHT", new SetSuffix<BooleanValue>(() => true, value => servo.Highlight = value));

            AddSuffix("POSITION", new Suffix<ScalarValue>(() => servo.Position));
            AddSuffix("MINCFGPOSITION", new Suffix<ScalarValue>(() => servo.MinConfigPosition));
            AddSuffix("MAXCFGPOSITION", new Suffix<ScalarValue>(() => servo.MaxConfigPosition));
            AddSuffix("MINPOSITION", new SetSuffix<ScalarValue>(() => servo.MinPosition, value => servo.MinPosition = value));
            AddSuffix("MAXPOSITION", new SetSuffix<ScalarValue>(() => servo.MaxPosition, value => servo.MaxPosition = value));
            AddSuffix("CONFIGSPEED", new Suffix<ScalarValue>(() => servo.ConfigSpeed));
            AddSuffix("CURRENTSPEED", new SetSuffix<ScalarValue>(() => servo.CurrentSpeed, value => servo.CurrentSpeed = value));
            AddSuffix("SPEED", new SetSuffix<ScalarValue>(() => servo.Speed, value => servo.Speed = value));
            AddSuffix("ACCELERATION", new SetSuffix<ScalarValue>(() => servo.Acceleration, value => servo.Acceleration = value));

            AddSuffix("ISMOVING", new Suffix<BooleanValue>(() => servo.IsMoving));
            AddSuffix("ISFREEMOVING", new Suffix<BooleanValue>(() => servo.IsFreeMoving));
            AddSuffix("LOCKED", new SetSuffix<BooleanValue>(() => servo.IsLocked, value => servo.IsLocked = value));
            AddSuffix("INVERTED", new SetSuffix<BooleanValue>(() => servo.IsAxisInverted, value => servo.IsAxisInverted = value));

            AddSuffix("MOVERIGHT", new NoArgsSuffix(MoveRight));
            AddSuffix("MOVELEFT", new NoArgsSuffix(MoveLeft));
            AddSuffix("MOVECENTER", new NoArgsSuffix(MoveCenter));
            AddSuffix("MOVENEXTPRESET", new NoArgsSuffix(MoveNextPreset));
            AddSuffix("MOVEPREVPRESET", new NoArgsSuffix(MovePrevPreset));
            AddSuffix("STOP", new NoArgsSuffix(Stop));

            AddSuffix("MOVETO", new TwoArgsSuffix<float, float>(MoveTo));

            AddSuffix("PART", new Suffix<PartValue>(GetPart));
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

        public PartValue GetPart()
        {
            var v = shared.Vessel;

            var p = v.Parts.Find (s => s.craftID == servo.UID);
            shared.Logger.LogError("Cannot find Infernal Robotics part with UID: " + servo.UID);

            return p != null ? new PartValue (p, shared) : null;
        }
    }
}