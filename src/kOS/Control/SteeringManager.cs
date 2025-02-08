using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using kOS.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Math = System.Math;

namespace kOS.Control
{
    [kOS.Safe.Utilities.KOSNomenclature("SteeringManager")]
    public class SteeringManager : Structure, IDisposable, IFlightControlParameter
    {
        public static void Copy(SteeringManager origin, SteeringManager destination)
        {
            destination.ShowAngularVectors = origin.ShowAngularVectors;
            destination.ShowFacingVectors = origin.ShowFacingVectors;
            destination.ShowSteeringStats = origin.ShowSteeringStats;
            destination.WriteCSVFiles = origin.WriteCSVFiles;
            destination.MaxStoppingTime = origin.MaxStoppingTime;
            destination.RollControlAngleRange = origin.RollControlAngleRange;

            destination.pitchPI.Ts = origin.pitchPI.Ts;
            destination.yawPI.Ts = origin.yawPI.Ts;
            destination.rollPI.Ts = origin.rollPI.Ts;
            destination.pitchPI.Loop = PIDLoop.DeepCopy(origin.pitchPI.Loop);
            destination.yawPI.Loop = PIDLoop.DeepCopy(origin.yawPI.Loop);
            destination.rollPI.Loop = PIDLoop.DeepCopy(origin.rollPI.Loop);

            destination.pitchRatePI = PIDLoop.DeepCopy(origin.pitchRatePI);
            destination.yawRatePI = PIDLoop.DeepCopy(origin.yawRatePI);
            destination.rollRatePI = PIDLoop.DeepCopy(origin.rollRatePI);

            destination.PitchTorqueAdjust = origin.PitchTorqueAdjust;
            destination.PitchTorqueFactor = origin.PitchTorqueFactor;
            destination.RollTorqueAdjust = origin.RollTorqueAdjust;
            destination.RollTorqueFactor = origin.RollTorqueFactor;
            destination.YawTorqueAdjust = origin.YawTorqueAdjust;
            destination.YawTorqueFactor = origin.YawTorqueFactor;
        }

        public bool FightsWithSas { get { return true; } }

        private Vessel internalVessel;

        public Vessel Vessel
        {
            get
            {
                return internalVessel;
            }
        }

        private SharedObjects shared;

        private uint partId = 0;

        public uint PartId { get { return partId; } }

        private bool enabled = false;

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                if (enabled)
                {
                    ResetIs();
                }
                else
                {
                    UpdateVectorRenders();

                    if (pitchRateWriter != null) pitchRateWriter.Dispose();
                    if (yawRateWriter != null) yawRateWriter.Dispose();
                    if (rollRateWriter != null) rollRateWriter.Dispose();
                    if (pitchTorqueWriter != null) pitchTorqueWriter.Dispose();
                    if (yawTorqueWriter != null) yawTorqueWriter.Dispose();
                    if (rollTorqueWriter != null) rollTorqueWriter.Dispose();
                    if (adjustTorqueWriter != null) adjustTorqueWriter.Dispose();

                    pitchRateWriter = null;
                    yawRateWriter = null;
                    rollRateWriter = null;
                    pitchTorqueWriter = null;
                    yawTorqueWriter = null;
                    rollTorqueWriter = null;
                    adjustTorqueWriter = null;
                }
            }
        }

        public bool ShowFacingVectors { get; set; }

        public bool ShowAngularVectors { get; set; }

        public bool ShowSteeringStats { get; set; }

        public bool WriteCSVFiles { get; set; }

        private const string FILE_BASE_NAME = "{0}-{1}-{2}.csv";
        private readonly string fileDateString = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

        public Direction Value { get; set; }

        public Direction TargetDirection { get { return Value == null ? new Direction() : Value; } }

        private Transform vesselTransform;

        private readonly TorquePI pitchPI = new TorquePI();
        private readonly TorquePI yawPI = new TorquePI();
        private readonly TorquePI rollPI = new TorquePI();

        private PIDLoop pitchRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);
        private PIDLoop yawRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);
        private PIDLoop rollRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);

        private KSP.IO.TextWriter pitchRateWriter;
        private KSP.IO.TextWriter yawRateWriter;
        private KSP.IO.TextWriter rollRateWriter;

        private KSP.IO.TextWriter pitchTorqueWriter;
        private KSP.IO.TextWriter yawTorqueWriter;
        private KSP.IO.TextWriter rollTorqueWriter;

        private KSP.IO.TextWriter adjustTorqueWriter;

        private readonly MovingAverage pitchTorqueCalc = new MovingAverage { SampleLimit = 15 };
        private readonly MovingAverage yawTorqueCalc = new MovingAverage { SampleLimit = 15 };
        private readonly MovingAverage rollTorqueCalc = new MovingAverage { SampleLimit = 15 };

        private bool EnableTorqueAdjust { get; set; }

        public MovingAverage AverageDuration = new MovingAverage { SampleLimit = 60 };

        #region doubles

        public const double RadToDeg = 180d / Math.PI;

        private const double CONTROLEPSILON = 1e-16;
        private const double TORQUEPIDEPSILON = 1e-6;

        private double sessionTime = double.MaxValue;
        private double lastSessionTime = double.MaxValue;

        public double MaxStoppingTime { get; set; }
        private double rollControlAngleRange;
        public double RollControlAngleRange
        {
            get
            {
                return rollControlAngleRange;
            }
            set
            {
                rollControlAngleRange = Math.Max(CONTROLEPSILON, Math.Min(180, value));
            }
        }

        private double accPitch = 0;
        private double accYaw = 0;
        private double accRoll = 0;
        private double torquePIDEpsilonMin; // really being precise here, but users can make this bigger to make it use less RCS.
        private double torquePIDEpsilonMax; // when it's totally off, as long as it's within this many degrees per second of the right rate, good enough.

        /// <summary>A value between rotationEpsilonMin and rotationEpsilonMax for the current epsilon to feed the PID for pitch</summary>
        private double lerpedPitchEpsilon = 0.0;
        /// <summary>A value between rotationEpsilonMin and rotationEpsilonMax for the current epsilon to feed the PID for yaw</summary>
        private double lerpedYawEpsilon = 0.0;
        /// <summary>A value between rotationEpsilonMin and rotationEpsilonMax for the current epsilon to feed the PID for roll</summary>
        private double lerpedRollEpsilon = 0.0;

        private double phi;
        private double phiPitch;
        private double phiYaw;
        private double phiRoll;

        private double maxPitchOmega;
        private double maxYawOmega;
        private double maxRollOmega;

        private double tgtPitchOmega;
        private double tgtYawOmega;
        private double tgtRollOmega;

        private double tgtPitchTorque;
        private double tgtYawTorque;
        private double tgtRollTorque;

        private const double RENDER_MULTIPLIER = 50;

        public double PitchTorqueAdjust { get; set; }
        public double YawTorqueAdjust { get; set; }
        public double RollTorqueAdjust { get; set; }

        public double PitchTorqueFactor { get; set; }
        public double YawTorqueFactor { get; set; }
        public double RollTorqueFactor { get; set; }

        private int vesselParts;

        #endregion doubles
        
        private readonly Dictionary<PartModule, ITorqueProvider> torqueProviders = new Dictionary<PartModule, ITorqueProvider>();

        private Quaternion vesselRotation;
        private Quaternion targetRot;

        #region Vectors

        private Vector3d centerOfMass;
        private Vector3d vesselUp;

        private Vector3d vesselForward;
        private Vector3d vesselTop;
        private Vector3d vesselStarboard;

        private Vector3d targetForward;
        private Vector3d targetTop;
        private Vector3d targetStarboard;

        private Vector3d adjustTorque;

        private Vector3d omega = Vector3d.zero; // x: pitch, y: yaw, z: roll
        private Vector3d lastOmega = Vector3d.zero;
        private Vector3d angularAcceleration = Vector3d.zero;
        private Vector3d momentOfInertia = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d measuredMomentOfInertia = Vector3d.zero;
        private Vector3d measuredTorque = Vector3d.zero;
        private Vector3d controlTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d rawTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll

        #endregion Vectors

        #region VectorRenderers

        private VectorRenderer vForward;
        private VectorRenderer vTop;
        private VectorRenderer vStarboard;

        private VectorRenderer vTgtForward;
        private VectorRenderer vTgtTop;
        private VectorRenderer vTgtStarboard;

        private VectorRenderer vWorldX;
        private VectorRenderer vWorldY;
        private VectorRenderer vWorldZ;

        private VectorRenderer vOmegaX;
        private VectorRenderer vOmegaY;
        private VectorRenderer vOmegaZ;

        private VectorRenderer vTgtTorqueX;
        private VectorRenderer vTgtTorqueY;
        private VectorRenderer vTgtTorqueZ;

        #endregion VectorRenderers

        public SteeringManager(SharedObjects sharedObj) : this(sharedObj.Vessel)
        {
        }

        public SteeringManager(Vessel vessel)
        {
            internalVessel = vessel;
            ShowFacingVectors = false;
            ShowAngularVectors = false;
            ShowSteeringStats = false;
            WriteCSVFiles = false;

            if (pitchRateWriter != null) pitchRateWriter.Dispose();
            if (yawRateWriter != null) yawRateWriter.Dispose();
            if (rollRateWriter != null) rollRateWriter.Dispose();

            if (pitchTorqueWriter != null) pitchTorqueWriter.Dispose();
            if (yawTorqueWriter != null) yawTorqueWriter.Dispose();
            if (rollTorqueWriter != null) rollTorqueWriter.Dispose();

            if (adjustTorqueWriter != null) adjustTorqueWriter.Dispose();

            pitchRateWriter = null;
            yawRateWriter = null;
            rollRateWriter = null;

            pitchTorqueWriter = null;
            yawTorqueWriter = null;
            rollTorqueWriter = null;

            adjustTorqueWriter = null;

            ResetToDefault();

            InitializeSuffixes();
        }

        public void ResetToDefault()
        {
            pitchPI.Ts = 2;
            yawPI.Ts = 2;
            rollPI.Ts = 2;
            // only neet to reset the PI's I value, other values are not accessible to users to modify
            pitchPI.ResetI();
            yawPI.ResetI();
            rollPI.ResetI();

            pitchRatePI.Kp = 1;
            pitchRatePI.Ki = 0.1;
            pitchRatePI.Kd = 0;
            yawRatePI.Kp = 1;
            yawRatePI.Ki = 0.1;
            yawRatePI.Kd = 0;
            rollRatePI.Kp = 1;
            rollRatePI.Ki = 0.1;
            rollRatePI.Kd = 0;

            adjustTorque = Vector3d.zero;

            EnableTorqueAdjust = false;

            MaxStoppingTime = 2;
            RollControlAngleRange = 5;

            torquePIDEpsilonMin = 0.0002d;
            torquePIDEpsilonMax = 0.001d;

            PitchTorqueAdjust = 0;
            YawTorqueAdjust = 0;
            RollTorqueAdjust = 0;

            PitchTorqueFactor = 1;
            YawTorqueFactor = 1;
            RollTorqueFactor = 1;
        }

        public void InitializeSuffixes()
        {
            AddSuffix("PITCHPID", new Suffix<PIDLoop>(() => pitchRatePI));
            AddSuffix("YAWPID", new Suffix<PIDLoop>(() => yawRatePI));
            AddSuffix("ROLLPID", new Suffix<PIDLoop>(() => rollRatePI));
            AddSuffix("ENABLED", new Suffix<BooleanValue>(() => Enabled));
            AddSuffix("TARGET", new Suffix<Direction>(() => TargetDirection));
            AddSuffix("RESETPIDS", new NoArgsVoidSuffix(ResetIs));
            AddSuffix("RESETTODEFAULT", new NoArgsVoidSuffix(ResetToDefault));
            AddSuffix("SHOWFACINGVECTORS", new SetSuffix<BooleanValue>(() => ShowFacingVectors, value => ShowFacingVectors = value));
            AddSuffix("SHOWANGULARVECTORS", new SetSuffix<BooleanValue>(() => ShowAngularVectors, value => ShowAngularVectors = value));
            AddSuffix("SHOWSTEERINGSTATS", new SetSuffix<BooleanValue>(() => ShowSteeringStats, value => ShowSteeringStats = value));
            AddSuffix("WRITECSVFILES", new SetSuffix<BooleanValue>(() => WriteCSVFiles, value => WriteCSVFiles = value));
            AddSuffix("PITCHTS", new SetSuffix<ScalarValue>(() => pitchPI.Ts, value => pitchPI.Ts = value));
            AddSuffix("YAWTS", new SetSuffix<ScalarValue>(() => yawPI.Ts, value => yawPI.Ts = value));
            AddSuffix("ROLLTS", new SetSuffix<ScalarValue>(() => rollPI.Ts, value => rollPI.Ts = value));
            AddSuffix("TORQUEEPSILONMIN", new SetSuffix<ScalarValue>(() => GetTorqueEpsilonMin(), SetTorqueEpsilonMin));
            AddSuffix("TORQUEEPSILONMAX", new SetSuffix<ScalarValue>(() => GetTorqueEpsilonMax(), SetTorqueEpsilonMax));
            AddSuffix("MAXSTOPPINGTIME", new SetSuffix<ScalarValue>(() => MaxStoppingTime, value => MaxStoppingTime = value));
            AddSuffix("ANGLEERROR", new Suffix<ScalarValue>(() => phi * RadToDeg));
            AddSuffix("PITCHERROR", new Suffix<ScalarValue>(() => phiPitch * RadToDeg));
            AddSuffix("YAWERROR", new Suffix<ScalarValue>(() => phiYaw * RadToDeg));
            AddSuffix("ROLLERROR", new Suffix<ScalarValue>(() => phiRoll * RadToDeg));
            AddSuffix("PITCHTORQUEADJUST", new SetSuffix<ScalarValue>(() => PitchTorqueAdjust, value => PitchTorqueAdjust = value));
            AddSuffix("YAWTORQUEADJUST", new SetSuffix<ScalarValue>(() => YawTorqueAdjust, value => YawTorqueAdjust = value));
            AddSuffix("ROLLTORQUEADJUST", new SetSuffix<ScalarValue>(() => RollTorqueAdjust, value => RollTorqueAdjust = value));
            AddSuffix("PITCHTORQUEFACTOR", new SetSuffix<ScalarValue>(() => PitchTorqueFactor, value => PitchTorqueFactor = value));
            AddSuffix("YAWTORQUEFACTOR", new SetSuffix<ScalarValue>(() => YawTorqueFactor, value => YawTorqueFactor = value));
            AddSuffix("ROLLTORQUEFACTOR", new SetSuffix<ScalarValue>(() => RollTorqueFactor, value => RollTorqueFactor = value));
            AddSuffix("AVERAGEDURATION", new Suffix<ScalarValue>(() => AverageDuration.Mean));
            AddSuffix("ROLLCONTROLANGLERANGE", new SetSuffix<ScalarValue>(() => RollControlAngleRange, value => RollControlAngleRange = value));
            AddSuffix("MOI", new Suffix<Vector>(() => new Vector(momentOfInertia)));
            AddSuffix("ACTUATION", new Suffix<Vector>(() => new Vector(accPitch, accRoll, accYaw)));
            AddSuffix("CONTROLTORQUE", new Suffix<Vector>(() => new Vector(controlTorque)));
            AddSuffix("MEASUREDTORQUE", new Suffix<Vector>(() => new Vector(measuredTorque)));
            AddSuffix("RAWTORQUE", new Suffix<Vector>(GetRawTorque));
            AddSuffix("ADJUSTTORQUE", new Suffix<Vector>(() => new Vector(adjustTorque)));
            AddSuffix("TARGETTORQUE", new Suffix<Vector>(() => new Vector(tgtPitchTorque, tgtRollTorque, tgtYawTorque)));
            AddSuffix("ANGULARVELOCITY", new Suffix<Vector>(() => new Vector(omega)));
            AddSuffix("ANGULARACCELERATION", new Suffix<Vector>(() => new Vector(angularAcceleration)));
            AddSuffix("ENABLETORQUEADJUST", new SetSuffix<BooleanValue>(() => EnableTorqueAdjust, value => EnableTorqueAdjust = value));
        }

        public void EnableControl(SharedObjects sharedObj)
        {
            shared = sharedObj;
            partId = sharedObj.KSPPart.flightID;
            ResetIs();
            Enabled = true;
            lastSessionTime = double.MaxValue;
            pitchTorqueCalc.Reset();
            rollTorqueCalc.Reset();
            yawTorqueCalc.Reset();

            adjustTorque = Vector3d.zero;
        }

        public void DisableControl()
        {
            shared = null;
            partId = 0;
            Enabled = false;
        }

        public void DisableControl(SharedObjects sharedObj)
        {
            if (enabled && partId != sharedObj.KSPPart.flightID)
            {
                // trying to disable control from a part that didn't win control last update, ignore.
                return;
            }
            DisableControl();
        }

        public VectorRenderer InitVectorRenderer(Color c, double width, SharedObjects sharedObj)
        {
            VectorRenderer rend = new VectorRenderer(sharedObj.UpdateHandler, sharedObj)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = Vector3d.zero,
                Vector = Vector3d.zero,
                Width = width
            };
            return rend;
        }

        private void ResetIs()
        {
            pitchPI.ResetI();
            yawPI.ResetI();
            rollPI.ResetI();
            pitchRatePI.ResetI();
            yawRatePI.ResetI();
            rollRatePI.ResetI();
        }

        public void OnFlyByWire(FlightCtrlState c)
        {
            Update(c);
        }

        private readonly System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        // Set it as if it was in Degrees when it's not, and Prevent setting Max val smaller than Min val:
        private void SetTorqueEpsilonMin(ScalarValue newVal)
        {
            torquePIDEpsilonMin = newVal;
            if (torquePIDEpsilonMin > torquePIDEpsilonMax)
                torquePIDEpsilonMax = torquePIDEpsilonMin;
        }
        // Report it as if it was in Degrees when it's not:
        private double GetTorqueEpsilonMin()
        {
            return torquePIDEpsilonMin;
        }

        // Set it as if it was in Degrees when it's not, and Prevent setting Max val smaller than Min val:
        private void SetTorqueEpsilonMax(ScalarValue newVal)
        {
            torquePIDEpsilonMax = newVal;
            if (torquePIDEpsilonMax < torquePIDEpsilonMin)
                torquePIDEpsilonMin = torquePIDEpsilonMax;
        }
        // Report it as if it was in Degrees when it's not:
        private double GetTorqueEpsilonMax()
        {
            return torquePIDEpsilonMax;
        }

        private Vector GetRawTorque()
        {
            UpdateTorque();
            return new Vector(rawTorque);
        }

        private void Update(FlightCtrlState c)
        {
            if (!Enabled)
            {
                return; // skip update if not enabled
            }
            if (Value == null)
            {
                SafeHouse.Logger.SuperVerbose("SteeringManager.Update: Value is <null>");
                return;
            }

            sw.Reset();
            sw.Start();
            lastSessionTime = sessionTime;
            sessionTime = shared.UpdateHandler.CurrentFixedTime;
            if (sessionTime - lastSessionTime > 1) ResetIs();
            //if (sessionTime > lastSessionTime)
            //{
            //}
            if (shared.Vessel.ActionGroups[KSPActionGroup.SAS])
            {
                UpdateStateVectors();
                UpdateControl(c);
                UpdateVectorRenders();
            }
            else
            {
                UpdateStateVectors();
                UpdateControlParts();
                UpdateTorque();
                UpdatePredictionPI();
                UpdateControl(c);
                if (ShowSteeringStats) PrintDebug();
                if (WriteCSVFiles) WriteCSVs();
                UpdateVectorRenders();
            }
            sw.Stop();
            AverageDuration.Update((double)sw.ElapsedTicks / (double)System.TimeSpan.TicksPerMillisecond);
        }

        private Direction GetDirectionFromValue(object value)
        {
            // The vesselUp vector is used when calculating the "up" vector of the direction, and it
            // is OK that it's a stale reference since LookRotation only uses the projection of the
            // vector anyways (essentially the "up" vector, excluding the "look at" vector).
            if (value is Direction)
                return (Direction)value;
            else if (value is Vector)
                return Direction.LookRotation((Vector)value, vesselUp);
            else if (value is Node)
                return Direction.LookRotation(((Node)value).GetBurnVector(), vesselUp);
            else if (value is string || value is StringValue)
            {
                if (string.Equals(value.ToString(), "KILL", StringComparison.OrdinalIgnoreCase))
                {
                    // For the value of "KILL" keep the vessel rotation pointed at the previous rotation.
                    // This value is intentionally stale from the last update, with the hope that it will
                    // lead to quicker stopping (essentially each update frame become a bisection).  It
                    // may however cause oscillation, so there may be need in the future to update the
                    // rotation instead.
                    return new Direction(vesselRotation);
                }
            }
            // Bindings pass primatives instead of encapsulated values, so we want to encapsulate the value
            // before throwing a type exception so that we can report the urser friendly type, instead of C#.
            value = Structure.FromPrimitive(value);
            throw new Safe.Exceptions.KOSWrongControlValueTypeException(
                "STEERING", KOSNomenclature.GetKOSName(value.GetType()), "Direction, Vector, Maneuver Node, or special string \"KILL\"");
        }

        public void UpdateStateVectors()
        {
            if (Value == null)
            {
                // There is a single dead frame where the steering manager is enabled, but the trigger has not
                // yet been run to update Value.  Skip the update in these cases, but log it in case we find a
                // time where the trigger and steering manager are no longer synced.
                shared.Logger.LogWarning("SteeringManager target direction is null, skipping this update.");
                return;
            }

            targetRot = Value.Rotation;
            centerOfMass = shared.Vessel.CoMD;

            vesselTransform = shared.Vessel.ReferenceTransform;
            // Found that the default rotation has top pointing forward, forward pointing down, and right pointing starboard.
            // This fixes that rotation.
            vesselRotation = vesselTransform.rotation * Quaternion.Euler(-90, 0, 0);

            vesselForward = vesselRotation * Vector3d.forward;
            vesselTop = vesselRotation * Vector3d.up;
            vesselStarboard = vesselRotation * Vector3d.right;
            vesselUp = (centerOfMass - shared.Vessel.mainBody.position).normalized;

            targetForward = targetRot * Vector3d.forward;
            targetTop = targetRot * Vector3d.up;
            targetStarboard = targetRot * Vector3d.right;

            Vector3d oldOmega = omega;
            // omega is angular rotation.  need to correct the signs to agree with the facing axis
            omega = Quaternion.Inverse(vesselRotation) * shared.Vessel.GetComponent<Rigidbody>().angularVelocity;
            omega.x *= -1; //positive values pull the nose to the starboard.
            //omega.y *= -1; // positive values pull the nose up.
            omega.z *= -1; // positive values pull the starboard side up.

            // TODO: Currently adjustments to MOI are only enabled in debug compiles.  Using this feature seems to be buggy, but it has potential
            // to be more resilient against random spikes in angular velocity.
            if (sessionTime > lastSessionTime)
            {
                double dt = sessionTime - lastSessionTime;
                angularAcceleration = (omega - oldOmega) / dt;
                angularAcceleration = new Vector3d(angularAcceleration.x, angularAcceleration.z, angularAcceleration.y);
            }
            
            // TODO: If stock vessel.MOI stops being so weird, we might be able to change the following line
            // into this instead.  (See the comment on FindMOI()'s header):
            //      momentOfInertia = shared.Vessel.MOI;
            momentOfInertia = FindMoI(); 

            adjustTorque = Vector3d.zero;
            measuredTorque = Vector3d.Scale(momentOfInertia, angularAcceleration);

            if (sessionTime > lastSessionTime && EnableTorqueAdjust)
            {
                if (Math.Abs(accPitch) > CONTROLEPSILON)
                {
                    adjustTorque.x = Math.Min(Math.Abs(pitchTorqueCalc.Update(measuredTorque.x / accPitch)) - rawTorque.x, 0);
                    //adjustTorque.x = Math.Abs(pitchTorqueCalc.Update(measuredTorque.x / accPitch) / rawTorque.x);
                }
                else adjustTorque.x = Math.Abs(pitchTorqueCalc.Update(pitchTorqueCalc.Mean));
                if (Math.Abs(accYaw) > CONTROLEPSILON)
                {
                    adjustTorque.z = Math.Min(Math.Abs(yawTorqueCalc.Update(measuredTorque.z / accYaw)) - rawTorque.z, 0);
                    //adjustTorque.z = Math.Abs(yawTorqueCalc.Update(measuredTorque.z / accYaw) / rawTorque.z);
                }
                else adjustTorque.z = Math.Abs(yawTorqueCalc.Update(yawTorqueCalc.Mean));
                if (Math.Abs(accRoll) > CONTROLEPSILON)
                {
                    adjustTorque.y = Math.Min(Math.Abs(rollTorqueCalc.Update(measuredTorque.y / accRoll)) - rawTorque.y, 0);
                    //adjustTorque.y = Math.Abs(rollTorqueCalc.Update(measuredTorque.y / accRoll) / rawTorque.y);
                }
                else adjustTorque.y = Math.Abs(rollTorqueCalc.Update(rollTorqueCalc.Mean));
            }
        }

        public void UpdateControlParts()
        {
            if (shared.Vessel.parts.Count != vesselParts)
            {
                vesselParts = shared.Vessel.parts.Count;
                torqueProviders.Clear();
                foreach (Part part in shared.Vessel.Parts)
                {
                    foreach (PartModule pm in part.Modules)
                    {
                        ITorqueProvider tp = pm as ITorqueProvider;
                        if (tp != null)
                        {
                            torqueProviders.Add(pm, tp);
                        }
                    }
                }
            }
        }

        public void UpdateTorque()
        {
            // controlTorque is the maximum amount of torque applied by setting a control to 1.0.
            controlTorque.Zero();
            rawTorque.Zero();
            Vector3d pitchControl = Vector3d.zero;
            Vector3d yawControl = Vector3d.zero;
            Vector3d rollControl = Vector3d.zero;

            Vector3 pos;
            Vector3 neg;
            foreach (var pm in torqueProviders.Keys)
            {
                var tp = torqueProviders[pm];
                CorrectedGetPotentialTorque(tp, out pos, out neg);
                // It is possible for the torque returned to be negative.  It's also possible
                // for the positive and negative actuation to differ.  Below averages the value
                // for positive and negative actuation in an attempt to compensate for some issues
                // of differing signs and asymmetric torque.
                rawTorque.x += (Math.Abs(pos.x) + Math.Abs(neg.x)) / 2;
                rawTorque.y += (Math.Abs(pos.y) + Math.Abs(neg.y)) / 2;
                rawTorque.z += (Math.Abs(pos.z) + Math.Abs(neg.z)) / 2;
            }

            rawTorque.x = (rawTorque.x + PitchTorqueAdjust) * PitchTorqueFactor;
            rawTorque.z = (rawTorque.z + YawTorqueAdjust) * YawTorqueFactor;
            rawTorque.y = (rawTorque.y + RollTorqueAdjust) * RollTorqueFactor;
            controlTorque = rawTorque + adjustTorque;
            //controlTorque = Vector3d.Scale(rawTorque, adjustTorque);
            //controlTorque = rawTorque;

            double minTorque = CONTROLEPSILON;
            if (controlTorque.x < minTorque) controlTorque.x = minTorque;
            if (controlTorque.y < minTorque) controlTorque.y = minTorque;
            if (controlTorque.z < minTorque) controlTorque.z = minTorque;
        }

        /// <summary>
        /// See https://github.com/KSP-KOS/KOS/issues/2814 for why this wrapper around KSP's API call exists.
        /// <para />
        /// </summary>
        void CorrectedGetPotentialTorque(ITorqueProvider tp, out Vector3 pos, out Vector3 neg)
        {
            if (tp is ModuleRCS)
            {
                // The stock call GetPotentialTorque is completely broken in the case of ModuleRCS.  So
                // this replaces it entirely until KSP ever fixes the bug that's been in their
                // bug list forever (probably won't get fixed).
                ModuleRCS rcs = tp as ModuleRCS;
                Part p = rcs.part;

                // This is the list of various reasons this RCS module might 
                // be suppressed right now.  It would be nice if all this
                // stuff flipped one common flag during Update for all the
                // rest of the code to check, but sadly that doesn't seem to
                // be the case and you have to check these things individually:
                if (p.ShieldedFromAirstream || !rcs.rcsEnabled || !rcs.isEnabled ||
                    rcs.isJustForShow || rcs.flameout || !rcs.rcs_active)
                {
                    pos = new Vector3(0f, 0f, 0f);
                    neg = new Vector3(0f, 0f, 0f);
                }
                else
                {
                    // The algorithm here is adapted from code in the MandatoryRCS mod
                    // that had to solve this same problem:

                    // Note the swapping of Y and Z axes to align with "part space":
                    Vector3 rotateEnables = new Vector3(rcs.enablePitch ? 1 : 0, rcs.enableRoll ? 1 : 0, rcs.enableYaw ? 1 : 0);
                    Vector3 translateEnables = new Vector3(rcs.enableX ? 1 : 0, rcs.enableZ ? 1 : 0, rcs.enableY ? 1 : 0);

                    pos = new Vector3(0f, 0f, 0f);
                    neg = new Vector3(0f, 0f, 0f);
                    for (int i = rcs.thrusterTransforms.Count-1; i >= 0; --i)
                    {
                        Transform rcsTransform = rcs.thrusterTransforms[i];

                        // Fixes github issue #2912:  As of KSP 1.11.x, RCS parts now use part variants.  To keep kOS
                        // from counting torque as if the superset of all variant nozzles were present, the ones not
                        // currently active have to be culled out here, since KSP isn't culling them out itself when
                        // it populates ModuleRCS.thrusterTransforms:
                        if (!rcsTransform.gameObject.activeInHierarchy)
                            continue;

                        Vector3 rcsPosFromCoM = rcsTransform.position - Vessel.CurrentCoM;
                        Vector3 rcsThrustDir = rcs.useZaxis ? -rcsTransform.forward : rcsTransform.up;
                        float powerFactor = rcs.thrusterPower * rcs.thrustPercentage * 0.01f;
                        // Normally you'd check for precision mode to nerf powerFactor here,
                        // but kOS doesn't obey that.
                        Vector3 thrust = powerFactor * rcsThrustDir;
                        Vector3 torque = Vector3d.Cross(rcsPosFromCoM, thrust);
                        Vector3 transformedTorque = Vector3.Scale(Vessel.ReferenceTransform.InverseTransformDirection(torque), rotateEnables);
                        pos += Vector3.Max(transformedTorque, Vector3.zero);
                        neg += Vector3.Min(transformedTorque, Vector3.zero);
                    }
                }
            }
            else if (tp is ModuleReactionWheel)
            {
                // Although ModuleReactionWheel *mostly* works, the stock version ignores
                // the authority limiter slider.  It would have been possible to just take
                // the result it gives and multiply it by the slider, but that relies on
                // stock KSP never fixing it themselves and thus kOS would end up double-
                // applying that multiplitation.  To avoid that, it seems better to just
                // make the entire thing homemade from scratch for now so if KSP ever fixes it
                // on their end that doesn't break it on kOS's end:
                ModuleReactionWheel wheel = tp as ModuleReactionWheel;

                if (!wheel.moduleIsEnabled || wheel.wheelState != ModuleReactionWheel.WheelState.Active || wheel.actuatorModeCycle == 2)
                {
                    pos = new Vector3(0f, 0f, 0f);
                    neg = new Vector3(0f, 0f, 0f);
                }
                else
                {
                    float nerf = wheel.authorityLimiter / 100f;
                    pos = new Vector3(nerf * wheel.PitchTorque, nerf * wheel.RollTorque, nerf * wheel.YawTorque);
                    neg = -1 * pos;
                }
            }
            else
            {
                tp.GetPotentialTorque(out pos, out neg);
            }
        }

        #region TEMPORARY MOI CALCULATION
        /// <summary>
        /// This is a replacement for the stock API Property "vessel.MOI", which seems buggy when used
        /// with "control from here" on parts other than the default control part.
        /// <br/>
        /// Right now the stock Moment of Inertia Property returns values in inconsistent reference frames that
        /// don't make sense when used with "control from here".  (It doesn't merely rotate the reference frame, as one
        /// would expect "control from here" to do.)
        /// </summary>   
        /// TODO: Check this again after each KSP stock release to see if it's been changed or not.
        public Vector3 FindMoI()
        {
            var tensor = Matrix4x4.zero;
            Matrix4x4 partTensor = Matrix4x4.identity;
            Matrix4x4 inertiaMatrix = Matrix4x4.identity;
            Matrix4x4 productMatrix = Matrix4x4.identity;
            foreach (var part in Vessel.Parts)
            {
                if (part.rb != null)
                {
                    KSPUtil.ToDiagonalMatrix2(part.rb.inertiaTensor, ref partTensor);

                    Quaternion rot = Quaternion.Inverse(vesselRotation) * part.transform.rotation * part.rb.inertiaTensorRotation;
                    Quaternion inv = Quaternion.Inverse(rot);

                    Matrix4x4 rotMatrix = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
                    Matrix4x4 invMatrix = Matrix4x4.TRS(Vector3.zero, inv, Vector3.one);

                    // add the part inertiaTensor to the ship inertiaTensor
                    KSPUtil.Add(ref tensor, rotMatrix * partTensor * invMatrix);

                    Vector3 position = vesselTransform.InverseTransformDirection(part.rb.position - centerOfMass);

                    // add the part mass to the ship inertiaTensor
                    KSPUtil.ToDiagonalMatrix2(part.rb.mass * position.sqrMagnitude, ref inertiaMatrix);
                    KSPUtil.Add(ref tensor, inertiaMatrix);

                    // add the part distance offset to the ship inertiaTensor
                    OuterProduct2(position, -part.rb.mass * position, ref productMatrix);
                    KSPUtil.Add(ref tensor, productMatrix);
                }
            }
            return KSPUtil.Diag(tensor);
        }

        /// <summary>
        /// Construct the outer product of two 3-vectors as a 4x4 matrix
        /// DOES NOT ZERO ANY THINGS WOT ARE ZERO OR IDENTITY INNIT
        /// </summary>
        public static void OuterProduct2(Vector3 left, Vector3 right, ref Matrix4x4 m)
        {
            m.m00 = left.x * right.x;
            m.m01 = left.x * right.y;
            m.m02 = left.x * right.z;
            m.m10 = left.y * right.x;
            m.m11 = left.y * right.y;
            m.m12 = left.y * right.z;
            m.m20 = left.z * right.x;
            m.m21 = left.z * right.y;
            m.m22 = left.z * right.z;
        }
        #endregion

        // Update prediction based on PI controls, sets the target angular velocity and the target torque for the vessel
        public void UpdatePredictionPI()
        {
            // calculate phi and pitch, yaw, roll components of phi (angular error)
            phi = Vector3d.Angle(vesselForward, targetForward) / RadToDeg;

            if (Vector3d.Angle(vesselTop, targetForward) > 90)
                phi *= -1;
            phiPitch = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselStarboard, targetForward)) / RadToDeg;
            if (Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselStarboard, targetForward)) > 90)
                phiPitch *= -1;
            phiYaw = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselTop, targetForward)) / RadToDeg;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselTop, targetForward)) > 90)
                phiYaw *= -1;
            phiRoll = Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselForward, targetTop)) / RadToDeg;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselForward, targetTop)) > 90)
                phiRoll *= -1;

            // Calculate the maximum allowable angular velocity and apply the limit, something we can stop in a reasonable amount of time
            maxPitchOmega = controlTorque.x * MaxStoppingTime / momentOfInertia.x;
            maxYawOmega = controlTorque.z * MaxStoppingTime / momentOfInertia.z;
            maxRollOmega = controlTorque.y * MaxStoppingTime / momentOfInertia.y;
            double sampletime = shared.UpdateHandler.CurrentFixedTime;

            // Because the value of phi is already error, we say the input is -error and the setpoint is 0 so the PID has the correct sign
            tgtPitchOmega = pitchRatePI.Update(sampletime, -phiPitch, 0, maxPitchOmega, 0d);
            lerpedPitchEpsilon = LerpEpsilon(tgtPitchOmega, maxPitchOmega, torquePIDEpsilonMin, torquePIDEpsilonMax);
            tgtYawOmega = yawRatePI.Update(sampletime, -phiYaw, 0, maxYawOmega, 0d);
            lerpedYawEpsilon = LerpEpsilon(tgtYawOmega, maxYawOmega, torquePIDEpsilonMin, torquePIDEpsilonMax);
            if (Math.Abs(phi) > RollControlAngleRange * Math.PI / 180d)
            {
                tgtRollOmega = 0;
                rollRatePI.ResetI();
            }
            else
            {
                tgtRollOmega = rollRatePI.Update(sampletime, -phiRoll, 0, maxRollOmega, 0d);
                lerpedRollEpsilon = LerpEpsilon(tgtRollOmega, maxRollOmega, torquePIDEpsilonMin, torquePIDEpsilonMax);
            }

            // Calculate target torque based on PID
            tgtPitchTorque = pitchPI.Update(sampletime, omega.x, tgtPitchOmega, momentOfInertia.x, controlTorque.x, lerpedPitchEpsilon);
            tgtYawTorque = yawPI.Update(sampletime, omega.y, tgtYawOmega, momentOfInertia.z, controlTorque.z, lerpedYawEpsilon);
            tgtRollTorque = rollPI.Update(sampletime, omega.z, tgtRollOmega, momentOfInertia.y, controlTorque.y, lerpedRollEpsilon);

            //tgtPitchTorque = pitchPI.Update(sampletime, pitchRate.Update(omega.x), tgtPitchOmega, momentOfInertia.x, controlTorque.x);
            //tgtYawTorque = yawPI.Update(sampletime, yawRate.Update(omega.y), tgtYawOmega, momentOfInertia.z, controlTorque.z);
            //tgtRollTorque = rollPI.Update(sampletime, rollRate.Update(omega.z), tgtRollOmega, momentOfInertia.y, controlTorque.y);
        }

        /// <summary>
        /// Find the epsilon to feed into the angular velocity PID on the next pass based on what
        /// outputs it gave on the previous pass.  The purpose of this is to make the steering
        /// use less RCS fuel by only caring about precision near the start and stop of a turn,
        /// and not caring so much about it when the rotation is currently underway coasting
        /// at the max allowed angular velocity.
        /// <para />
        /// This is a lerp between min and max epsilon values, with min being used when the desired
        /// angular velocity is small and max being used when the desired angular velocity is large:
        /// </summary>
        /// <param name="omega">most recent angular velocity ordered by the PID</param>
        /// <param name="maxOmega">max possible angular velocity the PID could produce</param>
        /// <param name="epsilonMin">min epsilon (used when omega is small)</param>
        /// <param name="epsilonMax">max epsilon (used when omega is at max)</param>
        /// <returns></returns>
        private static double LerpEpsilon(double omega, double maxOmega, double epsilonMin, double epsilonMax)
        {
            // How close is angular velocity to max, with some protection from div by zero:
            double ratio = (maxOmega == 0 ? 1.0 : Math.Abs(omega / maxOmega));

            return epsilonMin + ratio * (epsilonMax - epsilonMin);
        }

        public void UpdateControl(FlightCtrlState c)
        {
            if (shared.Vessel.ActionGroups[KSPActionGroup.SAS])
            {
                pitchPI.ResetI();
                yawPI.ResetI();
                rollPI.ResetI();
                pitchRatePI.ResetI();
                yawRatePI.ResetI();
                rollRatePI.ResetI();
                Quaternion target = TargetDirection.Rotation * Quaternion.Euler(90, 0, 0);
                shared.Vessel.Autopilot.SAS.LockRotation(target);
            }
            else
            {
                //TODO: include adjustment for static torque (due to engines mounted offcenter?
                //   Not sure what hvacengi meant by this comment - dunbaratu)
                //
                // The purpose of clampAccPitch, clampYawPitch, and clampRollPitch:
                //    The purpose of these values appears to be to prevent the controls
                //    from moving too far in a single update.  If the PIDs instructed
                //    the controls to move by more than 2x as big as they were in the
                //    previous pass, clamp them to only moving as much as 2x as far this
                //    time (then they can move 2x as far as that next pass, etc until
                //    they reach the desired level.)
                //    - No, I have no idea why that is being done.  I'm just documenting
                //      it for anyone trying to understand this code - dunbaratu.
                double clampAccPitch = Math.Max(Math.Abs(accPitch), 0.005) * 2;
                accPitch = tgtPitchTorque / controlTorque.x;
                if (Math.Abs(accPitch) < CONTROLEPSILON)
                    accPitch = 0;
                accPitch = Math.Max(Math.Min(accPitch, clampAccPitch), -clampAccPitch);
                c.pitch = (float)accPitch;
                double clampAccYaw = Math.Max(Math.Abs(accYaw), 0.005) * 2;
                accYaw = tgtYawTorque / controlTorque.z;
                if (Math.Abs(accYaw) < CONTROLEPSILON)
                    accYaw = 0;
                accYaw = Math.Max(Math.Min(accYaw, clampAccYaw), -clampAccYaw);
                c.yaw = (float)accYaw;
                double clampAccRoll = Math.Max(Math.Abs(accRoll), 0.005) * 2;
                accRoll = tgtRollTorque / controlTorque.y;
                if (Math.Abs(accRoll) < CONTROLEPSILON)
                    accRoll = 0;
                accRoll = Math.Max(Math.Min(accRoll, clampAccRoll), -clampAccRoll);
                c.roll = (float)accRoll;
            }
        }

        public void UpdateVectorRenders()
        {
            if (ShowFacingVectors && enabled)
            {
                if (vForward == null)
                {
                    vForward = InitVectorRenderer(Color.red, 1, shared);
                }
                if (vTop == null)
                {
                    vTop = InitVectorRenderer(Color.red, 1, shared);
                }
                if (vStarboard == null)
                {
                    vStarboard = InitVectorRenderer(Color.red, 1, shared);
                }

                vForward.Vector = vesselForward * RENDER_MULTIPLIER;
                vTop.Vector = vesselTop * RENDER_MULTIPLIER;
                vStarboard.Vector = vesselStarboard * RENDER_MULTIPLIER;

                if (vTgtForward == null)
                {
                    vTgtForward = InitVectorRenderer(Color.blue, 1, shared);
                }
                if (vTgtTop == null)
                {
                    vTgtTop = InitVectorRenderer(Color.blue, 1, shared);
                }
                if (vTgtStarboard == null)
                {
                    vTgtStarboard = InitVectorRenderer(Color.blue, 1, shared);
                }

                vTgtForward.Vector = targetForward * RENDER_MULTIPLIER * 0.75f;
                vTgtTop.Vector = targetTop * RENDER_MULTIPLIER * 0.75f;
                vTgtStarboard.Vector = targetStarboard * RENDER_MULTIPLIER * 0.75f;

                if (vWorldX == null)
                {
                    vWorldX = InitVectorRenderer(Color.white, 1, shared);
                }
                if (vWorldY == null)
                {
                    vWorldY = InitVectorRenderer(Color.white, 1, shared);
                }
                if (vWorldZ == null)
                {
                    vWorldZ = InitVectorRenderer(Color.white, 1, shared);
                }

                vWorldX.Vector = new Vector3d(1, 0, 0) * RENDER_MULTIPLIER * 2;
                vWorldY.Vector = new Vector3d(0, 1, 0) * RENDER_MULTIPLIER * 2;
                vWorldZ.Vector = new Vector3d(0, 0, 1) * RENDER_MULTIPLIER * 2;

                if (!vForward.GetShow()) vForward.SetShow(true);
                if (!vTop.GetShow()) vTop.SetShow(true);
                if (!vStarboard.GetShow()) vStarboard.SetShow(true);

                if (!vTgtForward.GetShow()) vTgtForward.SetShow(true);
                if (!vTgtTop.GetShow()) vTgtTop.SetShow(true);
                if (!vTgtStarboard.GetShow()) vTgtStarboard.SetShow(true);

                if (!vWorldX.GetShow()) vWorldX.SetShow(true);
                if (!vWorldY.GetShow()) vWorldY.SetShow(true);
                if (!vWorldZ.GetShow()) vWorldZ.SetShow(true);
            }
            else
            {
                if (vForward != null)
                {
                    if (vForward.GetShow()) vForward.SetShow(false);
                    vForward.Dispose();
                    vForward = null;
                }
                if (vTop != null)
                {
                    if (vTop.GetShow()) vTop.SetShow(false);
                    vTop.Dispose();
                    vTop = null;
                }
                if (vStarboard != null)
                {
                    if (vStarboard.GetShow()) vStarboard.SetShow(false);
                    vStarboard.Dispose();
                    vStarboard = null;
                }

                if (vTgtForward != null)
                {
                    if (vTgtForward.GetShow()) vTgtForward.SetShow(false);
                    vTgtForward.Dispose();
                    vTgtForward = null;
                }
                if (vTgtTop != null)
                {
                    if (vTgtTop.GetShow()) vTgtTop.SetShow(false);
                    vTgtTop.Dispose();
                    vTgtTop = null;
                }
                if (vTgtStarboard != null)
                {
                    if (vTgtStarboard.GetShow()) vTgtStarboard.SetShow(false);
                    vTgtStarboard.Dispose();
                    vTgtStarboard = null;
                }

                if (vWorldX != null)
                {
                    if (vWorldX.GetShow()) vWorldX.SetShow(false);
                    vWorldX.Dispose();
                    vWorldX = null;
                }
                if (vWorldY != null)
                {
                    if (vWorldY.GetShow()) vWorldY.SetShow(false);
                    vWorldY.Dispose();
                    vWorldY = null;
                }
                if (vWorldZ != null)
                {
                    if (vWorldZ.GetShow()) vWorldZ.SetShow(false);
                    vWorldZ.Dispose();
                    vWorldZ = null;
                }
            }

            if (ShowAngularVectors && enabled && !Vessel.ActionGroups[KSPActionGroup.SAS])
            {
                if (vOmegaX == null)
                {
                    vOmegaX = InitVectorRenderer(Color.cyan, 1, shared);
                }
                if (vOmegaY == null)
                {
                    vOmegaY = InitVectorRenderer(Color.cyan, 1, shared);
                }
                if (vOmegaZ == null)
                {
                    vOmegaZ = InitVectorRenderer(Color.cyan, 1, shared);
                }

                vOmegaX.Vector = vesselTop * omega.x * RENDER_MULTIPLIER * 100f;
                vOmegaX.Start = vesselForward * RENDER_MULTIPLIER;
                vOmegaY.Vector = vesselStarboard * omega.y * RENDER_MULTIPLIER * 100f;
                vOmegaY.Start = vesselForward * RENDER_MULTIPLIER;
                vOmegaZ.Vector = vesselStarboard * omega.z * RENDER_MULTIPLIER * 100f;
                vOmegaZ.Start = vesselTop * RENDER_MULTIPLIER;

                if (vTgtTorqueX == null)
                {
                    vTgtTorqueX = InitVectorRenderer(Color.green, 1, shared);
                }
                if (vTgtTorqueY == null)
                {
                    vTgtTorqueY = InitVectorRenderer(Color.green, 1, shared);
                }
                if (vTgtTorqueZ == null)
                {
                    vTgtTorqueZ = InitVectorRenderer(Color.green, 1, shared);
                }

                vTgtTorqueX.Vector = vesselTop * tgtPitchOmega * RENDER_MULTIPLIER * 100f;
                vTgtTorqueX.Start = vesselForward * RENDER_MULTIPLIER;
                //vTgtTorqueX.SetLabel("tgtPitchOmega: " + tgtPitchOmega);
                vTgtTorqueY.Vector = vesselStarboard * tgtRollOmega * RENDER_MULTIPLIER * 100f;
                vTgtTorqueY.Start = vesselTop * RENDER_MULTIPLIER;
                //vTgtTorqueY.SetLabel("tgtRollOmega: " + tgtRollOmega);
                vTgtTorqueZ.Vector = vesselStarboard * tgtYawOmega * RENDER_MULTIPLIER * 100f;
                vTgtTorqueZ.Start = vesselForward * RENDER_MULTIPLIER;
                //vTgtTorqueZ.SetLabel("tgtYawOmega: " + tgtYawOmega);

                if (!vOmegaX.GetShow()) vOmegaX.SetShow(true);
                if (!vOmegaY.GetShow()) vOmegaY.SetShow(true);
                if (!vOmegaZ.GetShow()) vOmegaZ.SetShow(true);

                if (!vTgtTorqueX.GetShow()) vTgtTorqueX.SetShow(true);
                if (!vTgtTorqueY.GetShow()) vTgtTorqueY.SetShow(true);
                if (!vTgtTorqueZ.GetShow()) vTgtTorqueZ.SetShow(true);
            }
            else
            {
                if (vOmegaX != null)
                {
                    if (vOmegaX.GetShow()) vOmegaX.SetShow(false);
                    vOmegaX.Dispose();
                    vOmegaX = null;
                }
                if (vOmegaY != null)
                {
                    if (vOmegaY.GetShow()) vOmegaY.SetShow(false);
                    vOmegaY.Dispose();
                    vOmegaY = null;
                }
                if (vOmegaZ != null)
                {
                    if (vOmegaZ.GetShow()) vOmegaZ.SetShow(false);
                    vOmegaZ.Dispose();
                    vOmegaZ = null;
                }

                if (vTgtTorqueX != null)
                {
                    if (vTgtTorqueX.GetShow()) vTgtTorqueX.SetShow(false);
                    vTgtTorqueX.Dispose();
                    vTgtTorqueX = null;
                }
                if (vTgtTorqueY != null)
                {
                    if (vTgtTorqueY.GetShow()) vTgtTorqueY.SetShow(false);
                    vTgtTorqueY.Dispose();
                    vTgtTorqueY = null;
                }
                if (vTgtTorqueZ != null)
                {
                    if (vTgtTorqueZ.GetShow()) vTgtTorqueZ.SetShow(false);
                    vTgtTorqueZ.Dispose();
                    vTgtTorqueZ = null;
                }
            }
        }

        public void PrintDebug()
        {
            shared.Screen.ClearScreen();
            shared.Screen.Print(string.Format("phi: {0}", phi * RadToDeg));
            shared.Screen.Print(string.Format("phiRoll: {0}", phiRoll * RadToDeg));
            shared.Screen.Print("    Pitch Values:");
            shared.Screen.Print(string.Format("phiPitch: {0}", phiPitch * RadToDeg));
            //shared.Screen.Print(string.Format("phiPitch: {0}", deltaRotation.eulerAngles.x));
            shared.Screen.Print(string.Format("I pitch: {0}", momentOfInertia.x));
            shared.Screen.Print(string.Format("torque pitch: {0}", controlTorque.x));
            shared.Screen.Print(string.Format("maxPitchOmega: {0}", maxPitchOmega));
            shared.Screen.Print(string.Format("tgtPitchOmega: {0}", tgtPitchOmega));
            shared.Screen.Print(string.Format("pitchOmega: {0}", omega.x));
            shared.Screen.Print(string.Format("tgtPitchTorque: {0}", tgtPitchTorque));
            shared.Screen.Print(string.Format("accPitch: {0}", accPitch));
            shared.Screen.Print("    Yaw Values:");
            shared.Screen.Print(string.Format("phiYaw: {0}", phiYaw * RadToDeg));
            //shared.Screen.Print(string.Format("phiYaw: {0}", deltaRotation.eulerAngles.y));
            shared.Screen.Print(string.Format("I yaw: {0}", momentOfInertia.z));
            shared.Screen.Print(string.Format("torque yaw: {0}", controlTorque.z));
            shared.Screen.Print(string.Format("maxYawOmega: {0}", maxYawOmega));
            shared.Screen.Print(string.Format("tgtYawOmega: {0}", tgtYawOmega));
            shared.Screen.Print(string.Format("yawOmega: {0}", omega.y));
            shared.Screen.Print(string.Format("tgtYawTorque: {0}", tgtYawTorque));
            shared.Screen.Print(string.Format("accYaw: {0}", accYaw));
            shared.Screen.Print("    Roll Values:");
            shared.Screen.Print(string.Format("phiRoll: {0}", phiRoll * RadToDeg));
            //shared.Screen.Print(string.Format("phiRoll: {0}", deltaRotation.eulerAngles.z));
            shared.Screen.Print(string.Format("I roll: {0}", momentOfInertia.y));
            shared.Screen.Print(string.Format("torque roll: {0}", controlTorque.y));
            shared.Screen.Print(string.Format("maxRollOmega: {0}", maxRollOmega));
            shared.Screen.Print(string.Format("tgtRollOmega: {0}", tgtRollOmega));
            shared.Screen.Print(string.Format("rollOmega: {0}", omega.z));
            shared.Screen.Print(string.Format("tgtRollTorque: {0}", tgtRollTorque));
            shared.Screen.Print(string.Format("accRoll: {0}", accRoll));
            shared.Screen.Print("    Processing Stats:");
            shared.Screen.Print(string.Format("Average Duration: {0}", AverageDuration.Mean));
            shared.Screen.Print(string.Format("Based on count: {0}", AverageDuration.ValueCount));
        }

        public void WriteCSVs()
        {
            if (pitchRateWriter == null)
            {
                pitchRateWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "pitchRate"));
                pitchRateWriter.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Kd,MinOutput,MaxOutput");
            }
            if (yawRateWriter == null)
            {
                yawRateWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "yawRate"));
                yawRateWriter.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Kd,MinOutput,MaxOutput");
            }
            if (rollRateWriter == null)
            {
                rollRateWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "rollRate"));
                rollRateWriter.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Kd,MinOutput,MaxOutput");
            }
            if (pitchTorqueWriter == null)
            {
                pitchTorqueWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "pitchTorque"));
                pitchTorqueWriter.WriteLine("LastSampleTime,Input,Setpoint,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MinOutput,MaxOutput");
            }
            if (yawTorqueWriter == null)
            {
                yawTorqueWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "yawTorque"));
                yawTorqueWriter.WriteLine("LastSampleTime,Input,Setpoint,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MinOutput,MaxOutput");
            }
            if (rollTorqueWriter == null)
            {
                rollTorqueWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "rollTorque"));
                rollTorqueWriter.WriteLine("LastSampleTime,Input,Setpoint,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MinOutput,MaxOutput");
            }
            if (adjustTorqueWriter == null)
            {
                adjustTorqueWriter = KSP.IO.File.AppendText<SteeringManager>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "adjustTorque"));
                adjustTorqueWriter.WriteLine("LastSampleTime,Target Pitch,Measured Pitch,Average Adjust Pitch,Raw Pitch,Target Yaw,Measured Yaw,Average Adjust Yaw,Raw Yaw,Target Roll,Measured Roll,Average Adjust Roll,Raw Roll,Samples Roll");
            }

            pitchRateWriter.WriteLine(pitchRatePI.ToCSVString());
            yawRateWriter.WriteLine(yawRatePI.ToCSVString());
            rollRateWriter.WriteLine(rollRatePI.ToCSVString());

            pitchTorqueWriter.WriteLine(pitchPI.ToCSVString());
            yawTorqueWriter.WriteLine(yawPI.ToCSVString());
            rollTorqueWriter.WriteLine(rollPI.ToCSVString());

            adjustTorqueWriter.WriteLine(string.Join(",", new[] {
                sessionTime.ToString(),
                tgtPitchTorque.ToString(),
                measuredTorque.x.ToString(),
                pitchTorqueCalc.Mean.ToString(),
                rawTorque.x.ToString(),
                tgtYawTorque.ToString(),
                measuredTorque.z.ToString(),
                yawTorqueCalc.Mean.ToString(),
                rawTorque.z.ToString(),
                tgtRollTorque.ToString(),
                measuredTorque.y.ToString(),
                rollTorqueCalc.Mean.ToString(),
                rawTorque.y.ToString(),
                rollTorqueCalc.ValueCount.ToString()
            }));
        }

        public void DisposeVectorRenderers()
        {
            if (vForward != null)
            {
                vForward.Dispose();
                vForward = null;
            }
            if (vTop != null)
            {
                vTop.Dispose();
                vTop = null;
            }
            if (vStarboard != null)
            {
                vStarboard.Dispose();
                vStarboard = null;
            }

            if (vTgtForward != null)
            {
                vTgtForward.Dispose();
                vTgtForward = null;
            }
            if (vTgtTop != null)
            {
                vTgtTop.Dispose();
                vTgtTop = null;
            }
            if (vTgtStarboard != null)
            {
                vTgtStarboard.Dispose();
                vTgtStarboard = null;
            }

            if (vTgtTorqueX != null)
            {
                vTgtTorqueX.Dispose();
                vTgtTorqueX = null;
            }
            if (vTgtTorqueY != null)
            {
                vTgtTorqueY.Dispose();
                vTgtTorqueY = null;
            }
            if (vTgtTorqueZ != null)
            {
                vTgtTorqueZ.Dispose();
                vTgtTorqueZ = null;
            }

            if (vWorldX != null)
            {
                vWorldX.Dispose();
                vWorldX = null;
            }
            if (vWorldY != null)
            {
                vWorldY.Dispose();
                vWorldY = null;
            }
            if (vWorldZ != null)
            {
                vWorldZ.Dispose();
                vWorldZ = null;
            }

            if (vOmegaX != null)
            {
                vOmegaX.Dispose();
                vOmegaX = null;
            }
            if (vOmegaY != null)
            {
                vOmegaY.Dispose();
                vOmegaY = null;
            }
            if (vOmegaZ != null)
            {
                vOmegaZ.Dispose();
                vOmegaZ = null;
            }
        }

        public void Dispose()
        {
            DisposeVectorRenderers();

            if (pitchRateWriter != null) pitchRateWriter.Dispose();
            if (yawRateWriter != null) yawRateWriter.Dispose();
            if (rollRateWriter != null) rollRateWriter.Dispose();

            if (pitchTorqueWriter != null) pitchTorqueWriter.Dispose();
            if (yawTorqueWriter != null) yawTorqueWriter.Dispose();
            if (rollTorqueWriter != null) rollTorqueWriter.Dispose();

            if (adjustTorqueWriter != null) adjustTorqueWriter.Dispose();

            pitchRateWriter = null;
            yawRateWriter = null;
            rollRateWriter = null;

            pitchTorqueWriter = null;
            yawTorqueWriter = null;
            rollTorqueWriter = null;

            adjustTorqueWriter = null;

        }

        public class TorquePI
        {
            public PIDLoop Loop { get; set; }

            public double I { get; private set; }

            public MovingAverage TorqueAdjust { get; set; }

            private double tr;

            public double Tr
            {
                get { return tr; }
                set
                {
                    tr = value;
                    ts = 4.0 * tr / 2.76;
                }
            }

            private double ts;

            public double Ts
            {
                get { return ts; }
                set
                {
                    ts = value;
                    tr = 2.76 * ts / 4.0;
                }
            }

            public TorquePI()
            {
                Loop = new PIDLoop();
                Ts = 2;
                TorqueAdjust = new MovingAverage();
            }

            public double Update(double sampleTime, double input, double setpoint, double momentOfInertia, double maxOutput, double epsilon)
            {
                I = momentOfInertia;

                Loop.Ki = momentOfInertia * Math.Pow(4.0 / ts, 2);
                Loop.Kp = 2 * Math.Pow(momentOfInertia * Loop.Ki, 0.5);
                return Loop.Update(sampleTime, input, setpoint, maxOutput, epsilon);
            }

            public void ResetI()
            {
                Loop.ResetI();
            }

            public override string ToString()
            {
                return string.Format("TorquePI[Kp:{0}, Ki:{1}, Output:{2}, Error:{3}, ErrorSum:{4}, Tr:{5}, Ts:{6}",
                    Loop.Kp, Loop.Ki, Loop.Output, Loop.Error, Loop.ErrorSum, Tr, Ts);
            }

            public string ToCSVString()
            {
                return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
                    Loop.LastSampleTime, Loop.Input, Loop.Setpoint, Loop.Error, Loop.ErrorSum, Loop.Output, Loop.Kp, Loop.Ki, Tr, Ts, I, Loop.MinOutput, Loop.MaxOutput);
            }
        }

        bool IFlightControlParameter.Enabled
        {
            get
            {
                return this.Enabled;
            }
        }

        bool IFlightControlParameter.IsAutopilot
        {
            get
            {
                return true;
            }
        }

        uint IFlightControlParameter.ControlPartId
        {
            get { return this.PartId; }
        }

        void IFlightControlParameter.UpdateValue(object value, SharedObjects shared)
        {
            if (!Enabled) // if control was somehow disabled by another processor, re-enable it
            {
                EnableControl(shared);
            }
            this.shared = shared; // set shared on every call to UpdateValue, the last processor to call wins.
            partId = shared.KSPPart.flightID;
            Value = GetDirectionFromValue(value);
        }

        object IFlightControlParameter.GetValue()
        {
            return Value;
        }

        void IFlightControlParameter.UpdateAutopilot(FlightCtrlState c, ControlTypes ctrlLock)
        {
            this.OnFlyByWire(c);
            if ((ctrlLock & ControlTypes.PITCH) != 0)
            {
                pitchPI.ResetI();
                pitchRatePI.ResetI();
            }
            if ((ctrlLock & ControlTypes.YAW) != 0)
            {
                yawPI.ResetI();
                yawRatePI.ResetI();
            }
            if ((ctrlLock & ControlTypes.ROLL) != 0)
            {
                rollPI.ResetI();
                rollRatePI.ResetI();
            }
        }

        bool IFlightControlParameter.SuppressAutopilot(FlightCtrlState c)
        {
            if (!Enabled || Value == null)
            {
                return false;
            }
            else
            {
                pitchPI.ResetI();
                yawPI.ResetI();
                rollPI.ResetI();
                pitchRatePI.ResetI();
                yawRatePI.ResetI();
                rollRatePI.ResetI();
                return true;
            }
        }

        void IFlightControlParameter.EnableControl(SharedObjects shared)
        {
            this.EnableControl(shared);
        }

        void IFlightControlParameter.DisableControl(SharedObjects shared)
        {
            this.DisableControl(shared);
        }

        void IFlightControlParameter.DisableControl()
        {
            this.DisableControl();
        }

        void IFlightControlParameter.CopyFrom(IFlightControlParameter origin)
        {
            SteeringManager smOrigin = origin as SteeringManager;
            if (smOrigin != null)
            {
                Copy(smOrigin, this);
            }
        }

        SharedObjects IFlightControlParameter.GetShared()
        {
            return shared;
        }

        Vessel IFlightControlParameter.GetResponsibleVessel()
        {
            return shared.Vessel;
        }
    }
}