using Expansions.Serenity;
using kOS.Control;
using kOS.Module;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;
using kOS.Suffixed.PartModuleField;
using kOS.Utilities;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("AttitudeController")]
    public class AttitudeController : Structure
    {
        public AttitudeController(PartValue part, PartModuleFields module, ITorqueProvider provider)
        {
            TorqueProvider = provider;
            this.Part = part;

            AddSuffix("PART", new Suffix<PartValue>(() => part, "The part this AttitudeController belongs to."));
            AddSuffix("MODULE", new Suffix<Structure>(() =>
            {
                if (module == null)
                    return new BooleanValue(false);
                return module;
            }, "The module this AttitudeController belongs to."));
            AddSuffix("ALLOWPITCH", new SetSuffix<BooleanValue>(() => AllowPitch, (v) => { AllowPitch = v; }, "Should this attitude controller respond to pitch input?"));
            AddSuffix("ALLOWROLL", new SetSuffix<BooleanValue>(() => AllowRoll, (v) => { AllowRoll = v; }, "Should this attitude controller respond to roll input?"));
            AddSuffix("ALLOWYAW", new SetSuffix<BooleanValue>(() => AllowYaw, (v) => { AllowYaw = v; }, "Should this attitude controller respond to yaw input?"));
            AddSuffix("ALLOWX", new SetSuffix<BooleanValue>(() => AllowX, (v) => { AllowX = v; }, "Should this attitude controller respond to translation X input?"));
            AddSuffix("ALLOWY", new SetSuffix<BooleanValue>(() => AllowY, (v) => { AllowY = v; }, "Should this attitude controller respond to translation Y input?"));
            AddSuffix("ALLOWZ", new SetSuffix<BooleanValue>(() => AllowZ, (v) => { AllowZ = v; }, "Should this attitude controller respond to translation Z input?"));

            AddSuffix("HASCUSTOMTHROTTLE", new Suffix<BooleanValue>(() => HasCustomThrottle, "Does this attitude controller have an independent custom throttle?"));
            AddSuffix("CUSTOMTHROTTLE", new SetSuffix<ScalarValue>(() => CustomThrottle, (v) => { CustomThrottle = v; }, "The value of the independent custom throttle."));
            AddSuffix("CONTROLLERTYPE", new Suffix<StringValue>(() => ControllerType, "The type of controller."));
            AddSuffix("STATUS", new Suffix<StringValue>(() => Status, "The status of the controller."));
            
            AddSuffix("RESPONSETIME", new Suffix<ScalarValue>(() => ResponseTime, "The response speed of the controller."));
            
            AddSuffix("POSITIVEROTATION", new Suffix<AttitudeCorrectionResult>(() => positiveRotation, "What is the torque applied when giving a positive input on pitch, roll and yaw."));
            AddSuffix("NEGATIVEROTATION", new Suffix<AttitudeCorrectionResult>(() => negativeRotation, "What is the torque applied when giving a negative input on pitch, roll and yaw."));
            
            AddSuffix("ROTATIONAUTHORITYLIMITER", new SetSuffix<ScalarValue>(() => RotationAuthorityLimiter, (v) => { RotationAuthorityLimiter = v; }, "The authority limit for rotating."));
            AddSuffix("TRANSLATIONAUTHORITYLIMITER", new SetSuffix<ScalarValue>(() => TranslationAuthorityLimiter, (v) => { TranslationAuthorityLimiter = v; }, "The authority limit for translating."));
            
            AddSuffix("RESPONSEFOR", new ThreeArgsSuffix<AttitudeCorrectionResult, Vector, Vector, ScalarValue>((rotation, translation, thrust) =>
            {
                return GetResponseFor((float)rotation.X, (float)rotation.Y, (float)rotation.Z, (float)translation.X, (float)translation.Y, (float)translation.Z, thrust);
            }, "What is the torque applied when setting the following controls."));
        }

        public static AttitudeController FromModule(PartValue part, PartModuleFields moduleStructure, PartModule module)
        {
            if (module is ModuleReactionWheel)
                return new AttitudeControllerReactionWheel(part, moduleStructure, (ModuleReactionWheel)module);
            if (module is ModuleRCS)
                return new AttitudeControllerRCS(part, moduleStructure, (ModuleRCS)module);
            if (module is ModuleControlSurface)
                return new AttitudeControllerControlSurface(part, moduleStructure, (ModuleControlSurface)module);
            if (module is ModuleRoboticServoRotor)
                return new AttitudeControllerRotor(part, moduleStructure, (ModuleRoboticServoRotor)module);
            if (module is ModuleEngines)
                return new AttitudeControllerEngine(part, moduleStructure);
            if (module is ModuleResourceDrain)
                return new AttitudeControllerDrainValve(part, moduleStructure, (ModuleResourceDrain)module);
            if (module is ModuleGimbal)
                return null; // Already covered by engine attitude controller
            if (module is ITorqueProvider)
                return new AttitudeController(part, moduleStructure, (ITorqueProvider)module);
            return null;
        }

        protected PartValue Part { get; private set; }
        protected ITorqueProvider TorqueProvider { get; private set; }
        public virtual bool AllowPitch { get { return false; } set { } }
        public virtual bool AllowRoll { get { return false; } set { } }
        public virtual bool AllowYaw { get { return false; } set { } }
        public virtual bool AllowX { get { return false; } set { } }
        public virtual bool AllowY { get { return false; } set { } }
        public virtual bool AllowZ { get { return false; } set { } }

        public virtual float RotationAuthorityLimiter { get { return 0; } set { } }
        public virtual float TranslationAuthorityLimiter { get { return 0; } set { } }

        public virtual bool HasCustomThrottle { get { return false; } }
        public virtual float CustomThrottle { get { return 0; } set { } }
        public virtual string ControllerType { get { return "UNKNOWN"; } }
        public virtual string Status { get { return "UNKNOWN"; } }
        public virtual float ResponseTime { get { return 0; } set { } }


        public override string ToString()
        {
            return "AttitudeController(" + ControllerType + ")";
        }

        protected void GetPotentialTorque(out Vector3 positive, out Vector3 negative)
        {
            var controlParameter = kOSVesselModule.GetInstance(Part.Shared.Vessel).GetFlightControlParameter("steering");
            var steeringManager = controlParameter as SteeringManager;

            if (steeringManager == null)
            {
                positive = Vector3.zero;
                negative = Vector3.zero;
                return;
            }
            Vector3 pos = Vector3.zero;
            Vector3 neg = Vector3.zero;
            steeringManager.CorrectedGetPotentialTorque(TorqueProvider, out pos, out neg);
            positive.x = pos.x;
            positive.y = pos.z;
            positive.z = pos.y;
            negative.x = neg.x;
            negative.y = neg.z;
            negative.z = neg.y;
        }
        public virtual AttitudeCorrectionResult positiveRotation
        {
            get
            {
                Vector3 pos = Vector3.zero;
                Vector3 _ = Vector3.zero;
                GetPotentialTorque(out pos, out _);

                AttitudeCorrectionResult estimate = GetResponseFor(1, 1, 1, 0, 0, 0, CustomThrottle);
                return new AttitudeCorrectionResult(new Vector(pos), estimate.translation);
            }
        }
        public virtual AttitudeCorrectionResult negativeRotation
        {
            get
            {
                Vector3 _ = Vector3.zero;
                Vector3 neg = Vector3.zero;
                GetPotentialTorque(out _, out neg);
                
                AttitudeCorrectionResult estimate = GetResponseFor(-1, -1, -1, 0, 0, 0, CustomThrottle);
                return new AttitudeCorrectionResult(new Vector(neg), estimate.translation);
            }
        }
        public virtual AttitudeCorrectionResult GetResponseFor(float pitch, float yaw, float roll, float x, float y, float z, float throttle)
        {
            return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero);
        }

        protected Vector3 StarVector { get { return VesselUtils.GetFacing(Part.Shared.Vessel).Rotation * Vector3.right; } }
        protected Vector3 TopVector { get { return VesselUtils.GetFacing(Part.Shared.Vessel).Rotation * Vector3.up; } }
        protected Vector3 ForeVector { get { return VesselUtils.GetFacing(Part.Shared.Vessel).Rotation * Vector3.forward; } }
        protected Vector3 PitchVector(Vector3 lever)
        {
            Vector3 projectedLever = Vector3.ProjectOnPlane(lever, StarVector);
            if (projectedLever.sqrMagnitude < 0.0001)
                return Vector3.zero;
            return Vector3.Cross(projectedLever.normalized, StarVector);
        }
        protected Vector3 YawVector(Vector3 lever)
        {
            Vector3 projectedLever = Vector3.ProjectOnPlane(lever, TopVector);
            if (projectedLever.sqrMagnitude < 0.0001)
                return Vector3.zero;
            return Vector3.Cross(projectedLever.normalized, TopVector);
        }
        protected Vector3 RollVector(Vector3 lever)
        {
            Vector3 projectedLever = Vector3.ProjectOnPlane(lever, ForeVector);
            if (projectedLever.sqrMagnitude < 0.0001)
                return Vector3.zero;
            return Vector3.Cross(projectedLever.normalized, ForeVector);
        }
        protected Vector3 PositionToLever(Vector3 position)
        {
            Vector3 shipCenterOfMass = Part.Shared.Vessel.CoMD;
            return position - shipCenterOfMass;
        }
        public static Direction GetTransformFacing(Transform transform)
        {
            var rotation = transform.rotation;
            Quaternion facing = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(rotation) * Quaternion.identity);
            return new Direction(facing);
        }

        protected AttitudeCorrectionResult SplitComponents(Vector3 lever, Vector3 thrust)
        {
            Vector3 pitchVector = PitchVector(lever);
            Vector3 yawVector = YawVector(lever);
            Vector3 rollVector = RollVector(lever);
            
            Vector3 pitchLever = Vector3.ProjectOnPlane(lever, StarVector);
            Vector3 yawLever = Vector3.ProjectOnPlane(lever, TopVector);
            Vector3 rollLever = Vector3.ProjectOnPlane(lever, ForeVector);

            float resultingPitch = Vector3.Dot(thrust, pitchVector) * pitchLever.magnitude;
            float resultingYaw = Vector3.Dot(thrust, yawVector) * yawLever.magnitude;
            float resultingRoll = Vector3.Dot(thrust, rollVector) * rollLever.magnitude;

            // Whatever force not used for angular momentum is thrust
            Vector3 remainingThrust = thrust;
            if (pitchVector.magnitude > 0)
                remainingThrust = Vector3.ProjectOnPlane(remainingThrust, pitchVector);
            if (yawVector.magnitude > 0)
                remainingThrust = Vector3.ProjectOnPlane(remainingThrust, yawVector);
            if (rollVector.magnitude > 0)
                remainingThrust = Vector3.ProjectOnPlane(remainingThrust, rollVector);

            float resultingX = Vector3.Dot(remainingThrust, StarVector);
            float resultingY = Vector3.Dot(remainingThrust, TopVector);
            float resultingZ = Vector3.Dot(remainingThrust, ForeVector);

            return new AttitudeCorrectionResult(new Vector(resultingPitch, resultingYaw, resultingRoll), new Vector(resultingX, resultingY, resultingZ));
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("AttitudeController", KOSToCSharp = false)]
    class AttitudeControllerReactionWheel : AttitudeController
    {
        public AttitudeControllerReactionWheel(PartValue part, PartModuleFields module, ModuleReactionWheel wheel)
            :base(part, module, wheel)
        {
            this.wheel = wheel;
        }

        protected ModuleReactionWheel wheel { get; private set; }
        
        public override string ControllerType { get { return "REACTIONWHEEL"; } }
        public override string Status { get { return wheel.State.ToString().ToUpper(); } }
        public override float RotationAuthorityLimiter { get { return wheel.authorityLimiter; } set { wheel.authorityLimiter = Math.Max(Math.Min(value, 100), 0); } }
        public override bool AllowPitch { get { return true; } set { } }
        public override bool AllowRoll { get { return true; } set { } }
        public override bool AllowYaw { get { return true; } set { } }
        public override float ResponseTime { get { return wheel.torqueResponseSpeed; } set { } }
        public override AttitudeCorrectionResult GetResponseFor(float pitch, float yaw, float roll, float x, float y, float z, float throttle)
        {
            Vector3 pos = Vector3.zero;
            Vector3 neg = Vector3.zero;
            GetPotentialTorque(out pos, out neg);
            Vector3 rotation = Vector3.zero;
            rotation += new Vector3(
                Math.Max(0, pitch) * pos.x,
                Math.Max(0, yaw) * pos.z,
                Math.Max(0, roll) * pos.y
            );
            rotation += new Vector3(
                Math.Min(0, pitch) * -neg.x,
                Math.Min(0, yaw) * -neg.z,
                Math.Min(0, roll) * -neg.y
            );
            return new AttitudeCorrectionResult(new Vector(rotation), new Vector(Vector.Zero));
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("AttitudeController", KOSToCSharp = false)]
    class AttitudeControllerControlSurface : AttitudeController
    {
        public AttitudeControllerControlSurface(PartValue part, PartModuleFields module, ModuleControlSurface surface)
            :base(part, module, surface)
        {
            this.surface = surface;
        }

        protected ModuleControlSurface surface { get; private set; }

        public override string ControllerType { get { return "CONTROLSURFACE"; } }
        public override bool AllowPitch { get { return !surface.ignorePitch; } set { surface.ignorePitch = !value; } }
        public override bool AllowRoll { get { return !surface.ignoreRoll; } set { surface.ignoreRoll = !value; } }
        public override bool AllowYaw { get { return !surface.ignoreYaw; } set { surface.ignoreYaw = !value; } }
        public override float RotationAuthorityLimiter { get { return surface.authorityLimiter; } set { surface.authorityLimiter = Math.Max(Math.Min(value, 100), 0); } }
        public override AttitudeCorrectionResult GetResponseFor(float pitch, float yaw, float roll, float x, float y, float z, float throttle)
        {
            Vector3 pos = Vector3.zero;
            Vector3 neg = Vector3.zero;
            GetPotentialTorque(out pos, out neg);
            Vector3 rotation = Vector3.zero;
            rotation += new Vector3(
                Math.Max(0, pitch) * pos.x,
                Math.Max(0, yaw) * pos.z,
                Math.Max(0, roll) * pos.y
            );
            rotation += new Vector3(
                Math.Min(0, pitch) * -neg.x,
                Math.Min(0, yaw) * -neg.z,
                Math.Min(0, roll) * -neg.y
            );
            return new AttitudeCorrectionResult(new Vector(rotation), new Vector(Vector.Zero));
        }
    }

    // This assumes the rotor is connected with the base connected to the controller, not the rotating end.
    [kOS.Safe.Utilities.KOSNomenclature("AttitudeController", KOSToCSharp = false)]
    class AttitudeControllerRotor : AttitudeController
    {
        public AttitudeControllerRotor(PartValue part, PartModuleFields module, ModuleRoboticServoRotor rotor)
            : base(part, module, null)
        {
            this.rotor = rotor;
        }

        protected ModuleRoboticServoRotor rotor { get; private set; }

        public override string ControllerType { get { return "ROTOR"; } }
        public override AttitudeCorrectionResult positiveRotation { get { return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero); } }
        public override AttitudeCorrectionResult negativeRotation { get { return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero); } }
        public override bool HasCustomThrottle { get { return true; } }
        public override float CustomThrottle { get { return rotor.maxTorque; } set { rotor.maxTorque = Math.Max(Math.Min(value, 100), 0); } }
        public override string Status { get { return rotor.motorState.ToUpper(); } }
        public override float ResponseTime { get { return rotor.rotorSpoolTime; } set { } }

        public override AttitudeCorrectionResult GetResponseFor(float pitch, float yaw, float roll, float x, float y, float z, float throttle)
        {
            if (!rotor.servoIsMotorized)
                return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero);
            // Likely makes too many assumptions about the axis direction but the rotation axis does not seem to be public.
            Quaternion rotator = rotor.servoTransformRotation;
            Vector3d facing = Part.GetFacing().Vector;
            Vector3 axis = new Vector3((float)facing.x, (float)facing.y, (float)facing.z);
            float pitchAlignment = Vector3.Dot(axis, StarVector);
            float yawAlignment = Vector3.Dot(axis, TopVector);
            float rollAlignment = Vector3.Dot(axis, ForeVector);
            Vector3 rotationEffect = new Vector3(pitchAlignment, yawAlignment, rollAlignment);

            if (rotor.rotateCounterClockwise)
                rotationEffect *= -1.0f;
            rotationEffect *= rotor.servoMotorSize;
            rotationEffect *= throttle / 100;
            return new AttitudeCorrectionResult(new Vector(rotationEffect), new Vector(0, 0, 0));
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("AttitudeController", KOSToCSharp = false)]
    class AttitudeControllerEngine : AttitudeController
    {
        public AttitudeControllerEngine(PartValue part, PartModuleFields module)
            : base(part, module, null)
        {
            // This assumes that:
            // - nozzles are controlled by at most one gimbal
            // - even though multiple nozzles are controlled by one gimbal, they can rotate independently
            // Gimbal assignment is not accurate but I can't find a good way to do it. It succesfully falls back to the first gimbal for now.

            var maxFound = new Dictionary<ModuleEngines, float>();
            gimbals = part.Part.Modules.OfType<ModuleGimbal>().ToList();
            gimbalLinking = new Dictionary<ModuleEngines, ModuleGimbal>();
            foreach (var engine in part.Part.Modules.OfType<ModuleEngines>())
            {
                gimbalLinking.Add(engine, null);
                maxFound[engine] = 0;
                if (gimbals.Any()) // Assign first gimbal by default
                    gimbalLinking[engine] = gimbals.First();
            }
            foreach (var gimbal in gimbals)
            {
                if (gimbal.engineMultsList == null)
                    continue;
                foreach (var sublist in gimbal.engineMultsList)
                {
                    if (sublist == null)
                        continue;
                    foreach (var kv in sublist)
                    {
                        var engine = kv.Key;
                        var strength = kv.Value;
                        if (strength < 0.01)
                            continue;
                        if (!gimbalLinking.ContainsKey(engine) || gimbalLinking[engine] == null)
                        {
                            gimbalLinking[engine] = gimbal;
                            maxFound[engine] = 0;
                        }

                        if (maxFound[engine] < strength)
                        {
                            gimbalLinking[engine] = gimbal;
                            maxFound[engine] = strength;
                        }
                    }
                }
            }
        }
        protected List<ModuleGimbal> gimbals { get; private set; }
        protected Dictionary<ModuleEngines, ModuleGimbal> gimbalLinking { get; private set; }

        public override bool AllowPitch
        {
            get
            {
                foreach (var gimbal in gimbals)
                    if (gimbal.enablePitch)
                        return true;
                return false;
            }
            set
            {
                foreach (var gimbal in gimbals)
                    gimbal.enablePitch = value;
            }
        }
        public override bool AllowRoll
        {
            get
            {
                foreach (var gimbal in gimbals)
                    if (gimbal.enableRoll)
                        return true;
                return false;
            }
            set
            {
                foreach (var gimbal in gimbals)
                    gimbal.enableRoll = value;
            }
        }
        public override bool AllowYaw
        {
            get
            {
                foreach (var gimbal in gimbals)
                    if (gimbal.enableYaw)
                        return true;
                return false;
            }
            set
            {
                foreach (var gimbal in gimbals)
                    gimbal.enableYaw = value;
            }
        }
        public override AttitudeCorrectionResult positiveRotation { get { return GetResponseFor(1, 1, 1, 0, 0, 0, CustomThrottle); } }
        public override AttitudeCorrectionResult negativeRotation { get { return GetResponseFor(-1, -1, -1, 0, 0, 0, CustomThrottle); } }
        public override string ControllerType { get { return "ENGINE"; } }

        public override bool HasCustomThrottle
        {
            get
            {
                var engines = gimbalLinking.Keys;
                foreach (var engine in engines)
                    if (engine.independentThrottle)
                        return true;
                return false;
            }
        }
        public override float CustomThrottle
        {
            get
            {
                var engines = gimbalLinking.Keys;
                if (engines.Count == 0)
                    return 0;
                return engines.Select((e) => e.independentThrottlePercentage).Average();
            }
            set
            {
                var engines = gimbalLinking.Keys;
                foreach (var engine in engines)
                    engine.independentThrottlePercentage = Math.Max(Math.Min(value, 100), 0);
            }
        }
        public override float ResponseTime
        {
            get
            {
                var engines = gimbalLinking.Keys;
                if (engines.Count == 0)
                    return 0;
                return engines.Select((e) => e.throttleResponseRate).Average();
            }
        }

        public override AttitudeCorrectionResult GetResponseFor(float pitch, float yaw, float roll, float x, float y, float z, float throttle)
        {
            // Assumes that engines burn to Facing * gimbal
            // Assumes that gimbals have equal freedom in both axis

            var engines = gimbalLinking.Keys;
            if (engines.Count == 0)
                return new AttitudeCorrectionResult(new Vector(0, 0, 0), new Vector(0, 0, 0));
            Vector3d facing3d = Part.GetFacing().Vector;
            Vector3 facing = new Vector3((float)facing3d.x, (float)facing3d.y, (float)facing3d.z);


            float shipThrottle = (float)kOSVesselModule.GetInstance(Part.Shared.Vessel).GetFlightControlParameter("throttle").GetValue();

            var result = new AttitudeCorrectionResult(new Vector(0, 0, 0), new Vector(0, 0, 0));

            foreach (var engine in engines)
            {
                if (engine.thrustTransforms.Count == 0)
                    continue;

                float engineThrottle = throttle / 100;
                if (!engine.independentThrottle)
                    throttle = shipThrottle;

                float thrust = engine.GetThrust(null, true, engineThrottle);
                // I assume this is supposed to be normalized
                float multiplierSum = engine.thrustTransformMultipliers.Sum();
                if (multiplierSum < 0.01)
                    multiplierSum = 1; // Handle all zero multipliers, assume equal thrust
                float thrustPerNozzle = thrust / engine.thrustTransforms.Count;


                for (int i = 0; i < engine.thrustTransforms.Count; i++)
                {
                    float scaledMultiplier = engine.thrustTransformMultipliers[i] / multiplierSum;
                    if (scaledMultiplier < 0.01)
                        scaledMultiplier = 1; // Handle all zero multipliers, assume equal thrust
                    float nozzleThrust = thrustPerNozzle * scaledMultiplier;

                    Vector3 thrustPosition = engine.thrustTransforms[i].position;

                    Vector3 thrustVector = facing;
                    Vector3 lever = PositionToLever(thrustPosition);
                    Vector3 pitchVector = PitchVector(lever);
                    Vector3 yawVector = YawVector(lever);
                    Vector3 rollVector = RollVector(lever);

                    if (gimbalLinking[engine] != null)
                    {
                        var gimbal = gimbalLinking[engine];

                        float maskedPitch = gimbal.enablePitch ? pitch : 0;
                        float maskedYaw = gimbal.enableYaw ? yaw : 0;
                        float maskedRoll = gimbal.enableRoll ? roll : 0;
                        Vector3 desiredDirection = maskedPitch * pitchVector + maskedYaw * yawVector + maskedRoll * rollVector;
                        if (desiredDirection.magnitude > 0.01)
                            desiredDirection = desiredDirection.normalized;

                        Vector3 desiredGimbalResponse = Vector3.ProjectOnPlane(desiredDirection, facing);
                        float rangeDegrees = gimbal.gimbalRange * gimbal.gimbalLimiter / 100;
                        float maxDeflection = (float)Math.Tan(rangeDegrees / 180 * Math.PI);
                        if (desiredGimbalResponse.magnitude > maxDeflection)
                        {
                            desiredGimbalResponse = desiredGimbalResponse.normalized * maxDeflection;
                        }
                        thrustVector = (desiredGimbalResponse + facing * (1 - desiredGimbalResponse.magnitude)).normalized; //readd main engine thrust
                    }
                    thrustVector *= nozzleThrust;

                    result += SplitComponents(lever, thrustVector);
                }
            }

            return result;
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("AttitudeController", KOSToCSharp = false)]
    class AttitudeControllerDrainValve : AttitudeController
    {
        public AttitudeControllerDrainValve(PartValue part, PartModuleFields module, ModuleResourceDrain drain)
            : base(part, module, null)
        {
            this.drain = drain;
        }

        protected ModuleResourceDrain drain { get; private set; }
        
        public override string ControllerType { get { return "DRAIN"; } }
        public override AttitudeCorrectionResult positiveRotation { get { return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero); } }
        public override AttitudeCorrectionResult negativeRotation { get { return new AttitudeCorrectionResult(Vector.Zero, Vector.Zero); } }
        public override AttitudeCorrectionResult GetResponseFor(float pitch, float yaw, float roll, float x, float y, float z, float throttle)
        {
            // Assumes thrust comes from the part position
            // Assumes all resources are drained (not always true)

            float ISP = 5.0f; // All resources in ResourcesGeneric.cfg have a drain ISP of 5.
            var parent = Part.Parent;
            float weightDelta = parent.Part.GetWetMass() - parent.Part.GetDryMass();
            float fuelFlow = weightDelta / 5; // Max throttle (20%) drains tank in 5 seconds

            float throttleFraction = Math.Min(Math.Max(throttle, 0), 20) / 20;
            float thrust = fuelFlow * ISP * 9.80665f * throttleFraction;

            Vector3d facing = Part.GetFacing().Vector;
            Vector3 lever = PositionToLever(Part.Part.transform.position);

            return SplitComponents(lever, facing * thrust);
        }
        public override bool HasCustomThrottle { get { return true; } }
        public override float CustomThrottle
        {
            get { return drain.drainRate; }
            set { drain.drainRate = Math.Min(Math.Max(value, 0), 20); }
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("AttitudeController", KOSToCSharp = false)]
    class AttitudeControllerRCS : AttitudeController
    {
        public AttitudeControllerRCS(PartValue part, PartModuleFields module, ModuleRCS rcs)
            : base(part, module, rcs)
        {
            this.rcs = rcs;
        }

        protected ModuleRCS rcs { get; private set; }
        
        public override string ControllerType { get { return "RCS"; } }
        public override bool AllowPitch { get { return rcs.enablePitch; } set { rcs.enablePitch = value; } }
        public override bool AllowRoll { get { return rcs.enableRoll; } set { rcs.enableRoll = value; } }
        public override bool AllowYaw { get { return rcs.enableYaw; } set { rcs.enableYaw = value; } }
        public override bool AllowX { get { return rcs.enableX; } set { rcs.enableX = value; } }
        public override bool AllowY { get { return rcs.enableY; } set { rcs.enableY = value; } }
        public override bool AllowZ { get { return rcs.enableZ; } set { rcs.enableZ = value; } }
        public override AttitudeCorrectionResult GetResponseFor(float pitch, float yaw, float roll, float x, float y, float z, float throttle)
        {
            var result = new AttitudeCorrectionResult(new Vector(0, 0, 0), new Vector(0, 0, 0));
            if (Part.Part.ShieldedFromAirstream || !rcs.rcsEnabled || !rcs.isEnabled ||
                rcs.isJustForShow || rcs.flameout || !rcs.rcs_active)
                return result;

            var maskedPitch = rcs.enablePitch ? pitch : 0;
            var maskedYaw = rcs.enableYaw ? yaw : 0;
            var maskedRoll = rcs.enableRoll ? roll : 0;
            var maskedX = rcs.enableX ? x : 0;
            var maskedY = rcs.enableY ? y : 0;
            var maskedZ = rcs.enableZ ? z : 0;

            foreach (var transform in rcs.thrusterTransforms)
            {
                Vector3 rcsPosFromCoM = transform.position - Part.Part.vessel.CurrentCoM;
                Vector3 rcsThrustDir = rcs.useZaxis ? -transform.forward : transform.up;
                float powerFactor = rcs.thrusterPower * rcs.thrustPercentage * 0.01f;
                // Normally you'd check for precision mode to nerf powerFactor here,
                // but kOS doesn't obey that.
                Vector3 thrust = powerFactor * rcsThrustDir;
                Vector3 torque = Vector3d.Cross(rcsPosFromCoM, thrust);
                Vector3 translation = rcsPosFromCoM.normalized * (float)Vector3d.Dot(rcsPosFromCoM.normalized, thrust);
                Vector3 transformedTorque = Part.Part.vessel.ReferenceTransform.InverseTransformDirection(torque);
                Vector3 transformedTranslation = Part.Part.vessel.ReferenceTransform.InverseTransformDirection(translation);

                Vector3 desiredTorque = new Vector3(maskedPitch, maskedRoll, maskedYaw);
                Vector3 desiredTranslation = new Vector3(maskedX, maskedY, maskedZ);

                // Big assumption here about how ksp prioritizes conflicing inputs
                float alignment = Vector3.Dot(transformedTorque.normalized, desiredTorque) +
                                  Vector3.Dot(transformedTranslation.normalized, desiredTranslation);
                
                // This simulates a bang-bang rcs, which makes it work for the positiveRotation case and the V(0,0,0) case.
                // KSP seems to use partial thrust though, unsure how to model this.
                if (alignment > 0)
                {
                    result += new AttitudeCorrectionResult(
                        new Vector(transformedTorque.x, transformedTorque.z, transformedTorque.y),
                        new Vector(transformedTranslation.x, transformedTranslation.z, transformedTranslation.y)
                        );
                }
            }
            return result;
        }
    }
}
