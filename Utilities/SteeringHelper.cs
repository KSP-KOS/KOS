using System;
using System.Collections.Generic;
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
            var momentOfInertia = vessel.findLocalMOI(centerOfMass);
            var target = targetDir.Rotation;
            var vesselR = vessel.transform.rotation;
            var delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0)*Quaternion.Inverse(vesselR)*target);

            var deltaEuler = ReduceAngles(delta.eulerAngles);
            deltaEuler.y *= -1;

            var torque = GetTorque(vessel);
            var inertia = GetEffectiveInertia(vessel, torque);

            var err = deltaEuler*Math.PI/180.0F;
            err += new Vector3d(inertia.x, inertia.z, inertia.y);

            var act = 120.0f*err;

            var precision = Mathf.Clamp((float) torque.x*20f/momentOfInertia.magnitude, 0.5f, 10f);
            var driveLimit = Mathf.Clamp01((float) (err.magnitude*380.0f/precision));

            act.x = Mathf.Clamp((float) act.x, -driveLimit, driveLimit);
            act.y = Mathf.Clamp((float) act.y, -driveLimit, driveLimit);
            act.z = Mathf.Clamp((float) act.z, -driveLimit, driveLimit);


            c.roll = Mathf.Clamp((float) (c.roll + act.z), -driveLimit, driveLimit);
            c.pitch = Mathf.Clamp((float) (c.pitch + act.x), -driveLimit, driveLimit);
            c.yaw = Mathf.Clamp((float) (c.yaw + act.y), -driveLimit, driveLimit);
        }

        public static Vector3d SwapYZ(Vector3d input)
        {
            return new Vector3d(input.x, input.z, input.y);
        }

        public static Vector3d Pow(Vector3d v3D, float exponent)
        {
            return new Vector3d(Math.Pow(v3D.x, exponent), Math.Pow(v3D.y, exponent), Math.Pow(v3D.z, exponent));
        }

        public static Vector3d GetEffectiveInertia(Vessel vessel, Vector3d torque)
        {
            var centerOfMass = vessel.findWorldCenterOfMass();
            var momentOfInertia = vessel.findLocalMOI(centerOfMass);
            var angularVelocity = Quaternion.Inverse(vessel.transform.rotation)*vessel.rigidbody.angularVelocity;
            var angularMomentum = new Vector3d(angularVelocity.x*momentOfInertia.x, angularVelocity.y*momentOfInertia.y,
                                               angularVelocity.z*momentOfInertia.z);

            var retVar = Vector3d.Scale
                (
                    Sign(angularMomentum)*2.0f,
                    Vector3d.Scale(Pow(angularMomentum, 2), Inverse(Vector3d.Scale(torque, momentOfInertia)))
                );

            retVar.y *= 10;

            return retVar;
        }

        public static Vector3d GetTorque(Vessel vessel)
        {
            var centerOfMass = vessel.findWorldCenterOfMass();

            float pitchYaw = 0;
            float roll = 0;

            foreach (var part in vessel.parts)
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
                    var max = rcsModule.thrusterPowers.Cast<float>().Aggregate<float, float>(0, Mathf.Max);

                    pitchYaw += max*relCoM.magnitude;
                }

                foreach (PartModule module in part.Modules)
                {
                    if (!(module is ModuleReactionWheel)) continue;
                    pitchYaw += ((ModuleReactionWheel) module).PitchTorque;
                    roll += ((ModuleReactionWheel) module).RollTorque;
                }
                float vectorThrust = (float) GetThrustTorque(part, vessel);
                pitchYaw += vectorThrust;
            }

            return new Vector3d(pitchYaw, roll, pitchYaw);
        }

        public static double GetThrustTorque(Part p, Vessel vessel)
        {
            var centerOfMass = vessel.CoM;
            if (p.State == PartStates.ACTIVE)
            {
                ModuleEngines engine = p.Modules.OfType<ModuleEngines>().FirstOrDefault();
                if (engine != null && engine.isOperational)
                {
                    float thrust = engine.CalculateThrust();
                    ModuleGimbal gimbal = p.Modules.OfType<ModuleGimbal>().FirstOrDefault();
                    if (gimbal != null && !gimbal.gimbalLock)
                    {
                        return Math.Sin(Math.Abs(gimbal.gimbalRange) * Math.PI / 180) *
                               thrust * (p.Rigidbody.worldCenterOfMass - centerOfMass).magnitude;
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
            return new Vector3d(1/input.x, 1/input.y, 1/input.z);
        }

        public static Vector3d Sign(Vector3d vector)
        {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }

        private static Vector3d AverageVector3D(IList<Vector3d> vectorArray, Vector3d newVector, int n)
        {
            double x = 0.0, y = 0.0, z = 0.0;
            var k = 0;

            // Loop through the array to determine average
            // Give more weight to newer items and less weight to older items
            for (var i = 0; i < n; i++)
            {
                k += i + 1;
                if (i < n - 1)
                {
                    vectorArray[i] = vectorArray[i + 1];
                }
                else
                {
                    vectorArray[i] = newVector;
                }
                x += vectorArray[i].x*(i + 1);
                y += vectorArray[i].y*(i + 1);
                z += vectorArray[i].z*(i + 1);
            }
            return new Vector3d(x/k, y/k, z/k);
        }
    }
}