using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace kOS
{
    public class Direction : SpecialValue
    {
        private Vector3d vector;
        public Vector3d Vector
        {
            get { return vector; }
            set 
            { 
                vector = value; rotation = Quaternion.LookRotation(value); euler = rotation.eulerAngles; 
            }
        }

        private Vector3d euler;
        public Vector3d Euler
        {
            get { return euler; }
            set 
            { 
                euler = value; rotation = Quaternion.Euler(value);
            }
        }

        private Quaternion rotation;
        public Quaternion Rotation
        {
            get { return rotation; }
            set { rotation = value; euler = value.eulerAngles; }
        }

        public Direction()
        {
        }

        public Direction(Quaternion q)
        {
            rotation = q;
            euler = q.eulerAngles;
        }

        public Direction(Vector3d v3d, bool isEuler)
        {
            if (isEuler)
            {
                Euler = v3d;
            }
            else
            {
                Vector = v3d; 
            }
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "PITCH") return euler.x;
            if (suffixName == "YAW") return euler.y;
            if (suffixName == "ROLL") return euler.z;
            if (suffixName == "VECTOR") return new kOS.Vector(vector);

            return base.GetSuffix(suffixName);
        }

        public void RedefineUp(Vector3d up)
        {
        }

        public static Direction operator *(Direction a, Direction b) { return new Direction(a.Rotation * b.Rotation); }
        public static Direction operator +(Direction a, Direction b) { return new Direction(a.Euler + b.Euler, true); }
        public static Direction operator -(Direction a, Direction b) { return new Direction(a.Euler - b.Euler, true); }
        
        public override object TryOperation(string op, object other, bool reverseOrder)
        {
            if (other is Vector)
            {
                other = ((Vector)other).ToDirection();
            }

            if (op == "*" && other is Direction)
            {
                // If I remember correctly, order of multiplication DOES matter with quaternions
                if (!reverseOrder)
                    return this * (Direction)other;
                else
                    return (Direction)other * this;
            }
            else if (op == "+" && other is Direction) return this + (Direction)other;
            else if (op == "-" && other is Direction)
            {
                if (!reverseOrder)
                    return this - (Direction)other;
                else
                    return (Direction)other - this;
            }

            return null;
        }

        public override string ToString()
        {
            return "R(" + Math.Round(euler.x, 3) + "," + Math.Round(euler.y, 3) + "," + Math.Round(euler.z, 3) + ")";
        }
    }
}
