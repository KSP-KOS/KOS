using kOS.Safe.Utilities;
using kOS.Suffixed;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Math = System.Math;

namespace kOS.Utilities
{
    public static class SteeringHelper
    {
        public static Vector3d PrevErr;
        public static Vector3d Integral;

        public static VectorRenderer vForward;
        public static VectorRenderer vTop;
        public static VectorRenderer vStarboard;

        public static VectorRenderer vTgtForward;
        public static VectorRenderer vTgtTop;
        public static VectorRenderer vTgtStarboard;

        public static VectorRenderer vWorldX;
        public static VectorRenderer vWorldY;
        public static VectorRenderer vWorldZ;

        public static VectorRenderer vOmegaX;
        public static VectorRenderer vOmegaY;
        public static VectorRenderer vOmegaZ;

        private static VectorRenderer vTgtTorqueX;
        private static VectorRenderer vTgtTorqueY;
        private static VectorRenderer vTgtTorqueZ;

        public static Dictionary<string, VectorRenderer> vEngines = new Dictionary<string, VectorRenderer>();
        public static Dictionary<string, VectorRenderer> vRcs = new Dictionary<string, VectorRenderer>();

        public static SharedObjects Shared;
        /*
        public static void InitVectorRenderers(SharedObjects shared)
        {
            Shared = shared;
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
            c = UnityEngine.Color.magenta;
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
                Width = 1.75
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
                Width = 1.75
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
                Width = 1.75
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
                Vector = new Vector3d(50, 0, 0),
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
                Vector = new Vector3d(0, 50, 0),
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
                Vector = new Vector3d(0, 0, 50),
                Width = 1
            };
            vOmegaZ.SetLabel("vOmegaZ");
            vOmegaZ.SetShow(true);

            if (vTgtTorqueX != null)
            {
                vTgtTorqueX.SetShow(false);
            }
            c = UnityEngine.Color.blue;
            vTgtTorqueX = new VectorRenderer(shared.UpdateHandler, shared)
            {
                Color = new RgbaColor(c.r, c.g, c.b),
                Start = new Vector3d(0, 0, 0),
                Vector = new Vector3d(50, 0, 0),
                Width = 1
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
                Width = 1
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
                Width = 1
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
        }*/

        public static void KillRotation(FlightCtrlState c, Vessel vessel)
        {
            //var act = vessel.transform.InverseTransformDirection(vessel.rigidbody.angularVelocity).normalized;

            //c.pitch = act.x;
            //c.roll = act.y;
            //c.yaw = act.z;

            SteerShipToward(VesselUtils.GetFacing(vessel), c, vessel);

            c.killRot = true;
        }

        public static void SteerShipToward(Direction targetDir, FlightCtrlState c, Vessel vessel)
        {
            if (vessel == null)
            {
                SafeHouse.Logger.LogError("SteerShipToward: Vessel is null!!");
                return;
            }
            float multiplier = 50;

            Vector3d centerOfMass = vessel.findWorldCenterOfMass();
            Vector3d momentOfInertia = vessel.findLocalMOI(centerOfMass);
            Vector3d up = (centerOfMass - vessel.mainBody.position).normalized;

            Quaternion targetRot = targetDir.Rotation;
            Transform vesselTransform = vessel.ReferenceTransform;
            Quaternion vesselRotation = vesselTransform.rotation * Quaternion.Euler(-90, 0, 0);

            Vector3d vesselForward = vesselRotation * Vector3d.forward;
            Vector3d vesselTop = vesselRotation * Vector3d.up;
            Vector3d vesselStarboard = vesselRotation * Vector3d.right;

            Vector3d targetForward = targetRot * Vector3d.forward;
            Vector3d targetTop = targetRot * Vector3d.up;
            Vector3d targetStarboard = targetRot * Vector3d.right;

            Vector3d torque = GetTorque(vessel, c.mainThrottle);
            //Vector3d angularVelocity = vesselRotation * vessel.rigidbody.angularVelocity;
            Vector3d angularVelocity = Quaternion.Inverse(vesselRotation) * vessel.rigidbody.angularVelocity;
            angularVelocity.x *= -1;
            //angularVelocity.y *= -1;
            angularVelocity.z *= -1;

            if (vForward != null)
            {
                vForward.Vector = vesselForward * multiplier;
                vTop.Vector = vesselTop * multiplier;
                vStarboard.Vector = vesselStarboard * multiplier;

                vTgtForward.Vector = targetForward * multiplier * 0.75f;
                vTgtTop.Vector = targetTop * multiplier * 0.75f;
                vTgtStarboard.Vector = targetStarboard * multiplier * 0.75f;

                vWorldX.Vector = new Vector3d(1, 0, 0) * multiplier * 0.5f;
                vWorldY.Vector = new Vector3d(0, 1, 0) * multiplier * 0.5f;
                vWorldZ.Vector = new Vector3d(0, 0, 1) * multiplier * 0.5f;

                //vWorldX.Vector = vesselTransform.right * multiplier * 0.5f;
                //vWorldY.Vector = vesselTransform.up * multiplier * 0.5f;
                //vWorldZ.Vector = vesselTransform.forward * multiplier * 0.5f;

                //vOmegaX.Vector = vesselForward * angularVelocity.x * multiplier * 100f;
                //vOmegaY.Vector = vesselTop * angularVelocity.y * multiplier * 100f;
                //vOmegaZ.Vector = vesselStarboard * angularVelocity.z * multiplier * 100f;

                vOmegaX.Vector = vesselTop * angularVelocity.x * multiplier * 100f;
                vOmegaX.Start = vesselForward * multiplier;
                vOmegaY.Vector = vesselStarboard * angularVelocity.y * multiplier * 100f;
                vOmegaY.Start = vesselForward * multiplier;
                vOmegaZ.Vector = vesselStarboard * angularVelocity.z * multiplier * 100f;
                vOmegaZ.Start = vesselTop * multiplier;
            }
            else
            {
                SafeHouse.Logger.LogWarning("Steering VectorRenderer is null");
            }

            // calculate phi and pitch, yaw, roll components of phi (angular error)
            double phi = Vector3d.Angle(vesselForward, targetForward) * Math.PI / 180d;
            if (Vector3d.Angle(vesselTop, targetForward) > 90)
                phi *= -1;
            double phiPitch = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselStarboard, targetForward)) * Math.PI / 180d;
            if (Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselStarboard, targetForward)) > 90)
                phiPitch *= -1;
            double phiYaw = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselTop, targetForward)) * Math.PI / 180d;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselTop, targetForward)) > 90)
                phiYaw *= -1;
            double phiRoll = Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselForward, targetTop)) * Math.PI / 180d;
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
            double tgtPitchOmega = phiPitch / dt1;
            double tgtYawOmega = phiYaw / dt1;
            //double tgtYawOmega = Math.Pow(Math.Sin(phiYaw), 2) * 10d;
            double tgtRollOmega = phiRoll / dtRoll1;

            // kill roll rotation until within 10 degrees of the target direction
            if (Math.Abs(phi) > 10 * Math.PI / 180d)
                tgtRollOmega = 0;

            // Adjust MOI if it's values are too low
            //if (momentOfInertia.x < 0.01)
            //    momentOfInertia.x = 0.01;
            if (momentOfInertia.y < 0.1)
                momentOfInertia.y = 0.1;
            //if (momentOfInertia.z < 0.01)
            //    momentOfInertia.z = 0.01;

            // Calculate the maximum allowable angular velocity and apply the limit, something we can stop in a reasonable amount of time
            double maxPitchOmega = torque.x * 0.5d / momentOfInertia.x;
            if (Math.Abs(tgtPitchOmega) > maxPitchOmega)
                tgtPitchOmega = maxPitchOmega * Math.Sign(tgtPitchOmega);
            double maxYawOmega = torque.z * 0.5d / momentOfInertia.z;
            if (Math.Abs(tgtYawOmega) > maxYawOmega)
                tgtYawOmega = maxYawOmega * Math.Sign(tgtYawOmega);
            double maxRollOmega = Math.PI / 10;
            if (Math.Abs(tgtRollOmega) > maxRollOmega)
                tgtRollOmega = maxRollOmega * Math.Sign(tgtRollOmega);

            // Calculate the desired torque to match to target
            double tgtPitchTorque = momentOfInertia.x * (tgtPitchOmega - angularVelocity.x) / dt2;
            double tgtYawTorque = momentOfInertia.z * (tgtYawOmega - angularVelocity.y) / dt2;
            double tgtRollTorque = momentOfInertia.y * (tgtRollOmega - angularVelocity.z) / dtRoll2;

            // Debug printing
            vTgtTorqueX.Vector = vesselTop * tgtPitchOmega * multiplier * 100f;
            vTgtTorqueX.Start = vesselForward * multiplier;
            vTgtTorqueX.SetLabel("tgtPitchOmega: " + tgtPitchOmega);
            vTgtTorqueY.Vector = vesselStarboard * tgtRollOmega * multiplier * 100f;
            vTgtTorqueY.Start = vesselTop * multiplier;
            vTgtTorqueY.SetLabel("tgtRollOmega: " + tgtRollOmega);
            vTgtTorqueZ.Vector = vesselStarboard * tgtYawOmega * multiplier * 100f;
            vTgtTorqueZ.Start = vesselForward * multiplier;
            vTgtTorqueZ.SetLabel("tgtYawOmega: " + tgtYawOmega);

            if (vessel.ActionGroups[KSPActionGroup.SAS])
            {
                Quaternion target = targetDir.Rotation * Quaternion.Euler(90, 0, 0);
                if (Quaternion.Angle(vessel.Autopilot.SAS.lockedHeading, target) > 5)
                    vessel.Autopilot.SAS.LockHeading(target, true);
                else
                    vessel.Autopilot.SAS.lockedHeading = target;
                return;
            }
            else
            {
                double epsilon = 0.0001;

                //c.pitch = 0;
                c.pitch = Mathf.Clamp((float)(tgtPitchTorque / torque.x), -1, 1);
                if (Math.Abs(c.pitch) < epsilon)
                    c.pitch = 0;
                //c.yaw = 0;
                c.yaw = Mathf.Clamp((float)(tgtYawTorque / torque.z), -1, 1);
                if (Math.Abs(c.yaw) < epsilon)
                    c.yaw = 0;
                //c.roll = 0;
                c.roll = Mathf.Clamp((float)(tgtRollTorque / torque.y), -1, 1);
                if (Math.Abs(c.roll) < epsilon)
                    c.roll = 0;
                //c.NeutralizeStick();
            }
        }

        public static Vector3d SwapYZ(Vector3d input)
        {
            return new Vector3d(input.x, input.z, input.y);
        }

        public static Vector3d Pow(Vector3d vector, float exponent)
        {
            return new Vector3d(Math.Pow(vector.x, exponent), Math.Pow(vector.y, exponent), Math.Pow(vector.z, exponent));
        }

        public static Vector3d GetEffectiveInertia(Vessel vessel, Vector3d torque)
        {
            var centerOfMass = vessel.findWorldCenterOfMass();
            var momentOfInertia = vessel.findLocalMOI(centerOfMass);
            var angularVelocity = Quaternion.Inverse(vessel.transform.rotation) * vessel.rigidbody.angularVelocity;
            var angularMomentum = new Vector3d(angularVelocity.x * momentOfInertia.x, angularVelocity.y * momentOfInertia.y, angularVelocity.z * momentOfInertia.z);

            var retVar = Vector3d.Scale
            (
                Sign(angularMomentum) * 2.0f,
                Vector3d.Scale(Pow(angularMomentum, 2), Inverse(Vector3d.Scale(torque, momentOfInertia)))
            );

            retVar.y *= 10;

            return retVar;
        }

        public static Vector3d GetTorque(Vessel vessel, float thrust)
        {
            var centerOfMass = vessel.findWorldCenterOfMass();
            var rollaxis = vessel.ReferenceTransform.up;
            rollaxis.Normalize();
            var pitchaxis = vessel.ReferenceTransform.forward;
            pitchaxis.Normalize();

            float pitch = 0.0f;
            float yaw = 0.0f;
            float roll = 0.0f;

            foreach (Part part in vessel.parts)
            {
                var relCoM = part.Rigidbody.worldCenterOfMass - centerOfMass;

                foreach (PartModule module in part.Modules)
                {
                    var wheel = module as ModuleReactionWheel;
                    if (wheel == null) continue;

                    pitch += wheel.PitchTorque;
                    yaw += wheel.YawTorque;
                    roll += wheel.RollTorque;
                }
                if (vessel.ActionGroups[KSPActionGroup.RCS])
                {
                    foreach (PartModule module in part.Modules)
                    {
                        var rcs = module as ModuleRCS;
                        if (rcs == null || !rcs.rcsEnabled) continue;

                        bool enoughfuel = rcs.propellants.All(p => (int)(p.totalResourceAvailable) != 0);
                        if (!enoughfuel) continue;
                        foreach (Transform thrustdir in rcs.thrusterTransforms)
                        {
                            string key = part.flightID.ToString() + thrustdir.name;
                            if (!vRcs.ContainsKey(key))
                            {
                                Color c = UnityEngine.Color.magenta;
                                var vecdraw = new VectorRenderer(Shared.UpdateHandler, Shared)
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
                            vRcs[key].Vector = thrustdir.forward * 10;
                            vRcs[key].Start = relCoM;

                            float rcsthrust = rcs.thrusterPower;
                            //just counting positive contributions in one direction. This is incorrect for asymmetric thruster placements.
                            roll += Mathf.Max(rcsthrust * Vector3.Dot(Vector3.Cross(relCoM, thrustdir.up), rollaxis), 0.0f);
                            pitch += Mathf.Max(rcsthrust * Vector3.Dot(Vector3.Cross(Vector3.Cross(relCoM, thrustdir.up), rollaxis), pitchaxis), 0.0f);
                            yaw += Mathf.Max(rcsthrust * Vector3.Dot(Vector3.Cross(Vector3.Cross(relCoM, thrustdir.up), rollaxis), Vector3.Cross(rollaxis, pitchaxis)), 0.0f);
                        }
                    }
                }
                pitch += (float)GetThrustTorque(part, vessel) * thrust;
                yaw += (float)GetThrustTorque(part, vessel) * thrust;
            }

            return new Vector3d(pitch, roll, yaw);
        }

        public static double GetThrustTorque(Part p, Vessel vessel)
        {
            foreach (PartModule module in p.Modules)
            {
                double thrust = 0;
                Vector3d position = p.Rigidbody.worldCenterOfMass - p.vessel.findWorldCenterOfMass();
                Quaternion rot;
                ModuleEngines me = module as ModuleEngines;
                if (me != null)
                {
                    if (me.isActiveAndEnabled)
                    {
                        foreach (var transform in me.thrustTransforms)
                        {
                            string key = p.flightID.ToString() + transform.name;
                            if (!vEngines.ContainsKey(key))
                            {
                                Color c = UnityEngine.Color.yellow;
                                var vecdraw = new VectorRenderer(Shared.UpdateHandler, Shared)
                                {
                                    Color = new RgbaColor(c.r, c.g, c.b),
                                    Start = new Vector3d(0, 0, 0),
                                    Vector = new Vector3d(0, 0, 0),
                                    Width = 0.25
                                };
                                vecdraw.SetLabel(key);
                                vecdraw.SetShow(true);
                                vEngines.Add(key, vecdraw);
                            }
                            vEngines[key].Vector = transform.forward * 50;
                            vEngines[key].Start = position;
                            //vEngines[key].Start = transform.localPosition;
                        }
                        thrust = me.GetCurrentThrust();
                    }
                }
                else
                {
                    ModuleGimbal gimbal = module as ModuleGimbal;
                    if (gimbal != null)
                    {
                        if (gimbal.isActiveAndEnabled && !gimbal.gimbalLock)
                        {
                            if (gimbal.initRots.Count > 0)
                                rot = gimbal.initRots[0];
                            double range = gimbal.gimbalRange * gimbal.gimbalLimiter;
                            range /= 100d;
                        }
                    }
                }
            }
            //TODO: implement gimbalthrust Torque calculation
            return 0;
        }

        private static Vector3d ReduceAngles(Vector3d input)
        {
            return new Vector3d(
                      (input.x > 180f) ? (input.x - 360f) : input.x,
                      -((input.y > 180f) ? (input.y - 360f) : input.y),
                      (input.z > 180f) ? (input.z - 360f) : input.z
                  );
        }

        public static Vector3d Inverse(Vector3d input)
        {
            return new Vector3d(1 / input.x, 1 / input.y, 1 / input.z);
        }

        public static Vector3d Sign(Vector3d vector)
        {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }
    }
}