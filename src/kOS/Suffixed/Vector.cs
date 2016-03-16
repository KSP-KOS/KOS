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

        public Vector()
        {
            InitializeSuffixes();
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

        public override object TryOperation(string op, object other, bool reverseOrder)
        {
            other = ConvertToDoubleIfNeeded(other);
            other = Structure.ToPrimitive(other);

            switch (op)
            {
                case "*":
                    if (other is Vector) return this * (Vector)other;
                    if (other is double) return this * (double)other;
                    break;

                case "/":
                    if (!reverseOrder)
                    {
                        if (other is Vector) throw new Exception("Cannot divide by a vector.");
                        if (other is double) return this * (1.0 / (double)other);
                    }
                    else
                    {
                        throw new NotImplementedException("Cannot divide by a vector.");
                    }
                    break;

                case "+":
                    if (other is Vector) return this + (Vector)other;
                    break;

                case "-":
                    if (!reverseOrder)
                    {
                        if (other is Vector) return this - (Vector)other;
                    }
                    else
                    {
                        if (other is Vector) return (Vector)other - this;
                    }
                    break;

                default:
                    throw new NotImplementedException(string.Format(
                        "Cannot perform operation: {0} {1} {2}", this, op, other));
            }

            return null;
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
            Vector b = obj as Vector;
            if (b != null)
            {
                if (X == b.X && Y == b.Y && Z == b.Z)
                    return true;
            }
            return false;
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
            if ((object)a != null)
            {
                if ((object)b != null)
                    return a.Equals(b);  // a and b are not null, use a's Equals method
                return false; // a is not null, b is null, therefor not equal
            }
            else if ((object)b != null)
                return false; // a is not null, b is null, therefor not equal
            return true; // both are null, return true
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