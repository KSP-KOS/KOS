using System;
using System.Linq;
using UnityEngine;
using kOS.Suffixed;

namespace kOS.Utilities
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

            var centerOfMass = vessel.findWorldCenterOfMass();
            var moi = vessel.findLocalMOI(centerOfMass);

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

            float precision = Mathf.Clamp((float)torque.x * 20f / moi.magnitude, 0.5f, 10f);
            float driveLimit = Mathf.Clamp01((float)(err.magnitude * 380.0f / precision));

            act.x = Mathf.Clamp((float)act.x, -driveLimit, driveLimit);
            act.y = Mathf.Clamp((float)act.y, -driveLimit, driveLimit);
            act.z = Mathf.Clamp((float)act.z, -driveLimit, driveLimit);


            c.roll = Mathf.Clamp((float)(c.roll + act.z), -driveLimit, driveLimit);
            c.pitch = Mathf.Clamp((float)(c.pitch + act.x), -driveLimit, driveLimit);
            c.yaw = Mathf.Clamp((float)(c.yaw + act.y), -driveLimit, driveLimit);
        }

        public static Vector3d Pow(Vector3d v3D, float exponent)
        {
            return new Vector3d(Math.Pow(v3D.x, exponent), Math.Pow(v3D.y, exponent), Math.Pow(v3D.z, exponent));
        }

        public static Vector3d GetEffectiveInertia(Vessel vessel, Vector3d torque)
        {
            var centerOfMass = vessel.findWorldCenterOfMass();
            var moi = vessel.findLocalMOI(centerOfMass);
            var angularVelocity = Quaternion.Inverse(vessel.transform.rotation) * vessel.rigidbody.angularVelocity;
            var angularMomentum = new Vector3d(angularVelocity.x * moi.x, angularVelocity.y * moi.y, angularVelocity.z * moi.z);

            var retVar = Vector3d.Scale
            (
                Sign(angularMomentum) * 2.0f,
                Vector3d.Scale(Pow(angularMomentum, 2), Inverse(Vector3d.Scale(torque, moi)))
            );

            retVar.y *= 10;

            return retVar;
        }

        public static Vector3d GetTorque(Vessel vessel, float thrust)
        {
            var centerOfMass = vessel.findWorldCenterOfMass();
            
            float pitchYaw = 0;
            float roll = 0;

            foreach (Part part in vessel.parts)
            {
                var relCoM = part.Rigidbody.worldCenterOfMass - centerOfMass;

                var pod = part as CommandPod;
                if (pod != null)
                {
                    pitchYaw += Math.Abs(pod.rotPower);
                    roll += Math.Abs(pod.rotPower);
                }

                var rcsModule = part as RCSModule;
                if (rcsModule != null)
                {
                    float max = rcsModule.thrusterPowers.Cast<float>().Aggregate<float, float>(0, Mathf.Max);

                    pitchYaw += max * relCoM.magnitude;
                }

                foreach (PartModule module in part.Modules)
                {
                    var wheel = module as ModuleReactionWheel;
                    if (wheel == null) continue;
                    pitchYaw += wheel.PitchTorque;
                    roll += wheel.RollTorque;
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


