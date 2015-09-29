using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
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
        public static Dictionary<string, SteeringManager> AllInstances = new Dictionary<string, SteeringManager>();

        public static SteeringManager GetInstance(SharedObjects shared)
        {
            string key = shared.Vessel.id.ToString();
            if (AllInstances.Keys.Contains(key))
            {
                SteeringManager instance = AllInstances[key];
                if (!instance.SubscribedParts.Contains(shared.KSPPart.flightID))
                {
                    AllInstances[key].SubscribedParts.Add(shared.KSPPart.flightID);
                }
                return AllInstances[key];
            }
            SteeringManager sm = new SteeringManager(shared);
            sm.KeyId = key;
            sm.SubscribedParts.Add(shared.KSPPart.flightID);
            AllInstances.Add(key, sm);
            return sm;
        }

        public static SteeringManager SwapInstance(SharedObjects shared, SteeringManager oldInstance)
        {
            if (shared.Vessel.id.ToString() == oldInstance.KeyId) return oldInstance;
            if (oldInstance.SubscribedParts.Contains(shared.KSPPart.flightID)) oldInstance.SubscribedParts.Remove(shared.KSPPart.flightID);
            SteeringManager instance = DeepCopy(oldInstance, shared);
            if (oldInstance.enabled)
            {
                if (oldInstance.partId == shared.KSPPart.flightID)
                {
                    oldInstance.DisableControl();
                    instance.EnableControl(shared);
                    instance.Value = oldInstance.Value;
                }
            }
            return instance;
        }

        public static void RemoveInstance(Guid vesselId)
        {
            if (AllInstances.ContainsKey(vesselId.ToString()))
            {
                AllInstances[vesselId.ToString()].Dispose();
                AllInstances.Remove(vesselId.ToString());
            }
        }

        public static SteeringManager DeepCopy(SteeringManager oldInstance, SharedObjects shared)
        {
            SteeringManager instance = GetInstance(shared);
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

        public string KeyId;

        public List<uint> SubscribedParts = new List<uint>();

        private SharedObjects shared;

        private uint partId = 0;

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

        private string fileBaseName = "{0}-{1}-{2}.csv";
        private string fileDateString = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

        public object Value { get; set; }

        public Direction TargetDirection { get { return this.GetDirectionFromValue(); } }

        private Transform vesselTransform;

        private TorquePI pitchPI = new TorquePI();
        private TorquePI yawPI = new TorquePI();
        private TorquePI rollPI = new TorquePI();

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

        private MovingAverage pitchRate = new MovingAverage() { SampleLimit = 5 };
        private MovingAverage yawRate = new MovingAverage() { SampleLimit = 5 };
        private MovingAverage rollRate = new MovingAverage() { SampleLimit = 5 };

        private MovingAverage pitchTorqueCalc = new MovingAverage() { SampleLimit = 5 };
        private MovingAverage yawTorqueCalc = new MovingAverage() { SampleLimit = 5 };
        private MovingAverage rollTorqueCalc = new MovingAverage() { SampleLimit = 5 };

        private bool EnableTorqueAdjust { get; set; }

        private MovingAverage pitchMOICalc = new MovingAverage() { SampleLimit = 5 };
        private MovingAverage yawMOICalc = new MovingAverage() { SampleLimit = 5 };
        private MovingAverage rollMOICalc = new MovingAverage() { SampleLimit = 5 };

        private bool EnableMOIAdjust { get; set; }

        public MovingAverage AverageDuration = new MovingAverage() { SampleLimit = 60 };

        private List<ThrustVector> allEngineVectors = new List<ThrustVector>();

        #region doubles
        public const double RadToDeg = 180d / Math.PI;

        private double epsilon = 0.00001;

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

        private double renderMultiplier = 50;

        public double PitchTorqueAdjust { get; set; }
        public double YawTorqueAdjust { get; set; }
        public double RollTorqueAdjust { get; set; }

        public double PitchTorqueFactor { get; set; }
        public double YawTorqueFactor { get; set; }
        public double RollTorqueFactor { get; set; }
        #endregion doubles

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

        private Vector3d omega = Vector3d.zero; // x: pitch, y: yaw, z: roll
        private Vector3d lastOmega = Vector3d.zero;
        private Vector3d angularAcceleration = Vector3d.zero;
        private Vector3d momentOfInertia = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d measuredMomentOfInertia = Vector3d.zero;
        private Vector3d adjustMomentOfInertia = Vector3d.one;
        private Vector3d measuredTorque = Vector3d.zero;
        private Vector3d adjustTorque = Vector3d.zero;
        private Vector3d staticTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d controlTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d rawTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d staticEngineTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll
        private Vector3d controlEngineTorque = Vector3d.zero; // x: pitch, z: yaw, y: roll

        private List<ForceVector> rcsVectors = new List<ForceVector>();
        private List<ForceVector> engineNeutVectors = new List<ForceVector>();
        private List<ForceVector> enginePitchVectors = new List<ForceVector>();
        private List<ForceVector> engineYawVectors = new List<ForceVector>();
        private List<ForceVector> engineRollVectors = new List<ForceVector>();

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
        public VectorRenderer vWorldZ;

        private VectorRenderer vOmegaX;
        private VectorRenderer vOmegaY;
        private VectorRenderer vOmegaZ;

        private VectorRenderer vTgtTorqueX;
        private VectorRenderer vTgtTorqueY;
        private VectorRenderer vTgtTorqueZ;

        private Dictionary<string, VectorRenderer> vEngines = new Dictionary<string, VectorRenderer>();
        private Dictionary<string, VectorRenderer> vRcs = new Dictionary<string, VectorRenderer>();

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

            EnableMOIAdjust = false;
            EnableTorqueAdjust = true;

            MaxStoppingTime = 1;

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
            AddSuffix("ENABLEMOIADJUST", new SetSuffix<bool>(() => EnableMOIAdjust, value => EnableMOIAdjust = value));
#endif
        }

        public void EnableControl(SharedObjects shared)
        {
            if (enabled && this.partId != shared.KSPPart.flightID)
                throw new kOS.Safe.Exceptions.KOSException("Steering Manager on this ship is already in use by another processor.");
            this.shared = shared;
            this.partId = shared.KSPPart.flightID;
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
            if (enabled && this.partId != shared.KSPPart.flightID)
                throw new kOS.Safe.Exceptions.KOSException("Cannon unbind Steering Manager on this ship in use by another processor.");
            this.partId = 0;
            Enabled = false;
        }

        public VectorRenderer InitVectorRenderer(UnityEngine.Color c, double width, SharedObjects shared)
        {
            VectorRenderer rend = new VectorRenderer(shared.UpdateHandler, shared)
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
            update(c);
        }

        public void OnRemoteTechPilot(FlightCtrlState c)
        {
            update(c);
        }

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        private void update(FlightCtrlState c)
        {
            if (Value != null)
            {
                lastSessionTime = sessionTime;
                sessionTime = Math.Round(Planetarium.GetUniversalTime(), 3);
                //if (sessionTime > lastSessionTime)
                //{
                //}
                sw.Reset();
                sw.Start();
                UpdateStateVectors();
                UpdateTorque();
                UpdatePredictionPI();
                UpdateControl(c);
                if (ShowSteeringStats) PrintDebug();
                if (WriteCSVFiles) WriteCSVs();
                UpdateVectorRenders();
                sw.Stop();
                AverageDuration.Update((double)sw.ElapsedTicks / (double)System.TimeSpan.TicksPerMillisecond);
            }
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
            throw new kOS.Safe.Exceptions.KOSWrongControlValueTypeException(
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
                if (EnableMOIAdjust)
                {
                    measuredMomentOfInertia = new Vector3d(tgtPitchTorque / angularAcceleration.x, tgtRollTorque / angularAcceleration.y, tgtYawTorque / angularAcceleration.z);

                    if (Math.Abs(accPitch) > epsilon)
                    {
                        adjustMomentOfInertia.x = pitchMOICalc.Update(Math.Abs(measuredMomentOfInertia.x) / momentOfInertia.x);
                    }
                    if (Math.Abs(accYaw) > epsilon)
                    {
                        adjustMomentOfInertia.z = yawMOICalc.Update(Math.Abs(measuredMomentOfInertia.z) / momentOfInertia.z);
                    }
                    if (Math.Abs(accRoll) > epsilon)
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

                if (Math.Abs(accPitch) > epsilon)
                {
                    adjustTorque.x = Math.Min(Math.Abs(pitchTorqueCalc.Update(measuredTorque.x / accPitch)) - rawTorque.x, 0);
                    //adjustTorque.x = Math.Abs(pitchTorqueCalc.Update(measuredTorque.x / accPitch) / rawTorque.x);
                }
                else adjustTorque.x = Math.Abs(pitchTorqueCalc.Update(pitchTorqueCalc.Mean));
                if (Math.Abs(accYaw) > epsilon)
                {
                    adjustTorque.z = Math.Min(Math.Abs(yawTorqueCalc.Update(measuredTorque.z / accYaw)) - rawTorque.z, 0);
                    //adjustTorque.z = Math.Abs(yawTorqueCalc.Update(measuredTorque.z / accYaw) / rawTorque.z);
                }
                else adjustTorque.z = Math.Abs(yawTorqueCalc.Update(yawTorqueCalc.Mean));
                if (Math.Abs(accRoll) > epsilon)
                {
                    adjustTorque.y = Math.Min(Math.Abs(rollTorqueCalc.Update(measuredTorque.y / accRoll)) - rawTorque.y, 0);
                    //adjustTorque.y = Math.Abs(rollTorqueCalc.Update(measuredTorque.y / accRoll) / rawTorque.y);
                }
                else adjustTorque.y = Math.Abs(rollTorqueCalc.Update(rollTorqueCalc.Mean));
            }
        }

        public void UpdateTorque()
        {
            // staticTorque will represent engine torque due to imbalanced placement
            staticTorque = new Vector3d(0, 0, 0);
            // controlTorque is the maximum amount of torque applied by setting a control to 1.0.
            controlTorque = new Vector3d(0, 0, 0);
            rawTorque = Vector3d.zero;
            Vector3d relCom;
            // clear all of the force vector storage lists to be refreshed during the update.
            allEngineVectors.Clear();
            rcsVectors.Clear();
            engineNeutVectors.Clear();
            enginePitchVectors.Clear();
            engineYawVectors.Clear();
            engineRollVectors.Clear();
            foreach (Part part in shared.Vessel.Parts)
            {
                relCom = part.Rigidbody.worldCenterOfMass - centerOfMass;
                Quaternion gimbalRotation = new Quaternion();
                float gimbalRange = 0;
                List<ThrustVector> engineVectors = new List<ThrustVector>();
                foreach (PartModule pm in part.Modules)
                {
                    ModuleReactionWheel wheel = pm as ModuleReactionWheel;
                    if (wheel != null)
                    {
                        if (wheel.isActiveAndEnabled && wheel.State == ModuleReactionWheel.WheelState.Active)
                        {
                            // TODO: Check to see if the component values depend on part orientation, and implement if needed.
                            rawTorque.x += wheel.PitchTorque;
                            rawTorque.z += wheel.YawTorque;
                            rawTorque.y += wheel.RollTorque;
                        }
                        continue;
                    }
                    ModuleRCS rcs = pm as ModuleRCS;
                    if (rcs != null)
                    {
                        //if (shared.Vessel.ActionGroups[KSPActionGroup.RCS] && rcs.rcsEnabled && !rcs.part.ShieldedFromAirstream)
                        if (shared.Vessel.ActionGroups[KSPActionGroup.RCS] && !rcs.part.ShieldedFromAirstream)
                        {
                            for (int i = 0; i < rcs.thrusterTransforms.Count; i++)
                            {
                                Transform thrustdir = rcs.thrusterTransforms[i];
                                ForceVector force = new ForceVector()
                                {
                                    Force = thrustdir.up * rcs.thrusterPower,
                                    Position = thrustdir.position - centerOfMass,
                                    // We need to adjust the relative center of mass because the rigid body
                                    // will report the position of the parent part
                                    ID = part.flightID.ToString() + "-" + i.ToString()
                                };
                                Vector3d torque = force.Torque;
                                rcsVectors.Add(force);

                                // component values of the local torque are calculated using the dot product with the rotation axis.
                                // Only using positive contributions, which is only valid when symmetric placement is assumed
                                rawTorque.x += Math.Max(Vector3d.Dot(torque, vesselStarboard), 0);
                                rawTorque.z += Math.Max(Vector3d.Dot(torque, vesselTop), 0);
                                rawTorque.y += Math.Max(Vector3d.Dot(torque, vesselForward), 0);
                            }
                            continue;
                        }
                    }
                    ModuleGimbal gimbal = pm as ModuleGimbal;
                    if (gimbal != null)
                    {
                        if (gimbal.gimbalLock)
                        {
                            foreach (var transform in gimbal.gimbalTransforms)
                            {
                                gimbalRotation = transform.rotation;
                                gimbalRange = 0;
                                foreach (ThrustVector tv in engineVectors)
                                {
                                    tv.Rotation = gimbalRotation;
                                    tv.GimbalRange = gimbalRange;
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < gimbal.gimbalTransforms.Count; i++)
                            {
                                Transform transform = gimbal.gimbalTransforms[i];
                                // init rotations are stored in a local scope.  Need to convert back to global scope.
                                var initRotation = transform.localRotation;
                                transform.localRotation = gimbal.initRots[i];
                                //vEngines[key].Start = transform.localPosition;
                                gimbalRotation = transform.rotation;
                                //gimbalRange = gimbal.gimbalRange * gimbal.gimbalLimiter / 100;
                                gimbalRange = gimbal.gimbalRange;
                                transform.localRotation = initRotation;
                                foreach (ThrustVector tv in engineVectors)
                                {
                                    tv.Rotation = gimbalRotation;
                                    tv.GimbalRange = gimbalRange;
                                }
                            }
                        }
                        continue;
                    }
                    ModuleEngines engine = pm as ModuleEngines;
                    if (engine != null)
                    {
                        if (engine.isActiveAndEnabled && engine.EngineIgnited)
                        {
                            foreach (var transform in engine.thrustTransforms)
                            {
                                relCom = transform.position - centerOfMass;
                                engineVectors.Add(new ThrustVector()
                                {
                                    Rotation = gimbalRotation,
                                    GimbalRange = gimbalRange,
                                    //ThrustMag = engine.GetMaxThrust(),
                                    ThrustMag = engine.finalThrust,
                                    Position = relCom,
                                    PartId = part.flightID.ToString()
                                });
                            }
                        }
                        else
                        {
                            string key = part.flightID.ToString();
                            if (vEngines.Keys.Contains(key))
                            {
                                vEngines[key].SetShow(false);
                                vEngines.Remove(key);
                            }
                            key = part.flightID.ToString() + "gimbaled";
                            if (vEngines.Keys.Contains(key))
                            {
                                vEngines[key].SetShow(false);
                                vEngines.Remove(key);
                            }
                            key = part.flightID.ToString() + "torque";
                            if (vEngines.Keys.Contains(key))
                            {
                                vEngines[key].SetShow(false);
                                vEngines.Remove(key);
                            }
                            key = part.flightID.ToString() + "control";
                            if (vEngines.Keys.Contains(key))
                            {
                                vEngines[key].SetShow(false);
                                vEngines.Remove(key);
                            }
                            key = part.flightID.ToString() + "position";
                            if (vEngines.Keys.Contains(key))
                            {
                                vEngines[key].SetShow(false);
                                vEngines.Remove(key);
                            }
                        }
                        continue;
                    }
                }
                allEngineVectors.AddRange(engineVectors);
            }
            staticEngineTorque.Zero();
            controlEngineTorque.Zero();
            Vector3d pitchControl = Vector3d.zero;
            Vector3d yawControl = Vector3d.zero;
            Vector3d rollControl = Vector3d.zero;
            foreach (var tv in allEngineVectors)
            {
                Vector3d[] vectors = tv.GetTorque(vesselForward, vesselTop, vesselStarboard);
                staticEngineTorque += vectors[0];
                pitchControl += vectors[1];
                yawControl += vectors[2];
                rollControl += vectors[3];
            }
            // Record the engine torque in a local vessel reference frame
            controlEngineTorque.x = pitchControl.magnitude;
            controlEngineTorque.z = yawControl.magnitude;
            controlEngineTorque.y = rollControl.magnitude;

            rawTorque.x += controlEngineTorque.x;
            rawTorque.z += controlEngineTorque.z;
            rawTorque.y += controlEngineTorque.y;

            rawTorque.x = (rawTorque.x + PitchTorqueAdjust) * PitchTorqueFactor;
            rawTorque.z = (rawTorque.z + YawTorqueAdjust) * YawTorqueFactor;
            rawTorque.y = (rawTorque.y + RollTorqueAdjust) * RollTorqueFactor;

            controlTorque = rawTorque + adjustTorque;
            //controlTorque = Vector3d.Scale(rawTorque, adjustTorque);
            //controlTorque = rawTorque;

            double minTorque = epsilon;
            if (controlTorque.x < minTorque) controlTorque.x = minTorque;
            if (controlTorque.y < minTorque) controlTorque.y = minTorque;
            if (controlTorque.z < minTorque) controlTorque.z = minTorque;
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
                return;
            }
            else
            {
                //TODO: include adjustment for static torque (due to engines)

                accPitch = tgtPitchTorque / controlTorque.x;
                if (Math.Abs(accPitch) < epsilon)
                    accPitch = 0;
                c.pitch = (float)accPitch;
                accYaw = tgtYawTorque / controlTorque.z;
                if (Math.Abs(accYaw) < epsilon)
                    accYaw = 0;
                c.yaw = (float)accYaw;
                accRoll = tgtRollTorque / controlTorque.y;
                if (Math.Abs(accRoll) < epsilon)
                    accRoll = 0;
                c.roll = (float)accRoll;
            }
        }

        public void UpdateVectorRenders()
        {
            if (ShowFacingVectors && enabled)
            {
                if (vForward == null)
                {
                    vForward = InitVectorRenderer(UnityEngine.Color.red, 1, shared);
                }
                if (vTop == null)
                {
                    vTop = InitVectorRenderer(UnityEngine.Color.red, 1, shared);
                }
                if (vStarboard == null)
                {
                    vStarboard = InitVectorRenderer(UnityEngine.Color.red, 1, shared);
                }

                vForward.Vector = vesselForward * renderMultiplier;
                vTop.Vector = vesselTop * renderMultiplier;
                vStarboard.Vector = vesselStarboard * renderMultiplier;

                if (vTgtForward == null)
                {
                    vTgtForward = InitVectorRenderer(UnityEngine.Color.blue, 1, shared);
                }
                if (vTgtTop == null)
                {
                    vTgtTop = InitVectorRenderer(UnityEngine.Color.blue, 1, shared);
                }
                if (vTgtStarboard == null)
                {
                    vTgtStarboard = InitVectorRenderer(UnityEngine.Color.blue, 1, shared);
                }

                vTgtForward.Vector = targetForward * renderMultiplier * 0.75f;
                vTgtTop.Vector = targetTop * renderMultiplier * 0.75f;
                vTgtStarboard.Vector = targetStarboard * renderMultiplier * 0.75f;

                if (vWorldX == null)
                {
                    vWorldX = InitVectorRenderer(UnityEngine.Color.white, 1, shared);
                }
                if (vWorldY == null)
                {
                    vWorldY = InitVectorRenderer(UnityEngine.Color.white, 1, shared);
                }
                if (vWorldZ == null)
                {
                    vWorldZ = InitVectorRenderer(UnityEngine.Color.white, 1, shared);
                }

                vWorldX.Vector = new Vector3d(1, 0, 0) * renderMultiplier * 2;
                vWorldY.Vector = new Vector3d(0, 1, 0) * renderMultiplier * 2;
                vWorldZ.Vector = new Vector3d(0, 0, 1) * renderMultiplier * 2;

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
                    vOmegaX = InitVectorRenderer(UnityEngine.Color.cyan, 1, shared);
                }
                if (vOmegaY == null)
                {
                    vOmegaY = InitVectorRenderer(UnityEngine.Color.cyan, 1, shared);
                }
                if (vOmegaZ == null)
                {
                    vOmegaZ = InitVectorRenderer(UnityEngine.Color.cyan, 1, shared);
                }

                vOmegaX.Vector = vesselTop * omega.x * renderMultiplier * 100f;
                vOmegaX.Start = vesselForward * renderMultiplier;
                vOmegaY.Vector = vesselStarboard * omega.y * renderMultiplier * 100f;
                vOmegaY.Start = vesselForward * renderMultiplier;
                vOmegaZ.Vector = vesselStarboard * omega.z * renderMultiplier * 100f;
                vOmegaZ.Start = vesselTop * renderMultiplier;

                if (vTgtTorqueX == null)
                {
                    vTgtTorqueX = InitVectorRenderer(UnityEngine.Color.green, 1, shared);
                }
                if (vTgtTorqueY == null)
                {
                    vTgtTorqueY = InitVectorRenderer(UnityEngine.Color.green, 1, shared);
                }
                if (vTgtTorqueZ == null)
                {
                    vTgtTorqueZ = InitVectorRenderer(UnityEngine.Color.green, 1, shared);
                }

                vTgtTorqueX.Vector = vesselTop * tgtPitchOmega * renderMultiplier * 100f;
                vTgtTorqueX.Start = vesselForward * renderMultiplier;
                //vTgtTorqueX.SetLabel("tgtPitchOmega: " + tgtPitchOmega);
                vTgtTorqueY.Vector = vesselStarboard * tgtRollOmega * renderMultiplier * 100f;
                vTgtTorqueY.Start = vesselTop * renderMultiplier;
                //vTgtTorqueY.SetLabel("tgtRollOmega: " + tgtRollOmega);
                vTgtTorqueZ.Vector = vesselStarboard * tgtYawOmega * renderMultiplier * 100f;
                vTgtTorqueZ.Start = vesselForward * renderMultiplier;
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
                foreach (var tv in allEngineVectors)
                {
                    string key = tv.PartId;
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(UnityEngine.Color.yellow, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = tv.NeutralForce.Force;
                    vEngines[key].Start = tv.Position;

                    key = tv.PartId + "gimbaled";
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(UnityEngine.Color.magenta, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    //vEngines[key].Vector = tv.PitchForce.Force;
                    //vEngines[key].Vector = tv.YawForce.Force;
                    vEngines[key].Vector = tv.RollForce.Force;
                    vEngines[key].Start = tv.Position;

                    key = tv.PartId + "torque";
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(UnityEngine.Color.red, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = tv.NeutralForce.Torque;
                    vEngines[key].Start = tv.Position;

                    key = tv.PartId + "control";
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(UnityEngine.Color.blue, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    //vEngines[key].Vector = tv.PitchForce.Torque;
                    //vEngines[key].Vector = tv.YawForce.Torque;
                    vEngines[key].Vector = tv.RollForce.Torque;
                    vEngines[key].Start = tv.Position;

                    key = tv.PartId + "position";
                    if (!vEngines.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(UnityEngine.Color.cyan, 0.25, shared);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = tv.Position;
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
                        var vecdraw = InitVectorRenderer(UnityEngine.Color.magenta, 0.25, shared);
                        vecdraw.SetShow(true);
                        vRcs.Add(key, vecdraw);
                    }
                    vRcs[key].Vector = force.Force;
                    vRcs[key].Start = force.Position;

                    key = force.ID + "torque";
                    if (!vRcs.ContainsKey(key))
                    {
                        var vecdraw = InitVectorRenderer(UnityEngine.Color.yellow, 0.25, shared);
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
                    string.Format(fileBaseName, fileDateString, shared.Vessel.vesselName, "pitchRate"));
                pitchRateWriter.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Kd,MaxOutput");
            }
            if (yawRateWriter == null)
            {
                yawRateWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(fileBaseName, fileDateString, shared.Vessel.vesselName, "yawRate"));
                yawRateWriter.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Kd,MaxOutput");
            }
            if (rollRateWriter == null)
            {
                rollRateWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(fileBaseName, fileDateString, shared.Vessel.vesselName, "rollRate"));
                rollRateWriter.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Kd,MaxOutput");
            }
            if (pitchTorqueWriter == null)
            {
                pitchTorqueWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(fileBaseName, fileDateString, shared.Vessel.vesselName, "pitchTorque"));
                pitchTorqueWriter.WriteLine("LastSampleTime,Input,Setpoint,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MaxOutput");
            }
            if (yawTorqueWriter == null)
            {
                yawTorqueWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(fileBaseName, fileDateString, shared.Vessel.vesselName, "yawTorque"));
                yawTorqueWriter.WriteLine("LastSampleTime,Input,Setpoint,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MaxOutput");
            }
            if (rollTorqueWriter == null)
            {
                rollTorqueWriter = KSP.IO.File.AppendText<PIDLoop>(
                    string.Format(fileBaseName, fileDateString, shared.Vessel.vesselName, "rollTorque"));
                rollTorqueWriter.WriteLine("LastSampleTime,Input,Setpoint,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MaxOutput");
            }
            if (adjustTorqueWriter == null)
            {
                adjustTorqueWriter = KSP.IO.File.AppendText<SteeringManager>(
                    string.Format(fileBaseName, fileDateString, shared.Vessel.vesselName, "adjustTorque"));
                adjustTorqueWriter.WriteLine("LastSampleTime,Target Pitch,Measured Pitch,Average Adjust Pitch,Raw Pitch,Target Yaw,Measured Yaw,Average Adjust Yaw,Raw Yaw,Target Roll,Measured Roll,Average Adjust Roll,Raw Roll,Samples Roll");
            }

            pitchRateWriter.WriteLine(pitchRatePI.ToCSVString());
            yawRateWriter.WriteLine(yawRatePI.ToCSVString());
            rollRateWriter.WriteLine(rollRatePI.ToCSVString());

            pitchTorqueWriter.WriteLine(pitchPI.ToCSVString());
            yawTorqueWriter.WriteLine(yawPI.ToCSVString());
            rollTorqueWriter.WriteLine(rollPI.ToCSVString());

            adjustTorqueWriter.WriteLine(string.Join(",", new string[] {
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

        public void RemoveInstance(SharedObjects shared)
        {
            if (enabled && partId == shared.KSPPart.flightID)
            {
                DisableControl();
            }
            if (SubscribedParts.Contains(shared.KSPPart.flightID)) SubscribedParts.Remove(shared.KSPPart.flightID);
            if (SubscribedParts.Count == 0)
            {
                if (AllInstances.ContainsKey(KeyId)) AllInstances.Remove(KeyId);
                this.Dispose();
            }
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

            if (AllInstances.Keys.Contains(KeyId)) AllInstances.Remove(KeyId);
        }

        public struct ForceVector
        {
            public Vector3d Force { get; set; }

            public Vector3d Position { get; set; }

            public Vector3d Torque { get { return Vector3d.Cross(Force, Position); } }

            public string ID { get; set; }
        }

        public class ThrustVector
        {
            public Quaternion Rotation;
            public float GimbalRange = 0;
            public float ThrustMag = 0;
            public Vector3d Position = Vector3d.zero;
            public string PartId;

            public ForceVector NeutralForce;
            public ForceVector PitchForce;
            public ForceVector YawForce;
            public ForceVector RollForce;

            public ThrustVector()
            {
            }

            public Vector3d Thrust { get { return Rotation * Vector3d.forward * ThrustMag; } }

            public Vector3d[] GetTorque(Vector3d forward, Vector3d top, Vector3d starboard)
            {
                Vector3d[] ret = new Vector3d[4];
                if (ThrustMag > 0.0001)
                {
                    Vector3d thrust = Thrust;
                    Vector3d neut = thrust;
                    NeutralForce = new ForceVector()
                    {
                        Force = Thrust,
                        Position = Position
                    };
                    ret[0] = NeutralForce.Torque;
                    if (GimbalRange > 0.0001)
                    {
                        Vector3d pitchAxis = Vector3d.Exclude(neut, starboard);
                        Vector3d yawAxis = Vector3d.Exclude(neut, top);
                        Vector3d rollAxis = Vector3d.Exclude(forward, Position);
                        PitchForce = new ForceVector()
                        {
                            Force = Quaternion.AngleAxis(GimbalRange, pitchAxis) * neut,
                            Position = Position
                        };
                        YawForce = new ForceVector()
                        {
                            Force = Quaternion.AngleAxis(GimbalRange, yawAxis) * neut,
                            Position = Position
                        };
                        if (rollAxis.sqrMagnitude < 0.02)
                        {
                            //RollForce = new ForceVector()
                            //{
                            //    Force = NeutralForce.Force,
                            //    Position = NeutralForce.Position
                            //};
                            RollForce = new ForceVector()
                            {
                                Force = Vector3d.zero,
                                Position = Vector3d.zero
                            };
                        }
                        else
                        {
                            RollForce = new ForceVector()
                            {
                                Force = Quaternion.AngleAxis(GimbalRange, rollAxis) * neut,
                                Position = Position
                            };
                        }
                    }
                    else
                    {
                        PitchForce = new ForceVector()
                        {
                            Force = NeutralForce.Force,
                            Position = NeutralForce.Position
                        };
                        YawForce = new ForceVector()
                        {
                            Force = NeutralForce.Force,
                            Position = NeutralForce.Position
                        };
                        RollForce = new ForceVector()
                        {
                            Force = NeutralForce.Force,
                            Position = NeutralForce.Position
                        };
                    }
                    ret[1] = PitchForce.Torque;
                    ret[2] = YawForce.Torque;
                    ret[3] = RollForce.Torque;
                }
                else
                {
                    NeutralForce = new ForceVector()
                    {
                        Force = Vector3d.zero,
                        Position = Vector3d.zero
                    };
                    PitchForce = new ForceVector()
                    {
                        Force = Vector3d.zero,
                        Position = Vector3d.zero
                    };
                    YawForce = new ForceVector()
                    {
                        Force = Vector3d.zero,
                        Position = Vector3d.zero
                    };
                    RollForce = new ForceVector()
                    {
                        Force = Vector3d.zero,
                        Position = Vector3d.zero
                    };
                    ret[0] = Vector3d.zero;
                    ret[1] = Vector3d.zero;
                    ret[2] = Vector3d.zero;
                    ret[3] = Vector3d.zero;
                }
                return ret;
            }
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
                Ts = 1;
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

        public class MovingAverage
        {
            public List<double> Values { get; set; }

            public double Mean { get; private set; }

            public int ValueCount { get { return Values.Count; } }

            public int SampleLimit { get; set; }

            public MovingAverage()
            {
                Reset();
                SampleLimit = 30;
            }

            public void Reset()
            {
                Mean = 0;
                if (Values == null) Values = new List<double>();
                else Values.Clear();
            }

            public double Update(double value)
            {
                if (!(double.IsInfinity(value) || double.IsNaN(value)))
                {
                    Values.Add(value);
                    while (Values.Count > SampleLimit)
                    {
                        Values.RemoveAt(0);
                    }
                    //if (Values.Count > 5) Mean = Values.OrderBy(e => e).Skip(1).Take(Values.Count - 2).Average();
                    //else Mean = Values.Average();
                    //Mean = Values.Average();
                    double sum = 0;
                    double count = 0;
                    double max = double.MinValue;
                    double min = double.MaxValue;
                    for (int i = 0; i < Values.Count; i++)
                    {
                        double val = Values[i];
                        if (val > max)
                        {
                            if (max != double.MinValue)
                            {
                                sum += max;
                                count++;
                            }
                            max = val;
                        }
                        else if (val < min)
                        {
                            if (min != double.MaxValue)
                            {
                                sum += min;
                                count++;
                            }
                            min = val;
                        }
                        else
                        {
                            sum += val;
                            count++;
                        }
                    }
                    if (count == 0)
                    {
                        if (max != double.MinValue)
                        {
                            sum += max;
                            count++;
                        }
                        if (min != double.MaxValue)
                        {
                            sum += min;
                            count++;
                        }
                    }
                    Mean = sum / count;
                    return Mean;
                }
                return value;
            }
        }
    }
}