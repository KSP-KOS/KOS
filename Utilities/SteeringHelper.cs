using System;
using System.Linq;
using UnityEngine;
using kOS.Suffixed;

namespace kOS
{
    
    public static class SteeringHelper
    {
        public static void KillRotation(FlightCtrlState c, Vessel vessel)
        {
            var act = vessel.transform.InverseTransformDirection(vessel.rigidbody.angularVelocity).normalized;
            
            c.pitch = act.x;
            c.roll = act.y;
            c.yaw = act.z;

            c.killRot = true;
        }

        public static void SteerShipToward(Direction targetDir, FlightCtrlState c, Vessel vessel)
        {
            // I take no credit for this, this is a stripped down, rearranged version of MechJeb's attitude control system

            var CoM = vessel.findWorldCenterOfMass();
            var MoI = vessel.findLocalMOI(CoM);

            var target = targetDir.Rotation;
            var vesselR = vessel.transform.rotation;

            Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselR) * target);

            Vector3d deltaEuler = ReduceAngles(delta.eulerAngles);
            deltaEuler.y *= -1;

            Vector3d torque = GetTorque(vessel, c.mainThrottle);
            Vector3d inertia = GetEffectiveInertia(vessel, torque);

            Vector3d err = deltaEuler * Math.PI / 180.0F;
            err += new Vector3d(inertia.x, inertia.z, inertia.y);

            Vector3d act = 120.0f * err;

            float precision = Mathf.Clamp((float)torque.x * 20f / MoI.magnitude, 0.5f, 10f);
            float driveLimit = Mathf.Clamp01((float)(err.magnitude * 380.0f / precision));

            act.x = Mathf.Clamp((float)act.x, -driveLimit, driveLimit);
            act.y = Mathf.Clamp((float)act.y, -driveLimit, driveLimit);
            act.z = Mathf.Clamp((float)act.z, -driveLimit, driveLimit);


            c.roll = Mathf.Clamp((float)(c.roll + act.z), -driveLimit, driveLimit);
            c.pitch = Mathf.Clamp((float)(c.pitch + act.x), -driveLimit, driveLimit);
            c.yaw = Mathf.Clamp((float)(c.yaw + act.y), -driveLimit, driveLimit);
            UnityEngine.Debug.Log("kOS Steer Throttle: " + c.mainThrottle);
            UnityEngine.Debug.Log("kOS Steer Pitch: " + c.pitch);
            UnityEngine.Debug.Log("kOS Steer Roll: " + c.roll);
            UnityEngine.Debug.Log("kOS Steer Yaw: " + c.yaw);
        }

        public static Vector3d Pow(Vector3d v3d, float exponent)
        {
            return new Vector3d(Math.Pow(v3d.x, exponent), Math.Pow(v3d.y, exponent), Math.Pow(v3d.z, exponent));
        }

        public static Vector3d GetEffectiveInertia(Vessel vessel, Vector3d torque)
        {
            var CoM = vessel.findWorldCenterOfMass();
            var MoI = vessel.findLocalMOI(CoM);
            var angularVelocity = Quaternion.Inverse(vessel.transform.rotation) * vessel.rigidbody.angularVelocity;
            var angularMomentum = new Vector3d(angularVelocity.x * MoI.x, angularVelocity.y * MoI.y, angularVelocity.z * MoI.z);

            var retVar = Vector3d.Scale
            (
                Sign(angularMomentum) * 2.0f,
                Vector3d.Scale(Pow(angularMomentum, 2), Inverse(Vector3d.Scale(torque, MoI)))
            );

            retVar.y *= 10;

            return retVar;
        }

        public static Vector3d GetTorque(Vessel vessel, float thrust)
        {
            var CoM = vessel.findWorldCenterOfMass();
            
            float pitchYaw = 0;
            float roll = 0;

            foreach (Part part in vessel.parts)
            {
                var relCoM = part.Rigidbody.worldCenterOfMass - CoM;

                if (part is CommandPod)
                {
                    pitchYaw += Math.Abs(((CommandPod)part).rotPower);
                    roll += Math.Abs(((CommandPod)part).rotPower);
                }

                if (part is RCSModule)
                {
                    float max = 0;
                    foreach (float power in ((RCSModule)part).thrusterPowers)
                    {
                        max = Mathf.Max(max, power);
                    }

                    pitchYaw += max * relCoM.magnitude;
                }

                foreach (PartModule module in part.Modules)
                {
                    if (module is ModuleReactionWheel)
                    {
                        pitchYaw += ((ModuleReactionWheel)module).PitchTorque;
                        roll += ((ModuleReactionWheel)module).RollTorque;
                    }
                }
                //TODO:This causes crazy steering, but should be figured out.
                //pitchYaw += (float)GetThrustTorque(part, vessel) * thrust;
            }
            
            return new Vector3d(pitchYaw, roll, pitchYaw);
        }

           public static double GetThrustTorque(Part p, Vessel vessel)
        {
            if (p.State == PartStates.ACTIVE)
            {
                var gimbal = p.Modules.OfType<ModuleGimbal>().FirstOrDefault();

                if (gimbal != null && !gimbal.gimbalLock)
                {
                    var engine = p.Modules.OfType<ModuleEngines>().FirstOrDefault();
                    var fxengine = p.Modules.OfType<ModuleEnginesFX>().FirstOrDefault();

                    var magnitude = (p.Rigidbody.worldCenterOfMass - vessel.CoM).magnitude;
                    var gimbalRange = Math.Sin(Math.Abs(gimbal.gimbalRange));

                    var engineActive = engine != null && engine.isOperational;
                    var enginefxActive = fxengine != null && fxengine.isOperational;

                    if (engineActive)
                    {
                        return gimbalRange * engine.finalThrust * magnitude;
                    }
                    if (enginefxActive)
                    {
                        return gimbalRange * fxengine.finalThrust * magnitude;
                    }
                }
            }
            return 0;
        }

        private static Vector3d ReduceAngles(Vector3d input)
        {
            return new Vector3d(
                      (input.x > 180f) ? (input.x - 360f) : input.x,
                      (input.y > 180f) ? (input.y - 360f) : input.y,
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


