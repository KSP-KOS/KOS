using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using UnityEngine;
using kOS.Safe.Serialization;
using System.Collections.Generic;
using kOS.Safe.Utilities;
using kOS.Safe;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Vector")]
    public class Vector : SerializableStructure
    {
        public const string DumpX = "x";
        public const string DumpY = "y";
        public const string DumpZ = "z";

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        /// <summary>
        /// Makes a vector of zero magnitude (V(0,0,0)).
        /// </summary>
        public Vector()
        {
            RegisterInitializer(InitializeSuffixes);
        }

        public Vector(Vector3d init)
            : this(init.x, init.y, init.z)
        {
        }

        public Vector(Vector3 init)
            : this(init.x, init.y, init.z)
        {
        }

        public Vector(float x, float y, float z)
            : this((double)x, (double)y, (double)z)
        {
        }

        public Vector(double x, double y, double z) : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static Vector CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new Vector();
            newObj.LoadDump(d);
            return newObj;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("X", new SetSuffix<ScalarValue>(() => X, value => X = value));
            AddSuffix("Y", new SetSuffix<ScalarValue>(() => Y, value => Y = value));
            AddSuffix("Z", new SetSuffix<ScalarValue>(() => Z, value => Z = value));
            AddSuffix("MAG", new SetSuffix<ScalarValue>(Magnitude, value =>
            {
                double oldMag = new Vector3d(X, Y, Z).magnitude;

                if (oldMag == 0) return; // Avoid division by zero

                X = X / oldMag * value;
                Y = Y / oldMag * value;
                Z = Z / oldMag * value;
            }));
            AddSuffix("VEC", new Suffix<Vector>(() => new Vector(X, Y, Z)));
            AddSuffix("NORMALIZED", new Suffix<Vector>(Normalized));
            AddSuffix("SQRMAGNITUDE", new Suffix<ScalarValue>(() => new Vector3d(X, Y, Z).sqrMagnitude));
            AddSuffix("DIRECTION", new SetSuffix<Direction>(ToDirection, value =>
            {
                var newMagnitude = Vector3d.forward * new Vector3d(X, Y, Z).magnitude;

                var newVector = value.Rotation * newMagnitude;

                X = newVector.x;
                Y = newVector.y;
                Z = newVector.z;
            }));
        }

        public ScalarValue Magnitude()
        {
            return new Vector3d(X, Y, Z).magnitude;
        }

        public Vector Normalized()
        {
            return new Vector(new Vector3d(X, Y, Z).normalized);
        }

        public Direction ToDirection()
        {
            return new Direction(ToVector3D(), false);
        }

        public static Vector Zero
        {
            get { return new Vector(Vector3d.zero); }
        }

        public Vector3d ToVector3D()
        {
            return new Vector3d(X, Y, Z);
        }

        public Vector3 ToVector3() // Vector3 is the single-precision version of Vector3D.
        {
            return new Vector3((float)X, (float)Y, (float)Z);
        }

        public override string ToString()
        {
            return "V(" + X + ", " + Y + ", " + Z + ")";
        }

        public override bool Equals(object obj)
        {
            Type compareType = typeof(Vector);
            if (compareType.IsInstanceOfType(obj))
            {
                Vector b = obj as Vector;
                return (X == b.X && Y == b.Y && Z == b.Z);
            }
            return false;
        }

        public override int GetHashCode()
        {
            // just returning a randomly selected constant hash value
            // since the class is mutable, so we cannot rely on X, Y, or Z
            // remaining constant for the entire object life
            return ~23; // bitwise complement "23"... just cause
        }

        public static implicit operator Vector3d(Vector d)
        {
            return d.ToVector3D();
        }

        public static explicit operator Direction(Vector d)
        {
            return new Direction(d.ToVector3D(), false);
        }

        public static double operator *(Vector a, Vector b)
        {
            return (Vector3d.Dot(a.ToVector3D(), b.ToVector3D()));
        }

        public static Vector operator *(Vector a, float b)
        {
            return new Vector(a.X * b, a.Y * b, a.Z * b);
        }

        public static Vector operator *(Vector a, double b)
        {
            return new Vector(a.X * b, a.Y * b, a.Z * b);
        }

        public static Vector operator *(Vector a, ScalarValue b)
        {
            return a * b.GetDoubleValue();
        }

        public static Vector operator *(float b, Vector a)
        {
            return new Vector(a.X * b, a.Y * b, a.Z * b);
        }

        public static Vector operator *(double b, Vector a)
        {
            return new Vector(a.X * b, a.Y * b, a.Z * b);
        }

        public static Vector operator *(ScalarValue b, Vector a)
        {
            return a * b.GetDoubleValue();
        }

        public static Vector operator /(Vector a, ScalarValue b)
        {
            return new Vector(a.X / b, a.Y / b, a.Z / b);
        }

        public static Vector operator /(Vector a, float b)
        {
            return new Vector(a.X / b, a.Y / b, a.Z / b);
        }

        public static Vector operator /(Vector a, double b)
        {
            return new Vector(a.X / b, a.Y / b, a.Z / b);
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.ToVector3D() + b.ToVector3D());
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.ToVector3D() - b.ToVector3D());
        }

        public static Vector operator -(Vector a)
        {
            return a * (-1d);
        }

        public static bool operator ==(Vector a, Vector b)
        {
            Type compareType = typeof(Vector);
            if (compareType.IsInstanceOfType(a))
            {
                return a.Equals(b); // a is not null, we can use the built in equals function
            }
            return !compareType.IsInstanceOfType(b); // a is null, return true if b is null and false if not null
        }

        public static bool operator !=(Vector a, Vector b)
        {
            return !(a == b);
        }

        public override Dump Dump()
        {
            DumpWithHeader dump = new DumpWithHeader();

            dump.Add(DumpX, X);
            dump.Add(DumpY, Y);
            dump.Add(DumpZ, Z);

            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            X = Convert.ToDouble(dump[DumpX]);
            Y = Convert.ToDouble(dump[DumpY]);
            Z = Convert.ToDouble(dump[DumpZ]);
        }
    }
}