﻿using System;
using kOS.Safe.Compilation;
using kOS.Safe.Function;
using kOS.Suffixed;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation;

namespace kOS.Function
{
    [Function("abs")]
    public class FunctionAbs : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Abs(argument);
            ReturnValue = result;
        }
    }

    [Function("mod")]
    public class FunctionMod : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double divisor = GetDouble(PopValueAssert(shared));
            double dividend = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = dividend % divisor;
            ReturnValue = result;
        }
    }

    [Function("floor")]
    public class FunctionFloor : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Floor(argument);
            ReturnValue = result;
        }
    }

    [Function("ceiling")]
    public class FunctionCeiling : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Ceiling(argument);
            ReturnValue = result;
        }
    }

    [Function("round")]
    public class FunctionRound : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int decimals;
            int argCount = CountRemainingArgs(shared);

            switch (argCount)
            {
                case 1:
                    decimals = 0;
                    break;
                case 2:
                    decimals = GetInt(PopValueAssert(shared));
                    break;
                default:
                    throw new KOSArgumentMismatchException(new []{1,2}, argCount);
            }

            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Round(argument, decimals);
            ReturnValue = result;
        }
    }

    [Function("sqrt")]
    public class FunctionSqrt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Sqrt(argument);
            ReturnValue = result;
        }
    }


    [Function("ln")]
    public class FunctionLn : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Log(argument);
            ReturnValue = result;
        }
    }

    [Function("log10")]
    public class FunctionLog10 : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Log10(argument);
            ReturnValue = result;
        }
    }

    [Function("min")]
    public class FunctionMin : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Structure argument1 = PopStructureAssertEncapsulated(shared);
            Structure argument2 = PopStructureAssertEncapsulated(shared);
            AssertArgBottomAndConsume(shared);
            Type scalarCompare = typeof(ScalarValue);
            Type stringCompare = typeof(StringValue);
            if (scalarCompare.IsInstanceOfType(argument1) && scalarCompare.IsInstanceOfType(argument2))
            {
                double d1 = ((ScalarValue)argument1).GetDoubleValue();
                double d2 = ((ScalarValue)argument2).GetDoubleValue();
                ReturnValue = Math.Min(d1, d2);
            }
            else if (stringCompare.IsInstanceOfType(argument1) && stringCompare.IsInstanceOfType(argument2))
            {
                string arg1 = argument1.ToString();
                string arg2 = argument2.ToString();
                int compareNum = string.Compare(arg1, arg2, StringComparison.OrdinalIgnoreCase);
                ReturnValue = (compareNum < 0) ? arg1 : arg2;
            }
            else
            {
                throw new KOSException("Argument Mismatch: the function MIN only accepts matching arguments of type Scalar or String");
            }
        }
    }

    [Function("max")]
    public class FunctionMax : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Structure argument1 = PopStructureAssertEncapsulated(shared);
            Structure argument2 = PopStructureAssertEncapsulated(shared);
            AssertArgBottomAndConsume(shared);
            Type scalarCompare = typeof(ScalarValue);
            Type stringCompare = typeof(StringValue);
            if (scalarCompare.IsInstanceOfType(argument1) && scalarCompare.IsInstanceOfType(argument2))
            {
                double d1 = ((ScalarValue)argument1).GetDoubleValue();
                double d2 = ((ScalarValue)argument2).GetDoubleValue();
                ReturnValue = Math.Max(d1, d2);
            }
            else if (stringCompare.IsInstanceOfType(argument1) && stringCompare.IsInstanceOfType(argument2))
            {
                string arg1 = argument1.ToString();
                string arg2 = argument2.ToString();
                int compareNum = string.Compare(arg1, arg2, StringComparison.OrdinalIgnoreCase);
                ReturnValue = (compareNum > 0) ? arg1 : arg2;
            }
            else
            {
                throw new KOSException("Argument Mismatch: the function MAX only accepts matching arguments of type Scalar or String");
            }
        }
    }

    [Function("random")]
    public class FunctionRandom : FunctionBase
    {
        private readonly Random random = new Random();

        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            ReturnValue = Structure.FromPrimitive(random.NextDouble());
        }
    }

    [Function("vcrs", "vectorcrossproduct")]
    public class FunctionVectorCross : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = GetVector(PopValueAssert(shared));
            var vector1 = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            if (vector1 != null && vector2 != null)
            {
                object result = new Vector(Vector3d.Cross(vector1, vector2));
                ReturnValue = result;
            }
            else
                throw new KOSException("vector cross product attempted with a non-vector value");
        }
    }

    [Function("vdot", "vectordotproduct")]
    public class FunctionVectorDot : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = GetVector(PopValueAssert(shared));
            var vector1 = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            if (vector1 != null && vector2 != null)
            {
                object result = Vector3d.Dot(vector1, vector2);
                ReturnValue = result;
            }
            else
                throw new KOSException("vector dot product attempted with a non-vector value");
        }
    }

    [Function("vxcl", "vectorexclude")]
    public class FunctionVectorExclude : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = GetVector(PopValueAssert(shared));
            var vector1 = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            if (vector1 != null && vector2 != null)
            {
                object result = new Vector(Vector3d.Exclude(vector1, vector2));
                ReturnValue = result;
            }
            else
                throw new KOSException("vector exclude attempted with a non-vector value");
        }
    }

    [Function("vang", "vectorangle")]
    public class FunctionVectorAngle : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = GetVector(PopValueAssert(shared));
            var vector1 = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            if (vector1 != null && vector2 != null)
            {
                object result = Vector3d.Angle(vector1, vector2);
                ReturnValue = result;
            }
            else
                throw new KOSException("vector angle calculation attempted with a non-vector value");
        }
    }


    [Function("char")]
    public class FunctionChar : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            string result = new string((char) argument, 1);
            ReturnValue = new StringValue(result);
        }
    }

    [Function("unchar")]
    public class FunctionUnchar : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string argument = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            char result = argument.ToCharArray()[0];
            ReturnValue = ScalarValue.Create((int)result);
        }
    }
}
