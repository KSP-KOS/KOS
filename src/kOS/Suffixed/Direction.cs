using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using UnityEngine;

namespace kOS.Suffixed
{
    public class Direction : Structure
    {
        private Vector3d euler;
        private Quaternion rotation;
        private Vector3d vector;

        public Direction()
        {
            DirectionInitializeSuffixes();
        }

        public Direction(Quaternion q)
            : this()
        {
            rotation = q;
            euler = q.eulerAngles;
            vector = rotation * Vector3d.forward;
        }

        public Direction(Vector3d v3D, bool isEuler)
            : this()
        {
            if (isEuler)
            {
                Euler = v3D;
            }
            else
            {
                Vector = v3D;
            }
        }

        // The following two are effectively constructors, but because they have
        // identical signatures they couldn't be differentiated if they were
        // constructors (which is probably why Unity didn't make them constructors
        // either):

        /// <summary>
        /// Produces a direction that if it was applied to vector v1, would
        /// cause it to rotate to be the same direction as vector v2.
        /// Note that there are technically an infinite number of such
        /// directions that it could legally return, because this method
        /// does not control the roll, the roll being lost information
        /// when dealing with just a single vector.
        /// </summary>
        /// <param name="v1">start from this vector</param>
        /// <param name="v2">go to this vector </param>
        /// <returns></returns>
        public static Direction FromVectorToVector(Vector3d v1, Vector3d v2)
        {
            return new Direction(Quaternion.FromToRotation(v1, v2));
        }

        /// <summary>
        /// Produces a direction in which you are looking in lookdirection, and
        /// rolled such that up is upDirection.
        /// Note, lookDirection and Updirection do not have to be perpendicular, but
        /// the farther from perpendicular they are, the worse the accuracy gets.
        /// </summary>
        /// <param name="lookDirection">direction to point</param>
        /// <param name="upDirection">direction for the 'TOP' of the roll axis</param>
        /// <returns>new direction.</returns>
        public static Direction LookRotation(Vector3d lookDirection, Vector3d upDirection)
        {
            return new Direction(Quaternion.LookRotation(lookDirection, upDirection));
        }

        // This next one doesn't have a common signature, but it's kept as a static
        // instead of a constructor because it's coherent with the other ones that way:

        /// <summary>
        /// Make a rotation of a given angle around a given axis vector.
        /// <param name="degrees">The angle around the axis to rotate, in degrees.</param>
        /// <param name="axis">The axis to rotate around.  Rotations use a left-hand rule because it's a left-handed coord system.</param>
        /// </summary>
        public static Direction AngleAxis(double degrees, Vector3d axis)
        {
            return new Direction(Quaternion.AngleAxis((float)degrees, axis));
        }

        public Vector3d Vector
        {
            get { return vector; }
            set
            {
                vector = value.normalized;
                rotation = Quaternion.LookRotation(value);
                euler = rotation.eulerAngles;
            }
        }

        public Vector3d Euler
        {
            get { return euler; }
            set
            {
                euler = value;
                rotation = Quaternion.Euler(value);
                vector = rotation * Vector3d.forward;
            }
        }

        public Quaternion Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                euler = value.eulerAngles;
                vector = rotation * Vector3d.forward;
            }
        }

        private void DirectionInitializeSuffixes()
        {
            AddSuffix("PITCH",
                      new Suffix<double>(() => euler.x,
                                         "The rotation around the universe's X axis.  The word 'PITCH' is a misnomer."));
            AddSuffix("YAW",
                      new Suffix<double>(() => euler.y,
                                         "The rotation around the universe's Y axis.  The word 'YAW' is a misnomer."));
            AddSuffix("ROLL",
                      new Suffix<double>(() => euler.z,
                                         "The rotation around the universe's Z axis.  The word 'ROLL' is a misnomer."));
            AddSuffix(new[] { "FOREVECTOR", "VECTOR" },
                      new Suffix<Vector>(() => new Vector(vector),
                                         "This direction's forward direction expressed as a unit vector."));
            AddSuffix(new[] { "TOPVECTOR", "UPVECTOR" },
                      new Suffix<Vector>(() => new Vector(rotation * Vector3.up),
                                         "This direction's top direction expressed as a unit vector."));
            AddSuffix(new[] { "STARVECTOR", "RIGHTVECTOR" },
                      new Suffix<Vector>(() => new Vector(rotation * Vector3.right),
                                         "This direction's starboard direction expressed as a unit vector."));
            AddSuffix("INVERSE",
                      new Suffix<Direction>(() => new Direction(rotation.Inverse()),
                                            "Returns the inverse of this direction - meaning the rotation that would go FROM this direction TO the universe's raw orientation."));
        }

        public static Direction operator *(Direction a, Direction b)
        {
            return new Direction(a.Rotation * b.Rotation);
        }

        public static Vector operator *(Direction a, Vector b)
        {
            return new Vector(a.Rotation * (Vector3d)b);
        }

        public static Vector operator *(Vector b, Direction a)
        {
            return new Vector(a.Rotation * (Vector3d)b);
        }

        public static Direction operator +(Direction a, Direction b)
        {
            return new Direction(a.Euler + b.Euler, true);
        }

        public static Direction operator -(Direction a, Direction b)
        {
            return new Direction(a.Euler - b.Euler, true);
        }

        public static Direction operator -(Direction a)
        {
            return new Direction(a.rotation.Inverse());
        }
        
        public override object TryOperation(string op, object other, bool reverseOrder)
        {
            var otherVector = other as Vector;
            if (otherVector != null)
            {
                Vector3d vec = otherVector;
                return new Vector(Rotation * vec);
            }

            if (op == "*" && other is Direction)
            {
                // If I remember correctly, order of multiplication DOES matter with quaternions
                return !reverseOrder ? this * (Direction)other : (Direction)other * this;
            }

            if (op == "+" && other is Direction)
            {
                return this + (Direction)other;
            }

            if (op == "-" && other is Direction)
            {
                return !reverseOrder ? this - (Direction)other : (Direction)other - this;
            }

            return null;
        }

        /// <summary>
        /// Returns this rotation relative to a starting rotation - ie.. how you would
        /// have to rotate from that start rotation to get to this one.
        /// </summary>
        /// <param name="fromDir">start rotation.</param>
        /// <returns>new Direction representing such a rotation.</returns>
        public Direction RelativeFrom(Direction fromDir)
        {
            return new Direction(Quaternion.RotateTowards(fromDir.rotation, rotation, 99999.0f));
        }

        public override string ToString()
        {
            return "R(" + Math.Round(euler.x, 3) + "," + Math.Round(euler.y, 3) + "," + Math.Round(euler.z, 3) + ")";
        }
    }
}