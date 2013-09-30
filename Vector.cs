using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class Vector : SpecialValue
    {
        float x;
        float y;
        float z;

        public Vector(Vector3d init)
        {
            x = (float)init.x;
            y = (float)init.y;
            z = (float)init.z;
        }

        public Vector(double x, double y, double z)
        {
            this.x = (float)x;
            this.y = (float)y;
            this.z = (float)z;
        }

        public Vector(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Direction ToDirection()
        {
            return new Direction(ToVector3D(), false);
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "X") return x;
            if (suffixName == "Y") return y;
            if (suffixName == "Z") return z;

            return base.GetSuffix(suffixName);
        }

        public Vector3d ToVector3D()
        {
            return new Vector3d(x,y,z); 
        }

        public override string ToString()
        {
            return "V(" + x + ", " + y + ", " + z + ")";
        }

        public static implicit operator Vector3d(Vector d)
        {
            return d.ToVector3D();
        }

        public static explicit operator Direction(Vector d)
        {
            return new Direction(d.ToVector3D(), false);
        }

        public static Vector operator *(Vector a, Vector b) { return new Vector(a.x * b.x, a.y * b.y, a.z * b.z); }
        public static Vector operator *(Vector a, float b) { return new Vector(a.x * b, a.y * b, a.z * b); }
        public static Vector operator +(Vector a, Vector b) { return new Vector(a.ToVector3D() + b.ToVector3D()); }
        public static Vector operator -(Vector a, Vector b) { return new Vector(a.ToVector3D() - b.ToVector3D()); }
    }
}
