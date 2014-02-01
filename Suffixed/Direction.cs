using System;
using UnityEngine;

namespace kOS.Suffixed
{
    public class Direction : SpecialValue
    {
        private Vector3d euler;

        private Quaternion rotation;
        private Vector3d vector;

        public Direction()
        {
        }

        public Direction(Quaternion q)
        {
            rotation = q;
            euler = q.eulerAngles;
        }

        public Direction(Vector3d v3D, bool isEuler)
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

        public Vector3d Vector
        {
            get { return vector; }
            set
            {
                vector = value;
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
            }
        }

        public Quaternion Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                euler = value.eulerAngles;
            }
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "PITCH":
                    return euler.x;
                case "YAW":
                    return euler.y;
                case "ROLL":
                    return euler.z;
                case "VECTOR":
                    return new Vector(vector);
            }

            return base.GetSuffix(suffixName);
        }

        public static Direction operator *(Direction a, Direction b)
        {
            return new Direction(a.Rotation*b.Rotation);
        }

        public static Direction operator +(Direction a, Direction b)
        {
            return new Direction(a.Euler + b.Euler, true);
        }

        public static Direction operator -(Direction a, Direction b)
        {
            return new Direction(a.Euler - b.Euler, true);
        }

        public object TryOperation(string op, object other, bool reverseOrder)
        {
            if (other is Vector)
            {
                other = ((Vector) other).ToDirection();
            }

            if (op == "*" && other is Direction)
            {
                // If I remember correctly, order of multiplication DOES matter with quaternions
                return !reverseOrder ? this*(Direction) other : (Direction) other*this;
            }

            if (op == "+" && other is Direction)
            {
                return this + (Direction) other;
            }

            if (op == "-" && other is Direction)
            {
                return !reverseOrder ? this - (Direction) other : (Direction) other - this;
            }

            return null;
        }

        public override string ToString()
        {
            return "R(" + Math.Round(euler.x, 3) + "," + Math.Round(euler.y, 3) + "," + Math.Round(euler.z, 3) + ")";
        }
    }
}