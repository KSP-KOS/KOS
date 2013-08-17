using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    public class Direction
    {
        private bool rollMatters;

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
        }

        public Direction(Vector3d v3d, bool isEuler)
        {
            if (isEuler)
            {
                rollMatters = true;
                Euler = v3d;
            }
            else
            {
                rollMatters = false;
                Vector = v3d; 
            }
        }

        public void RedefineUp(Vector3d up)
        {
        }

        public static Direction operator *(Direction a, Direction b) { return new Direction(a.Rotation * b.Rotation); }
        public static Direction operator +(Direction a, Direction b) { return new Direction(a.Euler + b.Euler, true); }
        public static Direction operator -(Direction a, Direction b) { return new Direction(a.Euler - b.Euler, true); }

        public override string ToString()
        {
            return "R(" + Math.Round(euler.x, 3) + "," + Math.Round(euler.y, 3) + "," + Math.Round(euler.z, 3) + ")";
        }
    }
}
