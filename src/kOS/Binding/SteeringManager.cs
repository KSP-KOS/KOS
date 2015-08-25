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
                AllInstances[key].SubscribedParts.Add(shared.KSPPart.flightID);
                return AllInstances[key];
            }
            SteeringManager sm = new SteeringManager(shared);
            sm.KeyId = key;
            sm.SubscribedParts.Add(shared.KSPPart.flightID);
            AllInstances.Add(key, sm);
            return sm;
        }

        public static SteeringManager GetInstance(Vessel vessel)
        {
            string key = vessel.id.ToString();
            if (AllInstances.Keys.Contains(key))
            {
                return AllInstances[key];
            }
            return null;
        }

        public string KeyId;

        public List<uint> SubscribedParts = new List<uint>();

        private SharedObjects shared;
        //Vessel vessel;

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
                    InitVectorRenderers();
                    ResetIs();
                }
                else
                {
                    HideVectorsRenderers();
                    if (pitchRateWriter != null) pitchRateWriter.Dispose();
                    if (yawRateWriter != null) yawRateWriter.Dispose();
                    if (rollRateWriter != null) rollRateWriter.Dispose();
                    if (pitchTorqueWriter != null) pitchTorqueWriter.Dispose();
                    if (yawTorqueWriter != null) yawTorqueWriter.Dispose();
                    if (rollTorqueWriter != null) rollTorqueWriter.Dispose();

                    pitchRateWriter = null;
                    yawRateWriter = null;
                    rollRateWriter = null;
                    pitchTorqueWriter = null;
                    yawTorqueWriter = null;
                    rollTorqueWriter = null;
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

        private MovingAverage pitchRate = new MovingAverage() { SampleLimit = 5 };
        private MovingAverage yawRate = new MovingAverage() { SampleLimit = 5 };
        private MovingAverage rollRate = new MovingAverage() { SampleLimit = 5 };

        private List<ThrustVector> allEngineVectors = new List<ThrustVector>();

        #region doubles

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

        private Vector3d omega;
        private Vector3d momentOfInertia;
        private Vector3d staticTorque = Vector3d.zero;
        private Vector3d controlTorque = Vector3d.zero;
        private Vector3d staticEngineTorque = Vector3d.zero;
        private Vector3d controlEngineTorque = Vector3d.zero;

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
            ShowFacingVectors = true;
            ShowAngularVectors = true;
            ShowThrustVectors = false;
            ShowRCSVectors = false;
            ShowSteeringStats = false;
            WriteCSVFiles = true;

            if (pitchRateWriter != null) pitchRateWriter.Dispose();
            if (yawRateWriter != null) yawRateWriter.Dispose();
            if (rollRateWriter != null) rollRateWriter.Dispose();

            if (pitchTorqueWriter != null) pitchTorqueWriter.Dispose();
            if (yawTorqueWriter != null) yawTorqueWriter.Dispose();
            if (rollTorqueWriter != null) rollTorqueWriter.Dispose();

            pitchRateWriter = null;
            yawRateWriter = null;
            rollRateWriter = null;

            pitchTorqueWriter = null;
            yawTorqueWriter = null;
            rollTorqueWriter = null;
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
            AddSuffix("ANGLEERROR", new Suffix<double>(() => phi));
            AddSuffix("PITCHERROR", new Suffix<double>(() => phiPitch));
            AddSuffix("YAWERROR", new Suffix<double>(() => phiYaw));
            AddSuffix("ROLLERROR", new Suffix<double>(() => phiRoll));
        }

        public void EnableControl(SharedObjects shared)
        {
            if (enabled && this.partId != shared.KSPPart.flightID)
                throw new kOS.Safe.Exceptions.KOSException("Steering Manager on this ship is already in use by another processor.");
            this.shared = shared;
            this.partId = shared.KSPPart.flightID;
            ResetIs();
            Enabled = true;
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

        public void InitVectorRenderers()
        {
            if (vForward != null)
            {
                vForward.SetShow(false);
            }
            Color c = UnityEngine.Color.red;
            vForward = InitVectorRenderer(c, 0.5, shared);
            vForward = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1
            };
            //vForward.SetLabel("vForward");
            vForward.SetShow(true);
            if (vTop != null)
            {
                vTop.SetShow(false);
            }
            vTop = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1
            };
            //vTop.SetLabel("vTop");
            vTop.SetShow(true);
            if (vStarboard != null)
            {
                vStarboard.SetShow(false);
            }
            vStarboard = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1
            };
            //vStarboard.SetLabel("vStarboard");
            vStarboard.SetShow(true);

            if (vTgtForward != null)
            {
                vTgtForward.SetShow(false);
            }
            c = UnityEngine.Color.blue;
            //c = new UnityEngine.Color(1.0f, 0.647f, 0f);
            vTgtForward = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1.5
            };
            //vTgtForward.SetLabel("vTgtForward");
            vTgtForward.SetShow(true);
            if (vTgtTop != null)
            {
                vTgtTop.SetShow(false);
            }
            vTgtTop = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1.5
            };
            //vTgtTop.SetLabel("vTgtTop");
            vTgtTop.SetShow(true);
            if (vTgtStarboard != null)
            {
                vTgtStarboard.SetShow(false);
            }
            vTgtStarboard = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1.5
            };
            //vTgtStarboard.SetLabel("vTgtStarboard");
            vTgtStarboard.SetShow(true);

            if (vWorldX != null)
            {
                vWorldX.SetShow(false);
            }
            c = UnityEngine.Color.white;
            vWorldX = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(50, 0, 0),
                Width = 0.25
            };
            //vWorldX.SetLabel("vWorldX");
            vWorldX.SetShow(true);
            if (vWorldY != null)
            {
                vWorldY.SetShow(false);
            }
            vWorldY = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 50, 0),
                Width = 0.25
            };
            //vWorldY.SetLabel("vWorldY");
            vWorldY.SetShow(true);
            if (vWorldZ != null)
            {
                vWorldZ.SetShow(false);
            }
            vWorldZ = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 50),
                Width = 0.25
            };
            //vWorldZ.SetLabel("vWorldZ");
            vWorldZ.SetShow(true);

            if (vOmegaX != null)
            {
                vOmegaX.SetShow(false);
            }
            c = UnityEngine.Color.cyan;
            vOmegaX = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1
            };
            //vOmegaX.SetLabel("vOmegaX");
            vOmegaX.SetShow(true);
            if (vOmegaY != null)
            {
                vOmegaY.SetShow(false);
            }
            vOmegaY = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1
            };
            //vOmegaY.SetLabel("vOmegaY");
            vOmegaY.SetShow(true);
            if (vOmegaZ != null)
            {
                vOmegaZ.SetShow(false);
            }
            vOmegaZ = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1
            };
            //vOmegaZ.SetLabel("vOmegaZ");
            vOmegaZ.SetShow(true);

            if (vTgtTorqueX != null)
            {
                vTgtTorqueX.SetShow(false);
            }
            c = UnityEngine.Color.green;
            vTgtTorqueX = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1.5
            };
            //vTgtTorqueX.SetLabel("vTgtTorqueX");
            vTgtTorqueX.SetShow(true);
            if (vTgtTorqueY != null)
            {
                vTgtTorqueY.SetShow(false);
            }
            vTgtTorqueY = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1.5
            };
            //vTgtTorqueY.SetLabel("vTgtTorqueY");
            vTgtTorqueY.SetShow(true);
            if (vTgtTorqueZ != null)
            {
                vTgtTorqueZ.SetShow(false);
            }
            vTgtTorqueZ = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1.5
            };
            //vTgtTorqueZ.SetLabel("vTgtTorqueZ");
            vTgtTorqueZ.SetShow(true);

            foreach (string key in vEngines.Keys.ToArray())
            {
                if (vEngines[key] != null) vEngines[key].SetShow(false);
                vEngines.Remove(key);
            }
            foreach (string key in vRcs.Keys.ToArray())
            {
                if (vRcs[key] != null) vRcs[key].SetShow(false);
                vRcs.Remove(key);
            }
        }

        public void HideVectorsRenderers()
        {
            if (vForward != null)
            {
                vForward.SetShow(false);
                vTop.SetShow(false);
                vStarboard.SetShow(false);

                vTgtForward.SetShow(false);
                vTgtTop.SetShow(false);
                vTgtStarboard.SetShow(false);

                vTgtTorqueX.SetShow(false);
                vTgtTorqueY.SetShow(false);
                vTgtTorqueZ.SetShow(false);

                vWorldX.SetShow(false);
                vWorldY.SetShow(false);
                vWorldZ.SetShow(false);

                vOmegaX.SetShow(false);
                vOmegaY.SetShow(false);
                vOmegaZ.SetShow(false);
            }

            foreach (string key in vEngines.Keys.ToArray())
            {
                if (vEngines[key] != null) vEngines[key].SetShow(false);
                vEngines.Remove(key);
            }
            foreach (string key in vRcs.Keys.ToArray())
            {
                if (vRcs[key] != null) vRcs[key].SetShow(false);
                vRcs.Remove(key);
            }
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

        private void update(FlightCtrlState c)
        {
            if (Value != null)
            {
                UpdateStateVectors();
                UpdateTorque();
                //UpdatePrediction();
                UpdatePredictionPI();
                UpdateControl(c);
                if (ShowSteeringStats) PrintDebug();
                if (WriteCSVFiles) WriteCSVs();
                UpdateVectorRenders();
            }
        }

        private Direction GetDirectionFromValue()
        {
            if (Value is Direction)
                return (Direction)Value;
            else if (Value is Vector)
                return Direction.LookRotation((Vector)Value, vesselUp);
            else if (Value is string)
            {
                if (string.Equals((string)Value, "kill", StringComparison.CurrentCultureIgnoreCase))
                {
                    return new Direction(vesselRotation);
                }
            }
            //return new Direction(new Vector3d(0, 0, 0), false);
            return null;
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
            momentOfInertia = shared.Vessel.findLocalMOI(centerOfMass);

            // Adjust MOI if it's values are too low
            if (momentOfInertia.x < 0.1)
                momentOfInertia.x = 0.1;
            if (momentOfInertia.y < 0.1)
                momentOfInertia.y = 0.1;
            if (momentOfInertia.z < 0.1)
                momentOfInertia.z = 0.1;

            if (momentOfInertia.x < 4.5)
                momentOfInertia.x *= 10d;
            if (momentOfInertia.y < 4.5)
                momentOfInertia.y *= 10d;
            if (momentOfInertia.z < 4.5)
                momentOfInertia.z *= 10d;

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

            // omega is angular rotation.  need to correct the signs to agree with the facing axis
            omega = Quaternion.Inverse(vesselRotation) * shared.Vessel.rigidbody.angularVelocity;
            omega.x *= -1;
            //omega.y *= -1;
            omega.z *= -1;
        }

        public void UpdateTorque()
        {
            // staticTorque will represent engine torque due to imbalanced placement
            staticTorque = new Vector3d(0, 0, 0);
            // controlTorque is the maximum amount of torque applied by setting a control to 1.0.
            controlTorque = new Vector3d(0, 0, 0);
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
                            controlTorque.x += wheel.PitchTorque;
                            controlTorque.z += wheel.YawTorque;
                            controlTorque.y += wheel.RollTorque;
                        }
                        continue;
                    }
                    ModuleRCS rcs = pm as ModuleRCS;
                    if (rcs != null)
                    {
                        if (shared.Vessel.ActionGroups[KSPActionGroup.RCS] && rcs.rcsEnabled && !rcs.part.ShieldedFromAirstream)
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
                                controlTorque.x += Math.Max(Vector3d.Dot(torque, vesselStarboard), 0);
                                controlTorque.z += Math.Max(Vector3d.Dot(torque, vesselTop), 0);
                                controlTorque.y += Math.Max(Vector3d.Dot(torque, vesselForward), 0);
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

            controlTorque.x += controlEngineTorque.x;
            controlTorque.z += controlEngineTorque.z;
            controlTorque.y += controlEngineTorque.y;

            //if (momentOfInertia.x < 4.5) controlTorque.x /= 10d;
            //if (momentOfInertia.z < 4.5) controlTorque.z /= 10d;
            //if (momentOfInertia.y < 4.5) controlTorque.y /= 10d;

            double minTorque = 0.0001;
            if (controlTorque.x < minTorque) controlTorque.x = minTorque;
            if (controlTorque.y < minTorque) controlTorque.y = minTorque;
            if (controlTorque.z < minTorque) controlTorque.z = minTorque;
        }

        // Keeping the old UpdatePrediction method for reference while implementing PI based control, can be deleted later.
        public void UpdatePrediction()
        {
            // calculate phi and pitch, yaw, roll components of phi (angular error)
            phi = Vector3d.Angle(vesselForward, targetForward) * Math.PI / 180d;
            if (Vector3d.Angle(vesselTop, targetForward) > 90)
                phi *= -1;
            phiPitch = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselStarboard, targetForward)) * Math.PI / 180d;
            if (Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselStarboard, targetForward)) > 90)
                phiPitch *= -1;
            phiYaw = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselTop, targetForward)) * Math.PI / 180d;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselTop, targetForward)) > 90)
                phiYaw *= -1;
            phiRoll = Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselForward, targetTop)) * Math.PI / 180d;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselForward, targetTop)) > 90)
                phiRoll *= -1;

            // dt1 is the dt factor for the angular velocity calculation
            // dt2 is the dt factor for the torque calculation
            double dt1 = 0.5d;
            double dt2 = TimeWarp.fixedDeltaTime * 8;
            double dtRoll1 = 0.5d;
            double dtRoll2 = TimeWarp.fixedDeltaTime * 2;

            // Calculate the target angular velocity based on the error (phi)
            tgtPitchOmega = phiPitch / dt1;
            tgtYawOmega = phiYaw / dt1;
            //double tgtYawOmega = Math.Pow(Math.Sin(phiYaw), 2) * 10d;
            tgtRollOmega = phiRoll / dtRoll1;

            // kill roll rotation until within 45 degrees of the target direction
            if (Math.Abs(phi) > 45 * Math.PI / 180d)
                tgtRollOmega = 0;

            // Calculate the maximum allowable angular velocity and apply the limit, something we can stop in a reasonable amount of time
            maxPitchOmega = controlTorque.x * 0.5d / momentOfInertia.x;
            if (Math.Abs(tgtPitchOmega) > maxPitchOmega)
                tgtPitchOmega = maxPitchOmega * Math.Sign(tgtPitchOmega);
            maxYawOmega = controlTorque.z * 0.5d / momentOfInertia.z;
            if (Math.Abs(tgtYawOmega) > maxYawOmega)
                tgtYawOmega = maxYawOmega * Math.Sign(tgtYawOmega);
            maxRollOmega = controlTorque.y * 0.5d / momentOfInertia.y;
            //maxRollOmega = Math.PI / 10;
            if (Math.Abs(tgtRollOmega) > maxRollOmega)
                tgtRollOmega = maxRollOmega * Math.Sign(tgtRollOmega);

            // Calculate the desired torque to match to target
            tgtPitchTorque = momentOfInertia.x * (tgtPitchOmega - omega.x) / dt2;
            tgtYawTorque = momentOfInertia.z * (tgtYawOmega - omega.y) / dt2;
            tgtRollTorque = momentOfInertia.y * (tgtRollOmega - omega.z) / dtRoll2;
        }

        // Update prediction based on PI controls, sets the target angular velocity and the target torque for the vessel
        public void UpdatePredictionPI()
        {
            // calculate phi and pitch, yaw, roll components of phi (angular error)
            phi = Vector3d.Angle(vesselForward, targetForward) * Math.PI / 180d;
            if (Vector3d.Angle(vesselTop, targetForward) > 90)
                phi *= -1;
            phiPitch = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselStarboard, targetForward)) * Math.PI / 180d;
            if (Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselStarboard, targetForward)) > 90)
                phiPitch *= -1;
            phiYaw = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselTop, targetForward)) * Math.PI / 180d;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselTop, targetForward)) > 90)
                phiYaw *= -1;
            phiRoll = Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselForward, targetTop)) * Math.PI / 180d;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselForward, targetTop)) > 90)
                phiRoll *= -1;

            // Calculate the maximum allowable angular velocity and apply the limit, something we can stop in a reasonable amount of time
            maxPitchOmega = controlTorque.x * 1d / momentOfInertia.x;
            maxYawOmega = controlTorque.z * 1d / momentOfInertia.z;
            maxRollOmega = controlTorque.y * 1d / momentOfInertia.y;

            double sampletime = shared.UpdateHandler.CurrentFixedTime;
            tgtPitchOmega = Math.Max(Math.Min(pitchRatePI.Update(sampletime, -phiPitch, 0, maxPitchOmega), maxPitchOmega), -maxPitchOmega);
            tgtYawOmega = Math.Max(Math.Min(yawRatePI.Update(sampletime, -phiYaw, 0, maxYawOmega), maxYawOmega), -maxYawOmega);
            if (Math.Abs(phi) > 5 * Math.PI / 180d)
            {
                tgtRollOmega = 0;
                rollRatePI.ResetI();
            }
            else
            {
                tgtRollOmega = Math.Max(Math.Min(rollRatePI.Update(sampletime, -phiRoll, 0, maxRollOmega), maxRollOmega), -maxRollOmega);
            }

            // Calculate target torque based on PID
            tgtPitchTorque = pitchPI.Update(shared.UpdateHandler.CurrentFixedTime, omega.x, tgtPitchOmega, momentOfInertia.x, controlTorque.x);
            tgtYawTorque = yawPI.Update(shared.UpdateHandler.CurrentFixedTime, omega.y, tgtYawOmega, momentOfInertia.z, controlTorque.z);
            tgtRollTorque = rollPI.Update(shared.UpdateHandler.CurrentFixedTime, omega.z, tgtRollOmega, momentOfInertia.y, controlTorque.y);

            tgtPitchTorque = pitchPI.Update(shared.UpdateHandler.CurrentFixedTime, pitchRate.Update(omega.x), tgtPitchOmega, momentOfInertia.x, controlTorque.x);
            tgtYawTorque = yawPI.Update(shared.UpdateHandler.CurrentFixedTime, yawRate.Update(omega.y), tgtYawOmega, momentOfInertia.z, controlTorque.z);
            tgtRollTorque = rollPI.Update(shared.UpdateHandler.CurrentFixedTime, rollRate.Update(omega.z), tgtRollOmega, momentOfInertia.y, controlTorque.y);
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
                double epsilon = 0.0001;

                //TODO: include adjustment for static torque (due to engines)

                accPitch = Math.Min(Math.Max(tgtPitchTorque / controlTorque.x, -1), 1);
                if (Math.Abs(accPitch) < epsilon)
                    accPitch = 0;
                c.pitch = (float)accPitch;
                accYaw = Math.Min(Math.Max(tgtYawTorque / controlTorque.z, -1), 1);
                if (Math.Abs(accYaw) < epsilon)
                    accYaw = 0;
                c.yaw = (float)accYaw;
                accRoll = Math.Min(Math.Max(tgtRollTorque / controlTorque.y, -1), 1);
                if (Math.Abs(accRoll) < epsilon)
                    accRoll = 0;
                c.roll = (float)accRoll;
            }
        }

        public void UpdateVectorRenders()
        {
            if (ShowFacingVectors)
            {
                vForward.Vector = vesselForward * renderMultiplier;
                vTop.Vector = vesselTop * renderMultiplier;
                vStarboard.Vector = vesselStarboard * renderMultiplier;

                vTgtForward.Vector = targetForward * renderMultiplier * 0.75f;
                vTgtTop.Vector = targetTop * renderMultiplier * 0.75f;
                vTgtStarboard.Vector = targetStarboard * renderMultiplier * 0.75f;

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
                if (vForward.GetShow()) vForward.SetShow(false);
                if (vTop.GetShow()) vTop.SetShow(false);
                if (vStarboard.GetShow()) vStarboard.SetShow(false);

                if (vTgtForward.GetShow()) vTgtForward.SetShow(false);
                if (vTgtTop.GetShow()) vTgtTop.SetShow(false);
                if (vTgtStarboard.GetShow()) vTgtStarboard.SetShow(false);

                if (vWorldX.GetShow()) vWorldX.SetShow(false);
                if (vWorldY.GetShow()) vWorldY.SetShow(false);
                if (vWorldZ.GetShow()) vWorldZ.SetShow(false);
            }

            if (ShowAngularVectors)
            {
                vOmegaX.Vector = vesselTop * omega.x * renderMultiplier * 100f;
                vOmegaX.Start = vesselForward * renderMultiplier;
                vOmegaY.Vector = vesselStarboard * omega.y * renderMultiplier * 100f;
                vOmegaY.Start = vesselForward * renderMultiplier;
                vOmegaZ.Vector = vesselStarboard * omega.z * renderMultiplier * 100f;
                vOmegaZ.Start = vesselTop * renderMultiplier;

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
                if (vOmegaX.GetShow()) vOmegaX.SetShow(false);
                if (vOmegaY.GetShow()) vOmegaY.SetShow(false);
                if (vOmegaZ.GetShow()) vOmegaZ.SetShow(false);

                if (vTgtTorqueX.GetShow()) vTgtTorqueX.SetShow(false);
                if (vTgtTorqueY.GetShow()) vTgtTorqueY.SetShow(false);
                if (vTgtTorqueZ.GetShow()) vTgtTorqueZ.SetShow(false);
            }

            if (ShowThrustVectors)
            {
                foreach (var tv in allEngineVectors)
                {
                    Vector3d[] vectors = tv.GetTorque(vesselForward, vesselTop, vesselStarboard);
                    string key = tv.PartId;
                    if (!vEngines.ContainsKey(key))
                    {
                        Color c = UnityEngine.Color.yellow;
                        var vecdraw = new VectorRenderer(shared.UpdateHandler, shared)
                        {
                            Color = new RgbaColor(c.r, c.g, c.b),
                            Start = new Vector3d(0, 0, 0),
                            Vector = new Vector3d(0, 0, 0),
                            Width = 0.25
                        };
                        //vecdraw.SetLabel(key);
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = tv.Thrust;
                    vEngines[key].Start = tv.Position;

                    key = tv.PartId + "gimbaled";
                    if (!vEngines.ContainsKey(key))
                    {
                        Color c = UnityEngine.Color.magenta;
                        var vecdraw = new VectorRenderer(shared.UpdateHandler, shared)
                        {
                            Color = new RgbaColor(c.r, c.g, c.b),
                            Start = new Vector3d(0, 0, 0),
                            Vector = new Vector3d(0, 0, 0),
                            Width = 0.25
                        };
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = tv.GetGimbaledThrust(vesselForward, vesselTop, vesselStarboard);
                    vEngines[key].Start = tv.Position;

                    key = tv.PartId + "torque";
                    if (!vEngines.ContainsKey(key))
                    {
                        Color c = UnityEngine.Color.red;
                        var vecdraw = new VectorRenderer(shared.UpdateHandler, shared)
                        {
                            Color = new RgbaColor(c.r, c.g, c.b),
                            Start = new Vector3d(0, 0, 0),
                            Vector = new Vector3d(0, 0, 0),
                            Width = 0.25
                        };
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = vectors[0];
                    vEngines[key].Start = tv.Position;

                    key = tv.PartId + "control";
                    if (!vEngines.ContainsKey(key))
                    {
                        Color c = UnityEngine.Color.blue;
                        var vecdraw = new VectorRenderer(shared.UpdateHandler, shared)
                        {
                            Color = new RgbaColor(c.r, c.g, c.b),
                            Start = new Vector3d(0, 0, 0),
                            Vector = new Vector3d(0, 0, 0),
                            Width = 0.25
                        };
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = vectors[1];
                    vEngines[key].Start = tv.Position;

                    key = tv.PartId + "position";
                    if (!vEngines.ContainsKey(key))
                    {
                        Color c = UnityEngine.Color.cyan;
                        var vecdraw = new VectorRenderer(shared.UpdateHandler, shared)
                        {
                            Color = new RgbaColor(c.r, c.g, c.b),
                            Start = new Vector3d(0, 0, 0),
                            Vector = new Vector3d(0, 0, 0),
                            Width = 0.25
                        };
                        vEngines.Add(key, vecdraw);
                        vEngines[key].SetShow(true);
                    }
                    vEngines[key].Vector = tv.Position;
                }
            }
            else
            {
                foreach (VectorRenderer vr in vEngines.Values)
                {
                    if (vr.GetShow()) vr.SetShow(false);
                }
            }

            if (ShowRCSVectors)
            {
                foreach (var force in rcsVectors)
                {
                    string key = force.ID;
                    if (!vRcs.ContainsKey(key))
                    {
                        Color c = UnityEngine.Color.magenta;
                        var vecdraw = new VectorRenderer(shared.UpdateHandler, shared)
                        {
                            Color = new RgbaColor(c.r, c.g, c.b),
                            Start = new Vector3d(0, 0, 0),
                            Vector = new Vector3d(0, 0, 0),
                            Width = 0.25
                        };
                        vecdraw.SetShow(true);
                        vRcs.Add(key, vecdraw);
                    }
                    vRcs[key].Vector = force.Force;
                    vRcs[key].Start = force.Position;

                    key = force.ID + "torque";
                    if (!vRcs.ContainsKey(key))
                    {
                        Color c = UnityEngine.Color.yellow;
                        var vecdraw = new VectorRenderer(shared.UpdateHandler, shared)
                        {
                            Color = new RgbaColor(c.r, c.g, c.b),
                            Start = new Vector3d(0, 0, 0),
                            Vector = new Vector3d(0, 0, 0),
                            Width = 0.1
                        };
                        vecdraw.SetShow(true);
                        vRcs.Add(key, vecdraw);
                    }
                    vRcs[key].Vector = force.Torque;
                    vRcs[key].Start = force.Position;
                }
            }
            else
            {
                foreach (VectorRenderer vr in vRcs.Values)
                {
                    if (vr.GetShow()) vr.SetShow(false);
                }
            }
        }

        public void PrintDebug()
        {
            shared.Screen.ClearScreen();
            shared.Screen.Print(string.Format("phi: {0}", phi * 180d / Math.PI));
            shared.Screen.Print(string.Format("phiRoll: {0}", phiRoll * 180d / Math.PI));
            shared.Screen.Print("    Pitch Values:");
            shared.Screen.Print(string.Format("phiPitch: {0}", phiPitch * 180d / Math.PI));
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
            shared.Screen.Print(string.Format("phiYaw: {0}", phiYaw * 180d / Math.PI));
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
            shared.Screen.Print(string.Format("phiRoll: {0}", phiRoll * 180d / Math.PI));
            //shared.Screen.Print(string.Format("phiRoll: {0}", deltaRotation.eulerAngles.z));
            shared.Screen.Print(string.Format("I roll: {0}", momentOfInertia.y));
            shared.Screen.Print(string.Format("torque roll: {0}", controlTorque.y));
            shared.Screen.Print(string.Format("torque gimbal: {0}", controlEngineTorque.y));
            shared.Screen.Print(string.Format("maxRollOmega: {0}", maxRollOmega));
            shared.Screen.Print(string.Format("tgtRollOmega: {0}", tgtRollOmega));
            shared.Screen.Print(string.Format("rollOmega: {0}", omega.z));
            shared.Screen.Print(string.Format("tgtRollTorque: {0}", tgtRollTorque));
            shared.Screen.Print(string.Format("accRoll: {0}", accRoll));
            shared.Screen.Print("    Dictionary Counts:");
            shared.Screen.Print(string.Format("vRCS count: {0}", vRcs.Count));
            shared.Screen.Print(string.Format("vEngines count: {0}", vEngines.Count));
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

            pitchRateWriter.WriteLine(pitchRatePI.ToCSVString());
            yawRateWriter.WriteLine(yawRatePI.ToCSVString());
            rollRateWriter.WriteLine(rollRatePI.ToCSVString());

            pitchTorqueWriter.WriteLine(pitchPI.ToCSVString());
            yawTorqueWriter.WriteLine(yawPI.ToCSVString());
            rollTorqueWriter.WriteLine(rollPI.ToCSVString());
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
                this.Dispose();
            }
        }

        public void Dispose()
        {
            HideVectorsRenderers();
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
            if (vForward != null)
            {
                vTgtTorqueZ.Dispose();
                vTop = null;
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

            if (pitchRateWriter != null) pitchRateWriter.Dispose();
            if (yawRateWriter != null) yawRateWriter.Dispose();
            if (rollRateWriter != null) rollRateWriter.Dispose();

            if (pitchTorqueWriter != null) pitchTorqueWriter.Dispose();
            if (yawTorqueWriter != null) yawTorqueWriter.Dispose();
            if (rollTorqueWriter != null) rollTorqueWriter.Dispose();

            pitchRateWriter = null;
            yawRateWriter = null;
            rollRateWriter = null;

            pitchTorqueWriter = null;
            yawTorqueWriter = null;
            rollTorqueWriter = null;

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
                    ret[0] = Vector3d.Cross(thrust, Position);
                    if (GimbalRange > 0.0001)
                    {
                        Vector3d pitchAxis = Vector3d.Exclude(neut, starboard);
                        Vector3d yawAxis = Vector3d.Exclude(neut, top);
                        Vector3d rollAxis = Vector3d.Exclude(forward, Position);
                        Quaternion rot = Quaternion.AngleAxis(GimbalRange, pitchAxis);
                        thrust = rot * neut;
                        ret[1] = Vector3d.Cross(thrust, Position);
                        rot = Quaternion.AngleAxis(GimbalRange, yawAxis);
                        thrust = rot * neut;
                        ret[2] = Vector3d.Cross(thrust, Position);
                        //rot = Rotation * Quaternion.AngleAxis(GimbalRange, forward);
                        double angle = Vector3d.Angle(forward, Position);
                        if (angle > 179 || angle < 1)
                        {
                            ret[3] = ret[0];
                        }
                        else
                        {
                            rot = Quaternion.AngleAxis(GimbalRange, rollAxis);
                            thrust = rot * neut;
                            ret[3] = Vector3d.Cross(thrust, Position);
                        }
                    }
                    else
                    {
                        ret[1] = Vector3d.zero;
                        ret[2] = Vector3d.zero;
                        ret[3] = Vector3d.zero;
                        //ret[1] = ret[0];
                        //ret[2] = ret[0];
                        //ret[3] = ret[0];
                    }
                }
                else
                {
                    ret[0] = Vector3d.zero;
                    ret[1] = Vector3d.zero;
                    ret[2] = Vector3d.zero;
                    ret[3] = Vector3d.zero;
                }
                return ret;
            }

            public Quaternion GetAngledRotation(Vector3d forward, Vector3d top, Vector3d starboard)
            {
                return Rotation * Quaternion.AngleAxis(GimbalRange, starboard);
            }

            public Vector3d GetGimbaledThrust(Vector3d forward, Vector3d top, Vector3d starboard)
            {
                Vector3d thrust = Thrust;
                double angle = Vector3d.Angle(forward, Position);
                if (angle > 179 || angle < 1)
                {
                    return thrust;
                }
                //Vector3d axis = Vector3d.Exclude(thrust, starboard);
                //Vector3d axis = Vector3d.Exclude(thrust, top);
                //Vector3d axis = Vector3d.Exclude(thrust, forward);
                Vector3d axis = Vector3d.Exclude(forward, Position);
                return Quaternion.AngleAxis(GimbalRange, axis) * thrust;
                //return Rotation * thrust;
            }
        }

        public class TorquePI
        {
            private PIDLoop Loop { get; set; }

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

            public int SampleLimit { get; set; }

            public MovingAverage()
            {
                Values = new List<double>();
                SampleLimit = 30;
            }

            public double Update(double value)
            {
                if (!(double.IsInfinity(value) || double.IsNaN(value)))
                {
                    Values.Add(value);
                    while (Values.Count >= SampleLimit)
                    {
                        Values.RemoveAt(0);
                    }
                    return Values.Average();
                }
                return value;
            }
        }
    }
}