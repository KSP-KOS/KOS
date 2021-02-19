using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.PartModuleField;
using kOS.Suffixed.Part;
using UnityEngine;
using System;
using Expansions.Serenity;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("AttitudeController")]
    class AttitudeController : Structure
    {
        public AttitudeController(PartValue part, PartModuleFields module, ITorqueProvider provider)
        {
            torqueProvider = provider;

            AddSuffix("PART", new Suffix<PartValue>(() => part, "The part this AttitudeController belongs to."));
            AddSuffix("MODULE", new Suffix<PartModuleFields>(() => module, "The module this AttitudeController belongs to."));
            AddSuffix("ALLOWPITCH", new SetSuffix<BooleanValue>(() => allowPitch, (v) => { allowPitch = v; }, "Should this attitude controller respond to pitch input?"));
            AddSuffix("ALLOWROLL", new SetSuffix<BooleanValue>(() => allowRoll, (v) => { allowRoll = v; }, "Should this attitude controller respond to roll input?"));
            AddSuffix("ALLOWYAW", new SetSuffix<BooleanValue>(() => allowYaw, (v) => { allowYaw = v; }, "Should this attitude controller respond to yaw input?"));
            AddSuffix("ALLOWX", new SetSuffix<BooleanValue>(() => allowX, (v) => { allowX = v; }, "Should this attitude controller respond to translation X input?"));
            AddSuffix("ALLOWY", new SetSuffix<BooleanValue>(() => allowY, (v) => { allowY = v; }, "Should this attitude controller respond to translation Y input?"));
            AddSuffix("ALLOWZ", new SetSuffix<BooleanValue>(() => allowZ, (v) => { allowZ = v; }, "Should this attitude controller respond to translation Z input?"));

            AddSuffix("HASCUSTOMTHROTTLE", new Suffix<BooleanValue>(() => hasCustomThrottle, "Does this attitude controller have an independent custom throttle?"));
            AddSuffix("CUSTOMTHROTTLE", new SetSuffix<ScalarValue>(() => customThrottle, (v) => { customThrottle = v; }, "The value of the independent custom throttle."));
            AddSuffix("CONTROLLERTYPE", new Suffix<StringValue>(() => controllerType, "The type of controller."));
            AddSuffix("STATUS", new Suffix<StringValue>(() => status, "The status of the controller. One of UNKNOWN, ACTIVE, INACTIVE, BROKEN"));
            
            AddSuffix("RESPONSETIME", new Suffix<ScalarValue>(() => responseTime, "The response speed of the controller."));
        }
        //void test()
        //{
        //    ModuleControlSurface a;
        //    ModuleRCS a;
        //    ModuleEngines a;
        //    ModuleReactionWheel a;
        //    Expansions.Serenity.ModuleRoboticServoRotor a;
        //    ModuleResourceDrain a;
        //}
        protected ITorqueProvider torqueProvider { get; private set; }
        public virtual bool allowPitch { get { return false; } set { } }
        public virtual bool allowRoll { get { return false; } set { } }
        public virtual bool allowYaw { get { return false; } set { } }
        public virtual bool allowX { get { return false; } set { } }
        public virtual bool allowY { get { return false; } set { } }
        public virtual bool allowZ { get { return false; } set { } }

        public virtual float rotationAuthorityLimiter { get { return 0; } set { } }
        public virtual float translationAuthorityLimiter { get { return 0; } set { } }

        public virtual bool hasCustomThrottle { get { return false; } }
        public virtual float customThrottle { get { return 0; } set { } }
        public virtual string controllerType { get { return "UNKNOWN"; } }
        public virtual string status { get { return "UNKNOWN"; } }
        public virtual float responseTime { get { return 0; } set { } }

        public virtual AttitudeCorrectionResult positiveRotation
        {
            get
            {
                Vector3 pos = Vector3.zero;
                Vector3 _ = Vector3.zero;
                torqueProvider.GetPotentialTorque(out pos, out _);
                return new AttitudeCorrectionResult(new Vector(pos), Vector.Zero);
            }
        }
        public virtual AttitudeCorrectionResult negativeRotation
        {
            get
            {
                Vector3 _ = Vector3.zero;
                Vector3 neg = Vector3.zero;
                torqueProvider.GetPotentialTorque(out _, out neg);
                return new AttitudeCorrectionResult(new Vector(neg), Vector.Zero);
            }
        }
        public virtual AttitudeCorrectionResult throttle { get { return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero); } }
    }

    class AttitudeControllerReactionWheel : AttitudeController
    {
        public AttitudeControllerReactionWheel(PartValue part, PartModuleFields module, ModuleReactionWheel wheel)
            :base(part, module, wheel)
        {
            this.wheel = wheel;
        }

        protected ModuleReactionWheel wheel { get; private set; }
        
        public override string controllerType { get { return "REACTIONWHEEL"; } }
        public override string status { get { return wheel.State.ToString().ToUpper(); } }
        public override float rotationAuthorityLimiter { get { return wheel.authorityLimiter; } set { wheel.authorityLimiter = Math.Max(Math.Min(value, 100), 0); } }
        public override bool allowPitch { get { return true; } set { } }
        public override bool allowRoll { get { return true; } set { } }
        public override bool allowYaw { get { return true; } set { } }
        public override float responseTime { get { return wheel.torqueResponseSpeed; } set { } }
    }

    class AttitudeControllerControlSurface : AttitudeController
    {
        public AttitudeControllerControlSurface(PartValue part, PartModuleFields module, ModuleControlSurface surface)
            :base(part, module, surface)
        {
            this.surface = surface;
        }

        protected ModuleControlSurface surface { get; private set; }

        public override string controllerType { get { return "CONTROLSURFACE"; } }
        public override bool allowPitch { get { return !surface.ignorePitch; } set { surface.ignorePitch = !value; } }
        public override bool allowRoll { get { return !surface.ignoreRoll; } set { surface.ignoreRoll = !value; } }
        public override bool allowYaw { get { return !surface.ignoreYaw; } set { surface.ignoreYaw = !value; } }
        public override float rotationAuthorityLimiter { get { return surface.authorityLimiter; } set { surface.authorityLimiter = Math.Max(Math.Min(value, 100), 0); } }
    }

    // This assumes the rotor is connected with the base connected to the controller, not the rotating end.
    class AttitudeControllerRotor : AttitudeController
    {
        public AttitudeControllerRotor(PartValue part, PartModuleFields module, ModuleRoboticServoRotor rotor)
            : base(part, module, null)
        {
            this.rotor = rotor;
        }

        protected ModuleRoboticServoRotor rotor { get; private set; }

        public override string controllerType { get { return "ROTOR"; } }
        public override AttitudeCorrectionResult positiveRotation { get { return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero); } }
        public override AttitudeCorrectionResult negativeRotation { get { return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero); } }
        public override AttitudeCorrectionResult throttle
        {
            get
            {
                if (!rotor.servoIsMotorized)
                    return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero);

                Vector3 rotationEffect = Vector3.zero;
                Vector3 
                //TODO: compute rotation effect on yaw, pitch, roll
                if (rotor.rotateCounterClockwise)
                    rotationEffect *= -1.0f;
                rotationEffect *= rotor.servoMotorSize;
                return new AttitudeCorrectionResult(new Vector(rotationEffect), Vector.Zero);
            }
        }
        public override bool hasCustomThrottle { get { return true; } }
        public override float customThrottle { get { return rotor.maxTorque; } set { rotor.maxTorque = Math.Max(Math.Min(value, 100), 0); } }
        public override string status { get { return rotor.motorState.ToUpper(); } }
        public override float responseTime { get { return rotor.rotorSpoolTime; } set { } }
    }
}
