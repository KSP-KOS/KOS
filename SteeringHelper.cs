using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    
    public static class SteeringHelper
    {
        public static Vector3d prev_err;
        public static Vector3d integral;
        private static Vector3d[] averagedAct = new Vector3d[5];

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
            var mass = vessel.GetTotalMass();
            var up = (CoM - vessel.mainBody.position).normalized;

            var target = targetDir.Rotation;
            var vesselR = vessel.transform.rotation;

            Quaternion delta;
            delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselR) * target);

            Vector3d deltaEuler = ReduceAngles(delta.eulerAngles);
            deltaEuler.y *= -1;

            Vector3d torque = GetTorque(vessel, c.mainThrottle);
            Vector3d inertia = GetEffectiveInertia(vessel, torque);

            Vector3d err = deltaEuler * Math.PI / 180.0F;
            err += SwapYZ(inertia * 8);
            err.Scale(SwapYZ(Vector3d.Scale(MoI * 3, Inverse(torque))));

            prev_err = err;

            Vector3d act = 400.0f * err;

            float precision = Mathf.Clamp((float)torque.x * 20f / MoI.magnitude, 0.5f, 10f);
            float drive_limit = Mathf.Clamp01((float)(err.magnitude * 450.0f / precision));
            
            act.x = Mathf.Clamp((float)act.x, -drive_limit, drive_limit);
            act.y = Mathf.Clamp((float)act.y, -drive_limit, drive_limit);
            act.z = Mathf.Clamp((float)act.z, -drive_limit, drive_limit);

            //act = averageVector3d(averagedAct, act, 2);

            c.roll = Mathf.Clamp((float)(c.roll + act.z), -drive_limit, drive_limit);
            c.pitch = Mathf.Clamp((float)(c.pitch + act.x), -drive_limit, drive_limit);
            c.yaw = Mathf.Clamp((float)(c.yaw + act.y), -drive_limit, drive_limit);
        }

        public static Vector3d SwapYZ(Vector3d input)
        {
            return new Vector3d(input.x, input.z, input.y);
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

                pitchYaw += (float)GetThrustTorque(part, vessel) * thrust;
            }
            
            return new Vector3d(pitchYaw, roll, pitchYaw);
        }

        public static double GetThrustTorque(Part p, Vessel vessel)
        {
            var CoM = vessel.CoM;

            if (p.State == PartStates.ACTIVE)
            {
                if (p is LiquidEngine)
                {
                    if (((LiquidEngine)p).thrustVectoringCapable)
                    {
                        return Math.Sin(Math.Abs(((LiquidEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                }
                else if (p is LiquidFuelEngine)
                {
                    if (((LiquidFuelEngine)p).thrustVectoringCapable)
                    {
                        return Math.Sin(Math.Abs(((LiquidFuelEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidFuelEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                }
                else if (p is AtmosphericEngine)
                {
                    if (((AtmosphericEngine)p).thrustVectoringCapable)
                    {
                        return Math.Sin(Math.Abs(((AtmosphericEngine)p).gimbalRange) * Math.PI / 180) * ((AtmosphericEngine)p).maximumEnginePower * ((AtmosphericEngine)p).totalEfficiency * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
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

        private static Vector3d averageVector3d(Vector3d[] vectorArray, Vector3d newVector, int n)
        {
            double x = 0.0, y = 0.0, z = 0.0;
            int k = 0;

            // Loop through the array to determine average
            // Give more weight to newer items and less weight to older items
            for (int i = 0; i < n; i++)
            {
                k += i + 1;
                if (i < n - 1) { vectorArray[i] = vectorArray[i + 1]; }
                else { vectorArray[i] = newVector; }
                x += vectorArray[i].x * (i + 1);
                y += vectorArray[i].y * (i + 1);
                z += vectorArray[i].z * (i + 1);
            }
            return new Vector3d(x / k, y / k, z / k);
        }
    }
}
