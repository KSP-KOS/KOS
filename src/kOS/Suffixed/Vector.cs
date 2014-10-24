﻿using System;
using kOS.Safe.Encapsulation;
using UnityEngine;

namespace kOS.Suffixed
{
    public class Vector : Structure
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector(Vector3d init)
        {
            X = init.x;
            Y = init.y;
            Z = init.z;
        }

        public Vector(Vector3 init) // Vector3 is the single-precision version of Vector3d.
        {
            X = init.x;
            Y = init.y;
            Z = init.z;
        }

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override object TryOperation(string op, object other, bool reverseOrder)
        {
            other = ConvertToDoubleIfNeeded(other);

            switch (op)
            {
                case "*":
                    if (other is Vector) return this*(Vector) other;
                    if (other is double) return this*(double) other;
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
                    if (other is Vector) return this + (Vector) other;
                    break;
                case "-":
                    if (!reverseOrder)
                    {
                        if (other is Vector) return this - (Vector) other;
                    }
                    else
                    {
                        if (other is Vector) return (Vector) other - this;
                    }
                    break;
                    
                default:
                    throw new NotImplementedException(string.Format(
                        "Cannot perform operation: {0} {1} {2}", this, op, other ) );
            }

            return null;
        }
        
        public double Magnitude()
        {
            return new Vector3d(X,Y,Z).magnitude;
        }
        
        public Vector Normalized()
        {
            return new Vector( new Vector3d(X,Y,Z).normalized );
        }

        public Direction ToDirection()
        {
            return new Direction(ToVector3D(), false);
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "X":
                    return X;
                case "Y":
                    return Y;
                case "Z":
                    return Z;
                case "MAG":
                    return Magnitude();
                case "VEC":
                    return new Vector(X, Y, Z);
                case "NORMALIZED":
                    return Normalized();
                case "SQRMAGNITUDE":
                    return new Vector3d(X, Y, Z).sqrMagnitude;
                case "DIRECTION":
                    return ToDirection();
            }

            return base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            double dblValue;
            Direction dirValue = null;
            bool isDouble = false;
            bool isDirection = false;

            if (value is double)
            {
                dblValue = (double)value;
                isDouble = true;
            }
            else if (double.TryParse(value.ToString(), out dblValue))
            {
                isDouble = true;                
            }
            else if (value is Direction)
            {
                dirValue = (Direction)value;
                isDirection = true;
            }

            // Type check (this is the sort of thing that it would be good
            // to do automatically with a standard suffix handling system):
            bool typeMismatch = false;
            switch (suffixName)
            {
                case "X":
                case "Y":
                case "Z":
                case "MAG":
                    if (!isDouble)
                        typeMismatch = true;
                    break;
                case "DIRECTION":
                    if (!isDirection)
                        typeMismatch = true;
                    break;
            }
            if (typeMismatch)
            {
                throw new InvalidCastException("Can't set a vector's :" + suffixName + " to a " + value.GetType().Name);
            }

            switch (suffixName)
            {
                case "X":
                    X = dblValue;
                    return true;                        
                case "Y":
                    Y = dblValue;
                    return true;
                case "Z":
                    Z = dblValue;
                    return true;
                case "MAG":
                    double oldMag = new Vector3d(X, Y, Z).magnitude;

                    if (oldMag == 0) return true; // Avoid division by zero

                    X = X/oldMag*dblValue;
                    Y = Y/oldMag*dblValue;
                    Z = Z/oldMag*dblValue;
                    return true;
                case "DIRECTION":
                    Vector3d newVal = dirValue.Rotation * Vector3d.forward;
                    X = newVal.x;
                    Y = newVal.y;
                    Z = newVal.z;
                    return true;
            }

            return base.SetSuffix(suffixName, value);
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
            return new Vector(a.X*b, a.Y*b, a.Z*b);
        }

        public static Vector operator *(Vector a, double b)
        {
            return new Vector(a.X*b, a.Y*b, a.Z*b);
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.ToVector3D() + b.ToVector3D());
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.ToVector3D() - b.ToVector3D());
        }
    }
}
