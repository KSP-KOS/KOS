using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using kOS.Safe.Utilities;
using UnityEngine;
using kOS.Suffixed;
using Math = System.Math;

namespace kOS.Binding
{
    public class SteeringManager
    {
        SharedObjects shared;
        Vessel vessel;

        private bool enabled = false;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled != value)
                {
                    if (!enabled && csvFile == null)
                    {
                        if (csvFile == null)
                        {
                            //csvFile = File.AppendText("torquePI.csv");
                            csvFile = File.CreateText("torquePI.csv");
                            csvFile.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,Tr,Ts,I,MaxOutput");
                        }
                        if (rateCSVFile == null)
                        {
                            //rateCSVFile = File.AppendText("ratePI.csv");
                            rateCSVFile = File.CreateText("ratePI.csv");
                            rateCSVFile.WriteLine("LastSampleTime,Error,ErrorSum,Output,Kp,Ki,MaxOutput");
                        }
                    }
                }
                enabled = value;
                if (enabled)
                {
                    InitVectorRenderers();
                    pitchPI.ResetI();
                    yawPI.ResetI();
                    rollPI.ResetI();
                    pitchRatePI.ResetI();
                    yawRatePI.ResetI();
                    rollRatePI.ResetI();
                }
                else
                {
                    HideVectorsRenderers();
                    if (csvFile != null)
                    {
                        csvFile.Flush();
                        csvFile.Close();
                        csvFile.Dispose();
                        csvFile = null;
                    }
                    if (rateCSVFile != null)
                    {
                        rateCSVFile.Flush();
                        rateCSVFile.Close();
                        rateCSVFile.Dispose();
                        rateCSVFile = null;
                    }
                }
            }
        }
        public object Value { get; set; }
        public Direction TargetDirection { get { return this.GetDirectionFromValue(); } }

        Transform vesselTransform;

        TorquePI pitchPI = new TorquePI();
        TorquePI yawPI = new TorquePI();
        TorquePI rollPI = new TorquePI();

        RatePI pitchRatePI = new RatePI();
        RatePI yawRatePI = new RatePI();
        RatePI rollRatePI = new RatePI();


        static StreamWriter rateCSVFile;
        static StreamWriter csvFile;

        List<ThrustVector> allEngineVectors = new List<ThrustVector>();

        #region doubles
        double accPitch = 0;
        double accYaw = 0;
        double accRoll = 0;

        double phi;
        double phiPitch;
        double phiYaw;
        double phiRoll;

        double maxPitchOmega;
        double maxYawOmega;
        double maxRollOmega;

        double tgtPitchOmega;
        double tgtYawOmega;
        double tgtRollOmega;

        double tgtPitchTorque;
        double tgtYawTorque;
        double tgtRollTorque;

        double renderMultiplier = 50;
        #endregion doubles

        Quaternion vesselRotation;
        Quaternion targetRot;

        #region Vectors
        Vector3d centerOfMass;
        Vector3d vesselUp;

        Vector3d vesselForward;
        Vector3d vesselTop;
        Vector3d vesselStarboard;

        Vector3d targetForward;
        Vector3d targetTop;
        Vector3d targetStarboard;

        Vector3d omega;
        Vector3d momentOfInertia;
        Vector3d staticTorque = Vector3d.zero;
        Vector3d controlTorque = Vector3d.zero;
        Vector3d staticEngineTorque = Vector3d.zero;
        Vector3d controlEngineTorque = Vector3d.zero;
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

        Dictionary<string, VectorRenderer> vEngines = new Dictionary<string, VectorRenderer>();
        Dictionary<string, VectorRenderer> vRcs = new Dictionary<string, VectorRenderer>();
        #endregion VectorRenders

        public SteeringManager()
        {
        }
        public SteeringManager(SharedObjects sharedObj)
        {
            shared = sharedObj;
            vessel = shared.Vessel;
            if (csvFile != null)
            {
                csvFile.Flush();
                csvFile.Close();
                csvFile.Dispose();
                csvFile = null;
            }
            if (rateCSVFile != null)
            {
                rateCSVFile.Flush();
                rateCSVFile.Close();
                rateCSVFile.Dispose();
                rateCSVFile = null;
            }
        }

        public void InitVectorRenderers()
        {
            if (vForward != null)
            {
                vForward.SetShow(false);
            }
            Color c = UnityEngine.Color.red;
            vForward = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(0, 0, 0),
                Width = 1
            };
            vForward.SetLabel("vForward");
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
            vTop.SetLabel("vTop");
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
            vStarboard.SetLabel("vStarboard");
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
            vTgtForward.SetLabel("vTgtForward");
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
            vTgtTop.SetLabel("vTgtTop");
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
            vTgtStarboard.SetLabel("vTgtStarboard");
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
            vWorldX.SetLabel("vWorldX");
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
            vWorldY.SetLabel("vWorldY");
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
            vWorldZ.SetLabel("vWorldZ");
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
            vOmegaX.SetLabel("vOmegaX");
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
            vOmegaY.SetLabel("vOmegaY");
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
            vOmegaZ.SetLabel("vOmegaZ");
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
            vTgtTorqueX.SetLabel("vTgtTorqueX");
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
            vTgtTorqueY.SetLabel("vTgtTorqueY");
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
            vTgtTorqueZ.SetLabel("vTgtTorqueZ");
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

        public void Update(Vessel vsl)
        {
            if (vessel != vsl) vessel = vsl;
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
            UpdateStateVectors();
            UpdateTorque();
            //UpdatePrediction();
            UpdatePredictionPI();
            UpdateControl(c);
            PrintDebug();
            UpdateVectorRenders();
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
            return new Direction(new Vector3d(0, 0, 0), false);
        }

        public void UpdateStateVectors()
        {
            targetRot = GetDirectionFromValue().Rotation;
            centerOfMass = vessel.findWorldCenterOfMass();
            momentOfInertia = vessel.findLocalMOI(centerOfMass);
            vesselUp = (centerOfMass - vessel.mainBody.position).normalized;

            vesselTransform = vessel.ReferenceTransform;
            // Found that the default rotation has top pointing forward, forward pointing down, and right pointing starboard.
            // This fixes that rotation.
            vesselRotation = vesselTransform.rotation * Quaternion.Euler(-90, 0, 0);

            vesselForward = vesselRotation * Vector3d.forward;
            vesselTop = vesselRotation * Vector3d.up;
            vesselStarboard = vesselRotation * Vector3d.right;

            targetForward = targetRot * Vector3d.forward;
            targetTop = targetRot * Vector3d.up;
            targetStarboard = targetRot * Vector3d.right;

            // omega is angular rotation.  need to correct the signs to agree with the facing axis
            omega = Quaternion.Inverse(vesselRotation) * vessel.rigidbody.angularVelocity;
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
            allEngineVectors.Clear();
            foreach (Part part in vessel.Parts)
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
                        if (wheel.isActiveAndEnabled  && wheel.State == ModuleReactionWheel.WheelState.Active)
                        {
                            controlTorque.x += wheel.PitchTorque;
                            controlTorque.z += wheel.YawTorque;
                            controlTorque.y += wheel.RollTorque;
                        }
                        continue;
                    }
                    ModuleRCS rcs = pm as ModuleRCS;
                    if (rcs != null)
                    {
                        if (vessel.ActionGroups[KSPActionGroup.RCS] && rcs.rcsEnabled)
                        {
                            for (int i = 0; i < rcs.thrusterTransforms.Count; i++)
                            {
                                Transform thrustdir = rcs.thrusterTransforms[i];
                                string key = part.flightID.ToString() + thrustdir.name + i.ToString();
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
                                    vecdraw.SetLabel(key);
                                    vecdraw.SetShow(true);
                                    vRcs.Add(key, vecdraw);
                                }
                                vRcs[key].Vector = thrustdir.forward * rcs.thrusterPower;
                                vRcs[key].Start = relCom;
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

            // Adjust MOI if it's values are too low
            //if (momentOfInertia.x < 0.01)
            //    momentOfInertia.x = 0.01;
            if (momentOfInertia.y < 0.1)
                momentOfInertia.y = 0.1;
            //if (momentOfInertia.z < 0.01)
            //    momentOfInertia.z = 0.01;

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

            // Adjust MOI if it's values are too low
            //if (momentOfInertia.x < 0.01)
            //    momentOfInertia.x = 0.01;
            if (momentOfInertia.y < 0.1)
                momentOfInertia.y = 0.1;
            //if (momentOfInertia.z < 0.01)
            //    momentOfInertia.z = 0.01;

            // Calculate the maximum allowable angular velocity and apply the limit, something we can stop in a reasonable amount of time
            maxPitchOmega = controlTorque.x * 0.5d / momentOfInertia.x;
            maxYawOmega = controlTorque.z * 0.5d / momentOfInertia.z;
            maxRollOmega = controlTorque.y * 0.125d / momentOfInertia.y;

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
            tgtPitchTorque = pitchPI.Update(shared.UpdateHandler.CurrentFixedTime, tgtPitchOmega - omega.x, momentOfInertia.x, controlTorque.x);
            tgtYawTorque = yawPI.Update(shared.UpdateHandler.CurrentFixedTime, tgtYawOmega - omega.y, momentOfInertia.z, controlTorque.z);
            tgtRollTorque = rollPI.Update(shared.UpdateHandler.CurrentFixedTime, tgtRollOmega - omega.z, momentOfInertia.y, controlTorque.y);
        }

        public void UpdateControl(FlightCtrlState c)
        {
            if (vessel.ActionGroups[KSPActionGroup.SAS])
            {
                Quaternion target = TargetDirection.Rotation * Quaternion.Euler(90, 0, 0);
                if (Quaternion.Angle(vessel.Autopilot.SAS.lockedHeading, target) > 15)
                    vessel.Autopilot.SAS.LockHeading(target, true);
                else
                    vessel.Autopilot.SAS.lockedHeading = target;
                return;
            }
            else
            {
                double epsilon = 0.0001;

                //TODO: include adjustment for static torque (due to engines)

                //c.pitch = 0;
                accPitch = Math.Min(Math.Max(tgtPitchTorque / controlTorque.x, -1), 1);
                if (Math.Abs(accPitch) < epsilon)
                    accPitch = 0;
                c.pitch = (float)accPitch;
                //c.yaw = 0;
                accYaw = Math.Min(Math.Max(tgtYawTorque / controlTorque.z, -1), 1);
                if (Math.Abs(accYaw) < epsilon)
                    accYaw = 0;
                c.yaw = (float)accYaw;
                //c.roll = 0;
                accRoll = Math.Min(Math.Max(tgtRollTorque / controlTorque.y, -1), 1);
                if (Math.Abs(accRoll) < epsilon)
                    accRoll = 0;
                c.roll = (float)accRoll;
                //c.NeutralizeStick();
            }
        }

        public void UpdateVectorRenders()
        {
            vForward.Vector = vesselForward * renderMultiplier;
            vTop.Vector = vesselTop * renderMultiplier;
            vStarboard.Vector = vesselStarboard * renderMultiplier;

            vTgtForward.Vector = targetForward * renderMultiplier * 0.75f;
            vTgtTop.Vector = targetTop * renderMultiplier * 0.75f;
            vTgtStarboard.Vector = targetStarboard * renderMultiplier * 0.75f;


            //vWorldX.Vector = staticEngineTorque * renderMultiplier * 2;
            //vWorldY.Vector = controlEngineTorque * renderMultiplier * 2;
            vWorldX.Vector = new Vector3d(1, 0, 0) * renderMultiplier * 2;
            vWorldY.Vector = new Vector3d(0, 1, 0) * renderMultiplier * 2;
            vWorldZ.Vector = new Vector3d(0, 0, 1) * renderMultiplier * 2;

            vOmegaX.Vector = vesselTop * omega.x * renderMultiplier * 100f;
            vOmegaX.Start = vesselForward * renderMultiplier;
            vOmegaY.Vector = vesselStarboard * omega.y * renderMultiplier * 100f;
            vOmegaY.Start = vesselForward * renderMultiplier;
            vOmegaZ.Vector = vesselStarboard * omega.z * renderMultiplier * 100f;
            vOmegaZ.Start = vesselTop * renderMultiplier;

            vTgtTorqueX.Vector = vesselTop * tgtPitchOmega * renderMultiplier * 100f;
            vTgtTorqueX.Start = vesselForward * renderMultiplier;
            vTgtTorqueX.SetLabel("tgtPitchOmega: " + tgtPitchOmega);
            vTgtTorqueY.Vector = vesselStarboard * tgtRollOmega * renderMultiplier * 100f;
            vTgtTorqueY.Start = vesselTop * renderMultiplier;
            vTgtTorqueY.SetLabel("tgtRollOmega: " + tgtRollOmega);
            vTgtTorqueZ.Vector = vesselStarboard * tgtYawOmega * renderMultiplier * 100f;
            vTgtTorqueZ.Start = vesselForward * renderMultiplier;
            vTgtTorqueZ.SetLabel("tgtYawOmega: " + tgtYawOmega);

            vForward.SetShow(true);
            vTop.SetShow(true);
            vStarboard.SetShow(true);
            vTgtForward.SetShow(true);
            vTgtTop.SetShow(true);
            vTgtStarboard.SetShow(true);
            vWorldX.SetShow(true);
            vWorldY.SetShow(true);
            vWorldZ.SetShow(true);
            vOmegaX.SetShow(true);
            vOmegaY.SetShow(true);
            vOmegaZ.SetShow(true);
            vTgtTorqueX.SetShow(true);
            vTgtTorqueY.SetShow(true);
            vTgtTorqueZ.SetShow(true);

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
                    vecdraw.SetLabel(key);
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

        public void PrintDebug()
        {
            shared.Screen.ClearScreen();
            shared.Screen.Print(string.Format("phi: {0}", phi * 180d / Math.PI));
            shared.Screen.Print(string.Format("phiRoll: {0}", phiRoll * 180d / Math.PI));
            shared.Screen.Print("    Pitch Values:");
            shared.Screen.Print(string.Format("phiPitch: {0}", phiPitch * 180d / Math.PI));
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
            if (csvFile != null) csvFile.WriteLine(yawPI.ToCSVString());
            if (rateCSVFile != null) rateCSVFile.WriteLine(yawRatePI.ToCSVString());
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
                        ret[1] = ret[0];
                        ret[2] = ret[0];
                        ret[3] = ret[0];
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
            public double Kp { get; private set; }
            public double Ki { get; private set; }
            public double Error { get; private set; }
            public double Output { get; private set; }
            public double I { get; private set; }
            public double MaxOutput { get; private set; }
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
                set {
                    ts = value;
                    tr = 2.76 * ts / 4.0;
                }
            }
            public double ErrorSum { get; private set; }
            public double LastSampleTime { get; private set; }
            
            public TorquePI()
            {
                Ki = 1;
                Kp = 1;
                Ts = 0.5;
                Output = 0;
                Error = 0;
                ErrorSum = 0;
                LastSampleTime = double.MaxValue;
            }

            public double Update(double sampleTime, double error, double momentOfInertia, double maxOutput)
            {
                Error = error;
                Ki = momentOfInertia * Math.Pow(4.0 / ts, 2);
                Kp = 2 * Math.Pow(momentOfInertia * Ki, 0.5);
                if (LastSampleTime < sampleTime && Ki != 0)
                {
                    double dt = sampleTime - LastSampleTime;
                    ErrorSum += error * dt;
                }
                Output = error * Kp + ErrorSum * Ki;
                if (Math.Abs(Output) > maxOutput)
                {
                    Output = Math.Sign(Output) * maxOutput;
                    if (Ki != 0) ErrorSum = Output / Ki;
                }
                MaxOutput = maxOutput;
                LastSampleTime = sampleTime;
                I = momentOfInertia;
                return Output;
            }

            public void ResetI()
            {
                ErrorSum = 0;
                LastSampleTime = double.MaxValue;
            }

            public override string ToString()
            {
                return string.Format("TorquePI[Kp:{0}, Ki:{1}, Output:{2}, Error:{3}, ErrorSum:{4}, Tr:{5}, Ts:{6}",
                    Kp, Ki, Output, Error, ErrorSum, Tr, Ts);
            }

            public string ToCSVString()
            {
                return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                    LastSampleTime, Error, ErrorSum, Output, Kp, Ki, Tr, Ts, I, MaxOutput);
            }
        }

        public class RatePI
        {
            public double Kp { get; private set; }
            public double Ki { get; private set; }
            public double Input { get; private set; }
            public double Setpoint { get; private set; }
            public double Error { get; private set; }
            public double Output { get; private set; }
            public double MaxOutput { get; private set; }
            private double tr;
            public double Tr
            {
                get { return tr; }
                set
                {
                    tr = value;
                }
            }
            private double ts;
            public double Ts
            {
                get { return ts; }
                set
                {
                    ts = value;
                }
            }
            public double ErrorSum { get; private set; }
            public double LastSampleTime { get; private set; }

            public RatePI()
            {
                Ki = 1;
                Kp = 1;
                Ts = 0.5;
                Tr = 0;
                Output = 0;
                Error = 0;
                ErrorSum = 0;
                LastSampleTime = double.MaxValue;
            }

            public double Update(double sampleTime, double input, double setpoint, double maxOutput)
            {
                double error = setpoint - input;
                Input = input;
                Error = error;
                Ki = 0.25;
                Kp = 1.0;
                if (LastSampleTime < sampleTime  && Ki != 0)
                {
                    double dt = sampleTime - LastSampleTime;
                    ErrorSum += error * dt;
                }
                Output = error * Kp + ErrorSum * Ki;
                if (Math.Abs(Output) > maxOutput)
                {
                    Output = Math.Sign(Output) * maxOutput;
                    if (Ki != 0) ErrorSum = Output / Ki;
                }
                MaxOutput = maxOutput;
                LastSampleTime = sampleTime;
                return Output;
            }

            public void ResetI()
            {
                ErrorSum = 0;
                LastSampleTime = double.MaxValue;
            }

            public override string ToString()
            {
                return string.Format("RatePI[Kp:{0}, Ki:{1}, Output:{2}, Error:{3}, ErrorSum:{4}, Tr:{5}, Ts:{6}",
                    Kp, Ki, Output, Error, ErrorSum, Tr, Ts);
            }

            public string ToCSVString()
            {
                return string.Format("{0},{1},{2},{3},{4},{5},{6}",
                    LastSampleTime, Error, ErrorSum, Output, Kp, Ki, MaxOutput);
            }
        }
    }
}
