using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public bool Enabled { get; set; }
        public object Value { get; set; }
        public Direction TargetDirection { get { return this.GetDirectionFromValue(); } }

        Transform vesselTransform;

        #region doubles
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

        #region Quaternions
        Quaternion vesselRotation;
        Quaternion targetRot;
        #endregion Quaternions

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
                Vector = new Vector3d(1, 0, 0),
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
                Vector = new Vector3d(1, 0, 0),
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
                Vector = new Vector3d(1, 0, 0),
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
                Vector = new Vector3d(1, 0, 0),
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
                Vector = new Vector3d(1, 0, 0),
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
                Vector = new Vector3d(1, 0, 0),
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
                Vector = new Vector3d(renderMultiplier * 2, 0, 0),
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
                Vector = new Vector3d(0, renderMultiplier * 2, 0),
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
                Vector = new Vector3d(0, 0, renderMultiplier * 2),
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
                Vector = new Vector3d(50, 0, 0),
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
                Vector = new Vector3d(0, 50, 0),
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
                Vector = new Vector3d(0, 0, 50),
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

        public void HideVectorsDraws()
        {
            vForward.SetShow(false);
            vTop.SetShow(false);
            vStarboard.SetShow(false);
        }

        public void Update(Vessel vsl)
        {
            if (vessel != vsl) vessel = vsl;
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
            vessel = shared.Vessel;
            UpdateStateVectors();
            UpdateTorque();
            UpdatePrediction();
            UpdateControl(c);
            UpdateVectorRenders();
            PrintDebug();
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
            foreach (Part part in vessel.Parts)
            {
                relCom = part.Rigidbody.worldCenterOfMass - centerOfMass;
                foreach (PartModule pm in part.Modules)
                {
                    ModuleReactionWheel wheel = pm as ModuleReactionWheel;
                    if (wheel != null)
                    {
                        if (wheel.isActiveAndEnabled)
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
                        }
                        continue;
                    }
                    ModuleEngines engine = pm as ModuleEngines;
                    if (engine != null)
                    {
                        continue;
                    }
                }
            }
        }

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
            //double dt1 = 5d * Math.Pow(phi / Math.PI, 2) + 5d * phi / Math.PI + 0.25;
            //double dt1 = Math.Max(TimeWarp.fixedDeltaTime, 10d * Math.Abs(phi) / Math.PI);
            //double dt1 = Math.PI / 10d;
            double dt1 = 0.5d;
            //double dt2 = 1d - Math.Pow(phi / Math.PI, 2);
            double dt2 = TimeWarp.fixedDeltaTime;
            //double dtRoll1 = 5d * Math.Pow(phiRoll / Math.PI, 2) + 5d * phiRoll / Math.PI + 0.25;
            double dtRoll1 = 2d;
            //double dtRoll2 = 1d - Math.Pow(phiRoll / Math.PI, 2);
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
            maxRollOmega = Math.PI / 10;
            if (Math.Abs(tgtRollOmega) > maxRollOmega)
                tgtRollOmega = maxRollOmega * Math.Sign(tgtRollOmega);

            // Calculate the desired torque to match to target
            tgtPitchTorque = momentOfInertia.x * (tgtPitchOmega - omega.x) / dt2;
            tgtYawTorque = momentOfInertia.z * (tgtYawOmega - omega.y) / dt2;
            tgtRollTorque = momentOfInertia.y * (tgtRollOmega - omega.z) / dtRoll2;
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
                c.pitch = Mathf.Clamp((float)(tgtPitchTorque / controlTorque.x), -1, 1);
                if (Math.Abs(c.pitch) < epsilon)
                    c.pitch = 0;
                //c.yaw = 0;
                c.yaw = Mathf.Clamp((float)(tgtYawTorque / controlTorque.z), -1, 1);
                if (Math.Abs(c.yaw) < epsilon)
                    c.yaw = 0;
                //c.roll = 0;
                c.roll = Mathf.Clamp((float)(tgtRollTorque / controlTorque.y), -1, 1);
                if (Math.Abs(c.roll) < epsilon)
                    c.roll = 0;
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

            vWorldX.Vector = new Vector3d(1, 0, 0) * renderMultiplier * 2;
            vWorldY.Vector = new Vector3d(0, 1, 0) * renderMultiplier * 2;
            vWorldZ.Vector = new Vector3d(0, 0, 1) * renderMultiplier * 2;

            //vWorldX.Vector = vesselTransform.right * multiplier * 0.5f;
            //vWorldY.Vector = vesselTransform.up * multiplier * 0.5f;
            //vWorldZ.Vector = vesselTransform.forward * multiplier * 0.5f;

            //vOmegaX.Vector = vesselForward * angularVelocity.x * multiplier * 100f;
            //vOmegaY.Vector = vesselTop * angularVelocity.y * multiplier * 100f;
            //vOmegaZ.Vector = vesselStarboard * angularVelocity.z * multiplier * 100f;

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
            shared.Screen.Print(string.Format("maxPitchOmega: {0}", maxPitchOmega));
            shared.Screen.Print(string.Format("tgtPitchOmega: {0}", tgtPitchOmega));
            shared.Screen.Print(string.Format("pitchOmega: {0}", omega.x));
            shared.Screen.Print(string.Format("tgtPitchTorque: {0}", tgtPitchTorque));
            shared.Screen.Print("    Yaw Values:");
            shared.Screen.Print(string.Format("phiYaw: {0}", phiYaw * 180d / Math.PI));
            shared.Screen.Print(string.Format("I yaw: {0}", momentOfInertia.z));
            shared.Screen.Print(string.Format("torque yaw: {0}", controlTorque.z));
            shared.Screen.Print(string.Format("maxYawOmega: {0}", maxYawOmega));
            shared.Screen.Print(string.Format("tgtYawOmega: {0}", tgtYawOmega));
            shared.Screen.Print(string.Format("yawOmega: {0}", omega.y));
            shared.Screen.Print(string.Format("tgtYawTorque: {0}", tgtYawTorque));
            shared.Screen.Print("    Roll Values:");
            shared.Screen.Print(string.Format("phiRoll: {0}", phiRoll * 180d / Math.PI));
            shared.Screen.Print(string.Format("I roll: {0}", momentOfInertia.y));
            shared.Screen.Print(string.Format("torque roll: {0}", controlTorque.y));
            shared.Screen.Print(string.Format("maxRollOmega: {0}", maxRollOmega));
            shared.Screen.Print(string.Format("tgtRollOmega: {0}", tgtRollOmega));
            shared.Screen.Print(string.Format("rollOmega: {0}", omega.z));
            shared.Screen.Print(string.Format("tgtRollTorque: {0}", tgtRollTorque));
        }
    }
}
