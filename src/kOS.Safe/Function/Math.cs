using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Function
{
    [Function("abs")]
    public class FunctionAbs : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Abs(argument);
            ReturnValue = result;
        }
    }

    [Function("mod")]
    public class FunctionMod : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double divisor = GetDouble(PopValueAssert(shared));
            double dividend = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = dividend % divisor;
            ReturnValue = result;
        }
    }

    [Function("floor")]
    public class FunctionFloor : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Floor(argument);
            ReturnValue = result;
        }
    }

    [Function("ceiling")]
    public class FunctionCeiling : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Ceiling(argument);
            ReturnValue = result;
        }
    }

    [Function("round")]
    public class FunctionRound : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
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
    public class FunctionSqrt : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Sqrt(argument);
            ReturnValue = result;
        }
    }


    [Function("ln")]
    public class FunctionLn : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Log(argument);
            ReturnValue = result;
        }
    }

    [Function("log10")]
    public class FunctionLog10 : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Log10(argument);
            ReturnValue = result;
        }
    }

    [Function("min")]
    public class FunctionMin : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
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
    public class FunctionMax : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
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
    public class FunctionRandom : SafeFunctionBase
    {
        private readonly Random random = new Random();

        public override void Execute(SafeSharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            ReturnValue = Structure.FromPrimitive(random.NextDouble());
        }
    }

    [Function("char")]
    public class FunctionChar : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            string result = new string((char) argument, 1);
            ReturnValue = new StringValue(result);
        }
    }

    [Function("unchar")]
    public class FunctionUnchar : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            string argument = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            char result = argument.ToCharArray()[0];
            ReturnValue = ScalarValue.Create((int)result);
        }
    }
}
