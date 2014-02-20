namespace kOS.Suffixed
{
    public class Vector : SpecialValue, IOperatable
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

        public Vector(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public object TryOperation(string op, object other, bool reverseOrder)
        {
            switch (op)
            {
                case "+":
                    if (other is Vector) return this + (Vector) other;
                    break;
                case "*":
                    if (other is Vector) return this*(Vector) other;
                    if (other is double) return this*(double) other;
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
            }

            return null;
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
                    return new Vector3d(X, Y, Z).magnitude;
                case "VEC":
                    return new Vector(x, y, z);
                case "NORMALIZED":
                    return new Vector(new Vector3d(x, y, z).normalized);
                case "SQRMAGNITUDE":
                    return new Vector3d(x, y, z).sqrMagnitude;
            }

            return base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            double dblValue;
            if (value is double)
            {
                dblValue = (double) value;
            }
            else if (!double.TryParse(value.ToString(), out dblValue))
            {
                return false;
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
            }

            return base.SetSuffix(suffixName, value);
        }

        public Vector3d ToVector3D()
        {
            return new Vector3d(X, Y, Z);
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