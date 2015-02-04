using System;
using System.Linq;
using UnityEngine;
using kOS.Suffixed;

namespace kOS.Utilities
{
    public static class SteeringHelper
    {
        public static Vector3d PrevErr;
        public static Vector3d Integral;

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
            if (vessel == null)
            {
                Debug.LogError("kOS: SteerShipToward: Vessel is null!!");
                return;
            }

            var centerOfMass = vessel.findWorldCenterOfMass();
            var momentOfInertia = vessel.findLocalMOI(centerOfMass);
            var mass = vessel.GetTotalMass();
            var up = (centerOfMass - vessel.mainBody.position).normalized;

            var target = targetDir.Rotation;
            var vesselRotation = vessel.ReferenceTransform.rotation;

            // some validations
            if (!Utils.IsValidNumber(c.mainThrottle) ||
                !Utils.IsValidVector(centerOfMass) ||
                !Utils.IsValidNumber(mass) ||
                !Utils.IsValidVector(up) ||
                !Utils.IsValidRotation(target) ||
                !Utils.IsValidRotation(vesselRotation))
            {
                return;
            }

            Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselRotation) * target);

            Vector3d deltaEuler = ReduceAngles(delta.eulerAngles);
            deltaEuler.y *= -1;

            Vector3d torque = GetTorque(vessel, c.mainThrottle);
            Vector3d inertia = GetEffectiveInertia(vessel, torque);

            Vector3d err = deltaEuler * Math.PI / 180.0F;
            err += new Vector3d(inertia.x, inertia.z, inertia.y);
            //err.Scale(SwapYZ(Vector3d.Scale(MoI, Inverse(torque))));

            PrevErr = err;

            Vector3d act = 120.0f * err;

            float precision = Mathf.Clamp((float)torque.x * 20f / momentOfInertia.magnitude, 0.5f, 10f);
            float driveLimit = Mathf.Clamp01((float)(err.magnitude * 380.0f / precision));

            act.x = Mathf.Clamp((float)act.x, -driveLimit, driveLimit);
            act.y = Mathf.Clamp((float)act.y, -driveLimit, driveLimit);
            act.z = Mathf.Clamp((float)act.z, -driveLimit, driveLimit);

            //act = averageVector3d(averagedAct, act, 2);

            c.roll = Mathf.Clamp((float)(c.roll + act.z), -driveLimit, driveLimit);
            c.pitch = Mathf.Clamp((float)(c.pitch + act.x), -driveLimit, driveLimit);
            c.yaw = Mathf.Clamp((float)(c.yaw + act.y), -driveLimit, driveLimit);

            /*
            // This revised version from 0.6 gave people problems with gravity turns. I've reverted but may try to make it work
             
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
            c.yaw = Mathf.Clamp((float)(c.yaw + act.y), -drive_limit, drive_limit);*/
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
            var rollaxis = vessel.transform.up;
            rollaxis.Normalize ();
            var pitchaxis = vessel.GetFwdVector ();
            pitchaxis.Normalize ();
            
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
                if (vessel.ActionGroups [KSPActionGroup.RCS])
                {
                    foreach (PartModule module in part.Modules) {
                        var rcs = module as ModuleRCS;
                        if (rcs == null || !rcs.rcsEnabled) continue;

                        bool enoughfuel = rcs.propellants.All(p => (int) (p.totalResourceAvailable) != 0);
                        if (!enoughfuel) continue;
                        foreach (Transform thrustdir in rcs.thrusterTransforms)
                        {
                            float rcsthrust = rcs.thrusterPower;
                            //just counting positive contributions in one direction. This is incorrect for asymmetric thruster placements.
                            roll += Mathf.Max(rcsthrust * Vector3.Dot(Vector3.Cross(relCoM, thrustdir.up), rollaxis), 0.0f);
                            pitch += Mathf.Max(rcsthrust * Vector3.Dot(Vector3.Cross(Vector3.Cross(relCoM, thrustdir.up), rollaxis), pitchaxis), 0.0f);
                            yaw += Mathf.Max(rcsthrust * Vector3.Dot(Vector3.Cross(Vector3.Cross(relCoM, thrustdir.up), rollaxis), Vector3.Cross(rollaxis,pitchaxis)),0.0f);
                        }
                    }
                }
                pitch += (float)GetThrustTorque(part, vessel) * thrust;
                yaw += (float)GetThrustTorque (part, vessel) * thrust;
            }
            
            return new Vector3d(pitch, roll, yaw);
        }

        public static double GetThrustTorque(Part p, Vessel vessel)
        {
            //TODO: implement gimbalthrust Torque calculation
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
