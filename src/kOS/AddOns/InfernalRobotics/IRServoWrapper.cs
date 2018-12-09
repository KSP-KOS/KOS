using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Suffixed.Part;

namespace kOS.AddOns.InfernalRobotics
{
    [kOS.Safe.Utilities.KOSNomenclature("IRServo")]
    public class IRServoWrapper : Structure
    {
        private readonly IRWrapper.IServo servo;
        private readonly SharedObjects shared;
        private readonly PartValue partValue;

        public IRServoWrapper(IRWrapper.IServo init, SharedObjects shared)
        {
            servo = init;
            this.shared = shared;
            this.partValue = GetPart();
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

            AddSuffix("MOVERIGHT", new NoArgsVoidSuffix(MoveRight));
            AddSuffix("MOVELEFT", new NoArgsVoidSuffix(MoveLeft));
            AddSuffix("MOVECENTER", new NoArgsVoidSuffix(MoveCenter));
            AddSuffix("MOVENEXTPRESET", new NoArgsVoidSuffix(MoveNextPreset));
            AddSuffix("MOVEPREVPRESET", new NoArgsVoidSuffix(MovePrevPreset));
            AddSuffix("STOP", new NoArgsVoidSuffix(Stop));

            AddSuffix("MOVETO", new TwoArgsSuffix<ScalarValue, ScalarValue>(MoveTo));

            AddSuffix("PART", new Suffix<PartValue>(() => this.partValue));
        }

        public void MoveRight()
        {
            partValue.ThrowIfNotCPUVessel();
            servo.MoveRight();
        }

        public void MoveLeft()
        {
            partValue.ThrowIfNotCPUVessel();
            servo.MoveLeft();
        }

        public void MoveCenter()
        {
            partValue.ThrowIfNotCPUVessel();
            servo.MoveCenter();
        }

        public void MoveNextPreset()
        {
            partValue.ThrowIfNotCPUVessel();
            servo.MoveNextPreset();
        }

        public void MovePrevPreset()
        {
            partValue.ThrowIfNotCPUVessel();
            servo.MovePrevPreset();
        }

        public void Stop()
        {
            partValue.ThrowIfNotCPUVessel();
            servo.Stop();
        }

        public void MoveTo(ScalarValue position, ScalarValue speed)
        {
            partValue.ThrowIfNotCPUVessel();
            servo.MoveTo(position, speed);
        }

        private PartValue GetPart()
        {
            var p = servo.HostPart;

            if (!p)
                shared.Logger.LogError("Cannot find Infernal Robotics part with UID: " + servo.UID);

            return p != null ? VesselTarget.CreateOrGetExisting(shared)[p] : null;
        }
    }
}
