using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Math = System.Math;

namespace kOS.Binding
{
    public class SteeringManager : Structure, IDisposable
    {
        public static SteeringManager DeepCopy(SteeringManager oldInstance, SharedObjects shared)
        {
            SteeringManager instance = SteeringManagerProvider.GetInstance(shared);
            instance.ShowAngularVectors = oldInstance.ShowAngularVectors;
            instance.ShowFacingVectors = oldInstance.ShowFacingVectors;
            instance.ShowRCSVectors = oldInstance.ShowRCSVectors;
            instance.ShowThrustVectors = oldInstance.ShowThrustVectors;
            instance.ShowSteeringStats = oldInstance.ShowSteeringStats;
            instance.WriteCSVFiles = oldInstance.WriteCSVFiles;
            instance.MaxStoppingTime = oldInstance.MaxStoppingTime;

            instance.pitchPI.Ts = oldInstance.pitchPI.Ts;
            instance.yawPI.Ts = oldInstance.yawPI.Ts;
            instance.rollPI.Ts = oldInstance.rollPI.Ts;
            instance.pitchPI.Loop = PIDLoop.DeepCopy(oldInstance.pitchPI.Loop);
            instance.yawPI.Loop = PIDLoop.DeepCopy(oldInstance.yawPI.Loop);
            instance.rollPI.Loop = PIDLoop.DeepCopy(oldInstance.rollPI.Loop);

            instance.pitchRatePI = PIDLoop.DeepCopy(oldInstance.pitchRatePI);
            instance.yawRatePI = PIDLoop.DeepCopy(oldInstance.yawRatePI);
            instance.rollRatePI = PIDLoop.DeepCopy(oldInstance.rollRatePI);

            instance.PitchTorqueAdjust = oldInstance.PitchTorqueAdjust;
            instance.PitchTorqueFactor = oldInstance.PitchTorqueFactor;
            instance.RollTorqueAdjust = oldInstance.RollTorqueAdjust;
            instance.RollTorqueFactor = oldInstance.RollTorqueFactor;
            instance.YawTorqueAdjust = oldInstance.YawTorqueAdjust;
            instance.YawTorqueFactor = oldInstance.YawTorqueFactor;
            return instance;
        }

        public Vessel Vessel
        {
            get
            {
                return shared.Vessel;
            }
        }

        public List<uint> SubscribedParts = new List<uint>();

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

        public bool ShowThrustVectors { get; set; }

        public bool ShowRCSVectors { get; set; }

        public bool ShowSteeringStats { get; set; }

        public bool WriteCSVFiles { get; set; }

        private const string FILE_BASE_NAME = "{0}-{1}-{2}.csv";
        private readonly string fileDateString = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

        public object Value { get; set; }

        public Direction TargetDirection { get { return GetDirectionFromValue(); } }

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

        private readonly MovingAverage pitchMOICalc = new MovingAverage { SampleLimit = 5 };
        private readonly MovingAverage yawMOICalc = new MovingAverage { SampleLimit = 5 };
        private readonly MovingAverage rollMOICalc = new MovingAverage { SampleLimit = 5 };

        private bool enableMoiAdjust;

        public MovingAverage AverageDuration = new MovingAverage { SampleLimit = 60 };

        #region doubles

        public const double RadToDeg = 180d / Math.PI;

        private const double EPSILON = 0.00001;

        private double sessionTime = double.MaxValue;
        private double lastSessionTime = double.MaxValue;

        public double MaxStoppingTime { get; set; }

        private double accPitch = 0;
        private double accYaw = 0;
        private double accRoll = 0;

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
        private double vesselMass = 0;

        #endregion doubles

        private readonly List<ModuleReactionWheel> reactionWheelModules = new List<ModuleReactionWheel>();
        private readonly List<ModuleRCS> rcsModules = new List<ModuleRCS>();
        private readonly List<ModuleEngines> engineModules = new List<ModuleEngines>();
        private readonly List<ModuleGimbal> gimbalModules = new List<ModuleGimbal>();

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
        private Vector3d adjustMomentOfInertia;

        private Vector3d omega = Vector3d.zero; // x: pitch, y: yaw, z: roll
        private Vector3d lastOmega = Vector3d.zero;
        private Vector3d angularAcceleration = Vector3d.zero;
        private Vector3d momentOfInertia = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d measuredMomentOfInertia = Vector3d.zero;
        private Vector3d measuredTorque = Vector3d.zero;
        private Vector3d staticTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d controlTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d rawTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d staticEngineTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d controlEngineTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll

        private readonly List<ForceVector> rcsVectors = new List<ForceVector>();
        private readonly List<ForceVector> engineNeutVectors = new List<ForceVector>();
        private readonly List<ForceVector> enginePitchVectors = new List<ForceVector>();
        private readonly List<ForceVector> engineYawVectors = new List<ForceVector>();
        private readonly List<ForceVector> engineRollVectors = new List<ForceVector>();

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

        private readonly Dictionary<string, VectorRenderer> vEngines = new Dictionary<string, VectorRenderer>();
        private readonly Dictionary<string, VectorRenderer> vRcs = new Dictionary<string, VectorRenderer>();

        #endregion VectorRenderers

        public SteeringManager(SharedObjects sharedObj)
        {
            shared = sharedObj;
            ShowFacingVectors = false;
            ShowAngularVectors = false;
            ShowThrustVectors = false;
            ShowRCSVectors = false;
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

            adjustTorque = Vector3d.zero;
            adjustMomentOfInertia = Vector3d.one;

            enableMoiAdjust = false;
            EnableTorqueAdjust = true;

            MaxStoppingTime = 2;

            PitchTorqueAdjust = 0;
            YawTorqueAdjust = 0;
            RollTorqueAdjust = 0;

            PitchTorqueFactor = 1;
            YawTorqueFactor = 1;
            RollTorqueFactor = 1;

            InitializeSuffixes();
        }

        public void InitializeSuffixes()
        {
            AddSuffix("PITCHPID", new Suffix<PIDLoop>(() => pitchRatePI));
            AddSuffix("YAWPID", new Suffix<PIDLoop>(() => yawRatePI));
            AddSuffix("ROLLPID", new Suffix<PIDLoop>(() => rollRatePI));
            AddSuffix("ENABLED", new Suffix<bool>(() => Enabled));
            AddSuffix("TARGET", new Suffix<Direction>(() => TargetDirection));
            AddSuffix("RESETPIDS", new NoArgsSuffix(ResetIs));
            AddSuffix("SHOWFACINGVECTORS", new SetSuffix<bool>(() => ShowFacingVectors, value => ShowFacingVectors = value));
            AddSuffix("SHOWANGULARVECTORS", new SetSuffix<bool>(() => ShowAngularVectors, value => ShowAngularVectors = value));
            AddSuffix("SHOWTHRUSTVECTORS", new SetSuffix<bool>(() => ShowThrustVectors, value => ShowThrustVectors = value));
            AddSuffix("SHOWRCSVECTORS", new SetSuffix<bool>(() => ShowRCSVectors, value => ShowRCSVectors = value));
            AddSuffix("SHOWSTEERINGSTATS", new SetSuffix<bool>(() => ShowSteeringStats, value => ShowSteeringStats = value));
            AddSuffix("WRITECSVFILES", new SetSuffix<bool>(() => WriteCSVFiles, value => WriteCSVFiles = value));
            AddSuffix("PITCHTS", new SetSuffix<double>(() => pitchPI.Ts, value => pitchPI.Ts = value));
            AddSuffix("YAWTS", new SetSuffix<double>(() => yawPI.Ts, value => yawPI.Ts = value));
            AddSuffix("ROLLTS", new SetSuffix<double>(() => rollPI.Ts, value => rollPI.Ts = value));
            AddSuffix("MAXSTOPPINGTIME", new SetSuffix<double>(() => MaxStoppingTime, value => MaxStoppingTime = value));
            AddSuffix("ANGLEERROR", new Suffix<double>(() => phi * RadToDeg));
            AddSuffix("PITCHERROR", new Suffix<double>(() => phiPitch * RadToDeg));
            AddSuffix("YAWERROR", new Suffix<double>(() => phiYaw * RadToDeg));
            AddSuffix("ROLLERROR", new Suffix<double>(() => phiRoll * RadToDeg));
            AddSuffix("PITCHTORQUEADJUST", new SetSuffix<double>(() => PitchTorqueAdjust, value => PitchTorqueAdjust = value));
            AddSuffix("YAWTORQUEADJUST", new SetSuffix<double>(() => YawTorqueAdjust, value => YawTorqueAdjust = value));
            AddSuffix("ROLLTORQUEADJUST", new SetSuffix<double>(() => RollTorqueAdjust, value => RollTorqueAdjust = value));
            AddSuffix("PITCHTORQUEFACTOR", new SetSuffix<double>(() => PitchTorqueFactor, value => PitchTorqueFactor = value));
            AddSuffix("YAWTORQUEFACTOR", new SetSuffix<double>(() => YawTorqueFactor, value => YawTorqueFactor = value));
            AddSuffix("ROLLTORQUEFACTOR", new SetSuffix<double>(() => RollTorqueFactor, value => RollTorqueFactor = value));
            AddSuffix("AVERAGEDURATION", new Suffix<double>(() => AverageDuration.Mean));
#if DEBUG
            AddSuffix("MOI", new Suffix<Vector>(() => new Vector(momentOfInertia)));
            AddSuffix("ACTUATION", new Suffix<Vector>(() => new Vector(accPitch, accRoll, accYaw)));
            AddSuffix("CONTROLTORQUE", new Suffix<Vector>(() => new Vector(controlTorque)));
            AddSuffix("MEASUREDTORQUE", new Suffix<Vector>(() => new Vector(measuredTorque)));
            AddSuffix("RAWTORQUE", new Suffix<Vector>(() => new Vector(rawTorque)));
            AddSuffix("TARGETTORQUE", new Suffix<Vector>(() => new Vector(tgtPitchTorque, tgtRollTorque, tgtYawTorque)));
            AddSuffix("ANGULARVELOCITY", new Suffix<Vector>(() => new Vector(omega)));
            AddSuffix("ANGULARACCELERATION", new Suffix<Vector>(() => new Vector(angularAcceleration)));
            AddSuffix("ENABLETORQUEADJUST", new SetSuffix<bool>(() => EnableTorqueAdjust, value => EnableTorqueAdjust = value));
            AddSuffix("ENABLEMOIADJUST", new SetSuffix<bool>(() => enableMoiAdjust, value => enableMoiAdjust = value));
#endif
        }

        public void EnableControl(SharedObjects sharedObj)
        {
            if (enabled && partId != sharedObj.KSPPart.flightID)
                throw new Safe.Exceptions.KOSException("Steering Manager on this ship is already in use by another processor.");
            shared = sharedObj;
            partId = sharedObj.KSPPart.flightID;
            ResetIs();
            Enabled = true;
            lastSessionTime = double.MaxValue;
            pitchTorqueCalc.Reset();
            rollTorqueCalc.Reset();
            yawTorqueCalc.Reset();

            adjustTorque = Vector3d.zero;
            adjustMomentOfInertia = Vector3d.one;
        }

        public void DisableControl()
        {
            if (enabled && partId != shared.KSPPart.flightID)
                throw new Safe.Exceptions.KOSException("Cannon unbind Steering Manager on this ship in use by another processor.");
            partId = 0;
            Enabled = false;
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

        public void Update(Vessel vsl)
        {
            //if (vessel != vsl) vessel = vsl;
            // Eventually I would like to update the vectors regardless of if flybywire is called,
            // so that the vector renderers will still update in time warp, but it doesn't work now.
            //UpdateStateVectors();
            //UpdateTorque();
            //UpdatePrediction();
            //UpdateVectorRenders();
            //PrintDebug();
        }

        public void OnFlyByWire(FlightCtrlState c)
        {
            Update(c);
        }

        public void OnRemoteTechPilot(FlightCtrlState c)
        {
            Update(c);
        }

        private readonly System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        private void Update(FlightCtrlState c)
        {
            if (Value == null) return;

            lastSessionTime = sessionTime;
            sessionTime = Math.Round(Planetarium.GetUniversalTime(), 3);
            //if (sessionTime > lastSessionTime)
            //{
            //}
            sw.Reset();
            sw.Start();
            UpdateStateVectors();
            UpdateControlParts();
            UpdateTorque();
            UpdatePredictionPI();
            UpdateControl(c);
            if (ShowSteeringStats) PrintDebug();
            if (WriteCSVFiles) WriteCSVs();
            UpdateVectorRenders();
            sw.Stop();
            AverageDuration.Update((double)sw.ElapsedTicks / (double)System.TimeSpan.TicksPerMillisecond);
        }

        private Direction GetDirectionFromValue()
        {
            if (Value is Direction)
                return (Direction)Value;
            else if (Value is Vector)
                return Direction.LookRotation((Vector)Value, vesselUp);
            else if (Value is Node)
                return Direction.LookRotation(((Node)Value).GetBurnVector(), vesselUp);
            else if (Value is string)
            {
                if (string.Equals((string)Value, "KILL", StringComparison.OrdinalIgnoreCase))
                {
                    return new Direction(vesselRotation);
                }
            }
            DisableControl();
            throw new Safe.Exceptions.KOSWrongControlValueTypeException(
                "STEERING", Value.GetType().Name, "Direction, Vector, Manuever Node, or special string \"KILL\"");
        }

        public void UpdateStateVectors()
        {
            Direction targetdir = GetDirectionFromValue();
            if (targetdir == null)
            {
                shared.Logger.LogWarning(string.Format("SteeringManager target direction is null, Value = {0}", Value));
                return;
            }

            targetRot = targetdir.Rotation;
            centerOfMass = shared.Vessel.findWorldCenterOfMass();

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
            omega = Quaternion.Inverse(vesselRotation) * shared.Vessel.rigidbody.angularVelocity;
            omega.x *= -1; //positive values pull the nose to the starboard.
            //omega.y *= -1; // positive values pull the nose up.
            omega.z *= -1; // positive values pull the starboard side up.

            // TODO: Currently adjustments to MOI are only enabled in debug compiles.  Using this feature seems to be buggy, but it has potential
            // to be more resiliant against random spikes in angular velocity.
            if (sessionTime > lastSessionTime)
            {
                double dt = sessionTime - lastSessionTime;
                angularAcceleration = (omega - oldOmega) / dt;
                angularAcceleration = new Vector3d(angularAcceleration.x, angularAcceleration.z, angularAcceleration.y);
                if (enableMoiAdjust)
                {
                    measuredMomentOfInertia = new Vector3d(
                        controlTorque.x * accPitch / angularAcceleration.x,
                        controlTorque.y * accRoll / angularAcceleration.y,
                        controlTorque.z * accYaw / angularAcceleration.z);

                    if (Math.Abs(accPitch) > EPSILON)
                    {
                        adjustMomentOfInertia.x = pitchMOICalc.Update(Math.Abs(measuredMomentOfInertia.x) / momentOfInertia.x);
                    }
                    if (Math.Abs(accYaw) > EPSILON)
                    {
                        adjustMomentOfInertia.z = yawMOICalc.Update(Math.Abs(measuredMomentOfInertia.z) / momentOfInertia.z);
                    }
                    if (Math.Abs(accRoll) > EPSILON)
                    {
                        adjustMomentOfInertia.y = rollMOICalc.Update(Math.Abs(measuredMomentOfInertia.y) / momentOfInertia.y);
                    }
                }
            }

            momentOfInertia = shared.Vessel.findLocalMOI(centerOfMass);
            momentOfInertia.Scale(adjustMomentOfInertia);
            adjustTorque = Vector3d.zero;
            measuredTorque = Vector3d.Scale(momentOfInertia, angularAcceleration);

            if (sessionTime > lastSessionTime && EnableTorqueAdjust)
            {
                if (Math.Abs(accPitch) > EPSILON)
                {
                    adjustTorque.x = Math.Min(Math.Abs(pitchTorqueCalc.Update(measuredTorque.x / accPitch)) - rawTorque.x, 0);
                    //adjustTorque.x = Math.Abs(pitchTorqueCalc.Update(measuredTorque.x / accPitch) / rawTorque.x);
                }
                else adjustTorque.x = Math.Abs(pitchTorqueCalc.Update(pitchTorqueCalc.Mean));
                if (Math.Abs(accYaw) > EPSILON)
                {
                    adjustTorque.z = Math.Min(Math.Abs(yawTorqueCalc.Update(measuredTorque.z / accYaw)) - rawTorque.z, 0);
                    //adjustTorque.z = Math.Abs(yawTorqueCalc.Update(measuredTorque.z / accYaw) / rawTorque.z);
                }
                else adjustTorque.z = Math.Abs(yawTorqueCalc.Update(yawTorqueCalc.Mean));
                if (Math.Abs(accRoll) > EPSILON)
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
                reactionWheelModules.Clear();
                rcsModules.Clear();
                engineModules.Clear();
                gimbalModules.Clear();
                foreach (Part part in shared.Vessel.Parts)
                {
                    int engineCount = 0;
                    ModuleGimbal partGimbal = null;
                    foreach (PartModule pm in part.Modules)
                    {
                        ModuleReactionWheel wheel = pm as ModuleReactionWheel;
                        if (wheel != null)
                        {
                            reactionWheelModules.Add(wheel);
                            continue;
                        }
                        ModuleRCS rcs = pm as ModuleRCS;
                        if (rcs != null)
                        {
                            rcsModules.Add(rcs);
                            continue;
                        }
                        ModuleGimbal gimbal = pm as ModuleGimbal;
                        if (gimbal != null)
                        {
                            partGimbal = gimbal;
                            if (engineCount > 0)
                            {
                                for (int i = 0; i < engineCount; i++)
                                {
                                    gimbalModules[gimbalModules.Count - i - 1] = gimbal;
                                }
                            }
                            continue;
                        }
                        ModuleEngines engine = pm as ModuleEngines;
                        if (engine != null)
                        {
                            engineCount++;
                            engineModules.Add(engine);
                            gimbalModules.Add(partGimbal);
                        }
                    }
                }
            }
        }

        public void UpdateTorque()
        {
            // staticTorque will represent engine torque due to imbalanced placement
            staticTorque.Zero();
            // controlTorque is the maximum amount of torque applied by setting a control to 1.0.
            controlTorque.Zero();
            rawTorque.Zero();
            staticEngineTorque.Zero();
            controlEngineTorque.Zero();
            Vector3d pitchControl = Vector3d.zero;
            Vector3d yawControl = Vector3d.zero;
            Vector3d rollControl = Vector3d.zero;

            rcsVectors.Clear();
            engineNeutVectors.Clear();
            enginePitchVectors.Clear();
            engineYawVectors.Clear();
            engineRollVectors.Clear();

            foreach (ModuleReactionWheel wheel in reactionWheelModules)
            {
                if (wheel.isActiveAndEnabled && wheel.State == ModuleReactionWheel.WheelState.Active)
                {
                    // TODO: Check to see if the component values depend on part orientation, and implement if needed.
                    rawTorque.x += wheel.PitchTorque;
                    rawTorque.z += wheel.YawTorque;
                    rawTorque.y += wheel.RollTorque;
                }
            }
            if (shared.Vessel.ActionGroups[KSPActionGroup.RCS])
            {
                foreach (ModuleRCS rcs in rcsModules)
                {
                    if (rcs.rcsEnabled && !rcs.part.ShieldedFromAirstream)
                    {
                        for (int i = 0; i < rcs.thrusterTransforms.Count; i++)
                        {
                            Transform thrustdir = rcs.thrusterTransforms[i];
                            ForceVector force = new ForceVector
                            {
                                Force = thrustdir.up * rcs.thrusterPower,
                                Position = thrustdir.position - centerOfMass,
                                // We need to adjust the relative center of mass because the rigid body
                                // will report the position of the parent part
                                ID = rcs.part.flightID.ToString() + "-" + i.ToString()
                            };
                            Vector3d torque = force.Torque;
                            rcsVectors.Add(force);

                            // component values of the local torque are calculated using the dot product with the rotation axis.
                            // Only using positive contributions, which is only valid when symmetric placement is assumed
                            rawTorque.x += Math.Max(Vector3d.Dot(torque, vesselStarboard), 0);
                            rawTorque.z += Math.Max(Vector3d.Dot(torque, vesselTop), 0);
                            rawTorque.y += Math.Max(Vector3d.Dot(torque, vesselForward), 0);
                        }
                    }
                }
            }
            for (int i = 0; i < engineModules.Count; i++)
            {
                ModuleEngines engine = engineModules[i];
                ModuleGimbal gimbal = gimbalModules[i];
                if (engine.isActiveAndEnabled && engine.EngineIgnited)
                {
                    // if the engine is active and ignited, calculate torque
                    float gimbalRange = 0;
                    List<Quaternion> initRots = new List<Quaternion>();
                    if (gimbal != null && !gimbal.gimbalLock)
                    {
                        gimbalRange = gimbal.gimbalRange * gimbal.gimbalLimiter / 100;  // grab the gimbal range

                        // To determine the neutral thrust vector, we reset the gimbal transform to it's "initial" value
                        // from the initRots array and then read the direction from the thrustTransform.  We do this for
                        // each gimbal object, and then return them to the previous value after calculating the thrust
                        // transforms, so that we don't inerupt the gimbal logic.
                        for (int gblIdx = 0; gblIdx < gimbal.gimbalTransforms.Count; gblIdx++)
                        {
                            initRots.Add(gimbal.gimbalTransforms[gblIdx].localRotation); // store the current rotation
                            gimbal.gimbalTransforms[gblIdx].localRotation = gimbal.initRots[gblIdx]; // set the rotation to the "initial" value
                        }
                    }
                    for (int xfrmIdx = 0; xfrmIdx < engine.thrustTransforms.Count; xfrmIdx++)
                    {
                        // iterate through each thrust transform.  Some engines have multiple transforms for
                        // thrust and/or gimbal, so we need to be able to use as many as the part contains.
                        Transform thrustTransform = engine.thrustTransforms[xfrmIdx];
                        Vector3d position = thrustTransform.position;
                        if (gimbal != null && !gimbal.gimbalLock)
                        {
                            Transform gimbalTransform = FindParentTransform(thrustTransform, gimbal.gimbalTransformName, engine.part.transform);
                            if (gimbalTransform != null)
                            {
                                // The gimbal position will not move as the gimbal rotates.  As of KSP 1.0.5, the Vector engine appears
                                // to be the only engine that moves it's thrust transform location, because the transform itself is located
                                // at the end of the nozzel.  Doing so means that our calculated available torque could very well be inaccurate
                                // specifically in the case of roll torque, where it may appear that an axial engine's thrust transform is offset from the
                                // vessel's axial centerline when in fact the axial engine can produce no roll torque.
                                // If there is ever an engine where the thrust transform does not align to the gimbal transform, this assumption
                                // will no longer be valid.
                                position = gimbalTransform.position;  // use the gimbal position as the thrust position
                            }
                        }
                        Vector3d neut = -engine.thrustTransforms[xfrmIdx].forward * engine.finalThrust / engine.thrustTransforms.Count; // the neutral control thrust (force is opposite of exhaust direction)
                        Vector3d relCom = position - centerOfMass;

                        string id = string.Format("{0}[{1}]", engine.part.flightID, xfrmIdx);

                        // calculate the neutral gimbal force vector based on the neutral direction and thrust magnitude
                        ForceVector neutralForce = new ForceVector
                        {
                            Force = neut,
                            ID = id,
                            Position = relCom,
                        };
                        engineNeutVectors.Add(neutralForce);
                        staticEngineTorque += neutralForce.Torque;
                        if (gimbalRange > EPSILON) // only do the gimbal calculation if the range allows it.
                        {
                            Vector3d pitchAxis = Vector3d.Exclude(neut, vesselStarboard);  // pitch rotates about the starboard vector
                            Vector3d yawAxis = Vector3d.Exclude(neut, vesselTop);  // yaw rotates about the top vector
                            Vector3d rollAxis = Vector3d.Exclude(vesselForward, relCom);  // roll rotates about the forward vector
                            ForceVector pitchForce = new ForceVector
                            {
                                Force = Quaternion.AngleAxis(gimbalRange, pitchAxis) * neut,
                                ID = id,
                                Position = relCom
                            };
                            enginePitchVectors.Add(pitchForce);
                            pitchControl += pitchForce.Torque;
                            ForceVector yawForce = new ForceVector
                            {
                                Force = Quaternion.AngleAxis(gimbalRange, yawAxis) * neut,
                                ID = id,
                                Position = relCom
                            };
                            engineYawVectors.Add(yawForce);
                            yawControl += yawForce.Torque;
                            ForceVector rollForce;

                            // because of deflection, sometimes an axial engine will look like it can 
                            // generate roll torque when it cannot do so reliably.  Ignore small offsets.
                            if (rollAxis.sqrMagnitude < 0.02)
                            {
                                rollForce = neutralForce;
                                rollForce = new ForceVector
                                {
                                    Force = neutralForce.Force,
                                    ID = id,
                                    Position = neutralForce.Position
                                };
                            }
                            else
                            {
                                rollForce = new ForceVector
                                {
                                    Force = Quaternion.AngleAxis(gimbalRange, rollAxis) * neut,
                                    ID = id,
                                    Position = relCom
                                };
                            }
                            engineRollVectors.Add(rollForce);
                            rollControl += rollForce.Torque;
                        }
                        else
                        {
                            // if there is no gimbal available, or it's too small, just clone the neutral thrust vector
                            enginePitchVectors.Add(new ForceVector
                            {
                                Force = neutralForce.Force,
                                ID = id,
                                Position = neutralForce.Position
                            });
                            engineYawVectors.Add(new ForceVector
                            {
                                Force = neutralForce.Force,
                                ID = id,
                                Position = neutralForce.Position
                            });
                            engineRollVectors.Add(new ForceVector
                            {
                                Force = neutralForce.Force,
                                ID = id,
                                Position = neutralForce.Position
                            });

                            pitchControl += neutralForce.Torque;
                            yawControl += neutralForce.Torque;
                            rollControl += neutralForce.Torque;
                        }
                    }
                    if (gimbal != null && !gimbal.gimbalLock)
                    {
                        // Reset each of the gimbal rotations back to their previous values.  This prevents
                        // our calculations from interupting the internal gimbal logic, since some gimbals
                        // do not rotate instantly.
                        for (int gblIdx = 0; gblIdx < gimbal.gimbalTransforms.Count; gblIdx++)
                        {
                            gimbal.gimbalTransforms[gblIdx].localRotation = initRots[gblIdx]; // set the rotation to the previous value
                        }
                    }
                }
                else
                {
                    // if an engine is disabled, hide the associated vectors (do this for all transforms)
                    for (int xfrmIdx = 0; xfrmIdx < engine.thrustTransforms.Count; xfrmIdx++)
                    {
                        string key = string.Format("{0}[{1}]", engine.part.flightID, xfrmIdx);
                        if (vEngines.Keys.Contains(key))
                        {
                            vEngines[key].SetShow(false);
                            vEngines.Remove(key);
                        }
                        key = engine.part.flightID.ToString() + "gimbaled";
                        if (vEngines.Keys.Contains(key))
                        {
                            vEngines[key].SetShow(false);
                            vEngines.Remove(key);
                        }
                        key = engine.part.flightID.ToString() + "torque";
                        if (vEngines.Keys.Contains(key))
                        {
                            vEngines[key].SetShow(false);
                            vEngines.Remove(key);
                        }
                        key = engine.part.flightID.ToString() + "control";
                        if (vEngines.Keys.Contains(key))
                        {
                            vEngines[key].SetShow(false);
                            vEngines.Remove(key);
                        }
                        key = engine.part.flightID.ToString() + "position";
                        if (vEngines.Keys.Contains(key))
                        {
                            vEngines[key].SetShow(false);
                            vEngines.Remove(key);
                        }
                    }
                }
            }

            // Because the engines may generate torque about the other 2 axes when trying to rotate about one,
            // use a dot product to get the component of the torque in the desired direction.  Also subtract out
            // the static engine torque, to account for offset placement.  This still does not properly balance
            // engines that are offset, since the available torque may be drastically different between the
            // positive and negative limit.  If there is a large static torque, it's also possible for the
            // calculation to show that there is a small amount of available torque, even though the static
            // torque will actually overpower the control torque.  Eventually it would be nice to modify this
            // calculation to properly handle asymetric gimbal torque, but for now it will need to be a
            // constraint of ship design.
            controlEngineTorque.x = Math.Abs(Vector3d.Dot(pitchControl - staticEngineTorque, vesselStarboard));
            controlEngineTorque.z = Math.Abs(Vector3d.Dot(yawControl - staticEngineTorque, vesselTop));
            controlEngineTorque.y = Math.Abs(Vector3d.Dot(rollControl - staticEngineTorque, vesselForward));

            rawTorque.x += controlEngineTorque.x;
            rawTorque.z += controlEngineTorque.z;
            rawTorque.y += controlEngineTorque.y;

            rawTorque.x = (rawTorque.x + PitchTorqueAdjust) * PitchTorqueFactor;
            rawTorque.z = (rawTorque.z + YawTorqueAdjust) * YawTorqueFactor;
            rawTorque.y = (rawTorque.y + RollTorqueAdjust) * RollTorqueFactor;

            controlTorque = rawTorque + adjustTorque;
            //controlTorque = Vector3d.Scale(rawTorque, adjustTorque);
            //controlTorque = rawTorque;

            double minTorque = EPSILON;
            if (controlTorque.x < minTorque) controlTorque.x = minTorque;
            if (controlTorque.y < minTorque) controlTorque.y = minTorque;
            if (controlTorque.z < minTorque) controlTorque.z = minTorque;
        }

        public Transform FindParentTransform(Transform transform, string name, Transform topLevel)
        {
            if (transform.parent.name == name) return transform.parent;
            else if (transform.parent == null) return null;
            else if (transform.parent == topLevel) return null;
            else return FindParentTransform(transform.parent, name, topLevel);
        }

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
            tgtPitchOmega = pitchRatePI.Update(sampletime, -phiPitch, 0, maxPitchOmega);
            tgtYawOmega = yawRatePI.Update(sampletime, -phiYaw, 0, maxYawOmega);
            if (Math.Abs(phi) > 5 * Math.PI / 180d)
            {
                tgtRollOmega = 0;
                rollRatePI.ResetI();
            }
            else
            {
                tgtRollOmega = rollRatePI.Update(sampletime, -phiRoll, 0, maxRollOmega);
            }

            // Calculate target torque based on PID
            tgtPitchTorque = pitchPI.Update(sampletime, omega.x, tgtPitchOmega, momentOfInertia.x, controlTorque.x);
            tgtYawTorque = yawPI.Update(sampletime, omega.y, tgtYawOmega, momentOfInertia.z, controlTorque.z);
            tgtRollTorque = rollPI.Update(sampletime, omega.z, tgtRollOmega, momentOfInertia.y, controlTorque.y);

            //tgtPitchTorque = pitchPI.Update(sampletime, pitchRate.Update(omega.x), tgtPitchOmega, momentOfInertia.x, controlTorque.x);
            //tgtYawTorque = yawPI.Update(sampletime, yawRate.Update(omega.y), tgtYawOmega, momentOfInertia.z, controlTorque.z);
            //tgtRollTorque = rollPI.Update(sampletime, rollRate.Update(omega.z), tgtRollOmega, momentOfInertia.y, controlTorque.y);
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
                if (Quaternion.Angle(shared.Vessel.Autopilot.SAS.lockedHeading, target) > 15)
                    shared.Vessel.Autopilot.SAS.LockHeading(target, true);
                else
                    shared.Vessel.Autopilot.SAS.lockedHeading = target;
            }
            else
            {
                //TODO: include adjustment for static torque (due to engines)
                double clampAccPitch = Math.Max(Math.Abs(accPitch), 0.005) * 2;
                accPitch = tgtPitchTorque / controlTorque.x;
                if (Math.Abs(accPitch) < EPSILON)
                    accPitch = 0;
                accPitch = Math.Max(Math.Min(accPitch, clampAccPitch), -clampAccPitch);
                c.pitch = (float)accPitch;
                double clampAccYaw = Math.Max(Math.Abs(accYaw), 0.005) * 2;
                accYaw = tgtYawTorque / controlTorque.z;
                if (Math.Abs(accYaw) < EPSILON)
                    accYaw = 0;
                accYaw = Math.Max(Math.Min(accYaw, clampAccYaw), -clampAccYaw);
                c.yaw = (float)accYaw;
                double clampAccRoll = Math.Max(Math.Abs(accRoll), 0.005) * 2;
                accRoll = tgtRollTorque / controlTorque.y;
                if (Math.Abs(accRoll) < EPSILON)
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

            if (ShowAngularVectors && enabled)
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

            if (ShowThrustVectors && enabled)
            {
                foreach (var fv in engineNeutVectors)
                {
                    string key = fv.ID;
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(Color.yellow, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = -fv.Force;
                    vEngines[key].Start = fv.Position;

                    key = fv.ID + "torque";
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(Color.red, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = fv.Torque;
                    vEngines[key].Start = fv.Position;

                    key = fv.ID + "position";
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(Color.cyan, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = fv.Position;
                }
                foreach (var fv in engineRollVectors)
                {
                    string key = fv.ID + "gimbaled";
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(Color.magenta, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = -fv.Force;
                    vEngines[key].Start = fv.Position;

                    key = fv.ID + "control";
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(Color.blue, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = fv.Torque;
                    vEngines[key].Start = fv.Position;
                }
            }
            else
            {
                foreach (string key in vEngines.Keys)
                {
                    vEngines[key].SetShow(false);
                }
                vEngines.Clear();
            }

            if (ShowRCSVectors && enabled)
            {
                foreach (var force in rcsVectors)
                {
                    string key = force.ID;
                    if (!vRcs.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(Color.magenta, 0.25, shared);
                        vecdraw.SetShow(true);
                        vRcs.Add(key, vecdraw);
                    }
                    vRcs[key].Vector = force.Force;
                    vRcs[key].Start = force.Position;

                    key = force.ID + "torque";
                    if (!vRcs.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(Color.yellow, 0.25, shared);
                        vecdraw.SetShow(true);
                        vRcs.Add(key, vecdraw);
                    }
                    vRcs[key].Vector = force.Torque;
                    vRcs[key].Start = force.Position;
                }
            }
            else
            {
                foreach (string key in vRcs.Keys)
                {
                    vRcs[key].SetShow(false);
                }
                vRcs.Clear();
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
            shared.Screen.Print(string.Format("torque gimbal: {0}", controlEngineTorque.x));
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
            shared.Screen.Print(string.Format("torque gimbal: {0}", controlEngineTorque.z));
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
            shared.Screen.Print(string.Format("torque gimbal: {0}", controlEngineTorque.y));
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
                pitchRateWriter.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Kd,MaxOutput");
            }
            if (yawRateWriter == null)
            {
                yawRateWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "yawRate"));
                yawRateWriter.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Kd,MaxOutput");
            }
            if (rollRateWriter == null)
            {
                rollRateWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "rollRate"));
                rollRateWriter.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Kd,MaxOutput");
            }
            if (pitchTorqueWriter == null)
            {
                pitchTorqueWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "pitchTorque"));
                pitchTorqueWriter.WriteLine("LastSampleTime,Input,Setpoint,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MaxOutput");
            }
            if (yawTorqueWriter == null)
            {
                yawTorqueWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "yawTorque"));
                yawTorqueWriter.WriteLine("LastSampleTime,Input,Setpoint,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MaxOutput");
            }
            if (rollTorqueWriter == null)
            {
                rollTorqueWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(FILE_BASE_NAME, fileDateString, shared.Vessel.vesselName, "rollTorque"));
                rollTorqueWriter.WriteLine("LastSampleTime,Input,Setpoint,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MaxOutput");
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

            foreach (string key in vEngines.Keys.ToArray())
            {
                if (vEngines[key] != null)
                {
                    vEngines[key].SetShow(false);
                    vEngines[key].Dispose();
                }
                vEngines.Remove(key);
            }
            foreach (string key in vRcs.Keys.ToArray())
            {
                if (vRcs[key] != null)
                {
                    vRcs[key].SetShow(false);
                    vRcs[key].Dispose();
                }
                vRcs.Remove(key);
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

            SteeringManagerProvider.RemoveInstance(shared.Vessel);
        }

        private struct ForceVector
        {
            public Vector3d Force { get; set; }

            public Vector3d Position { get; set; }

            public Vector3d Torque { get { return Vector3d.Cross(Force, Position); } }

            public string ID { get; set; }
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

            public double Update(double sampleTime, double input, double setpoint, double momentOfInertia, double maxOutput)
            {
                I = momentOfInertia;

                Loop.Ki = momentOfInertia * Math.Pow(4.0 / ts, 2);
                Loop.Kp = 2 * Math.Pow(momentOfInertia * Loop.Ki, 0.5);
                return Loop.Update(sampleTime, input, setpoint, maxOutput);
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
                return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                    Loop.LastSampleTime, Loop.Input, Loop.Setpoint, Loop.Error, Loop.ErrorSum, Loop.Output, Loop.Kp, Loop.Ki, Tr, Ts, I, Loop.MaxOutput);
            }
        }
    }
}