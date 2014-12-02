using System;
using kOS.Safe.Compilation;
using kOS.Safe.Function;
using kOS.Suffixed;

namespace kOS.Function
{
    [Function("abs")]
    public class FunctionAbs : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Abs(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("mod")]
    public class FunctionMod : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double divisor = GetDouble(shared.Cpu.PopValue());
            double dividend = GetDouble(shared.Cpu.PopValue());
            double result = dividend % divisor;
            shared.Cpu.PushStack(result);
        }
    }

    [Function("floor")]
    public class FunctionFloor : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Floor(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("ceiling")]
    public class FunctionCeiling : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Ceiling(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("roundnearest")]
    public class FunctionRoundNearest : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Round(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("round")]
    public class FunctionRound : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int decimals = GetInt(shared.Cpu.PopValue());
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Round(argument, decimals);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("sqrt")]
    public class FunctionSqrt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Sqrt(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("ln")]
    public class FunctionLn : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Log(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("log10")]
    public class FunctionLog10 : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Log10(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("min")]
    public class FunctionMin : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object argument1 = shared.Cpu.PopValue();
            object argument2 = shared.Cpu.PopValue();
            
            Calculator calculator = Calculator.GetCalculator(argument1, argument2);
            object result = calculator.Min(argument1, argument2);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("max")]
    public class FunctionMax : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object argument1 = shared.Cpu.PopValue();
            object argument2 = shared.Cpu.PopValue();

            Calculator calculator = Calculator.GetCalculator(argument1, argument2);
            object result = calculator.Max(argument1, argument2);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("random")]
    public class FunctionRandom : FunctionBase
    {
        private readonly Random random = new Random();

        public override void Execute(SharedObjects shared)
        {
            shared.Cpu.PushStack(random.NextDouble());
        }
    }

    [Function("vcrs", "vectorcrossproduct")]
    public class FunctionVectorCross : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = shared.Cpu.PopValue() as Vector;
            var vector1 = shared.Cpu.PopValue() as Vector;

            if (vector1 != null && vector2 != null)
            {
                object result = new Vector(Vector3d.Cross(vector1.ToVector3D(), vector2.ToVector3D()));
                shared.Cpu.PushStack(result);
            }
        }
    }

    [Function("vdot", "vectordotproduct")]
    public class FunctionVectorDot : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = shared.Cpu.PopValue() as Vector;
            var vector1 = shared.Cpu.PopValue() as Vector;

            if (vector1 != null && vector2 != null)
            {
                object result = Vector3d.Dot(vector1.ToVector3D(), vector2.ToVector3D());
                shared.Cpu.PushStack(result);
            }
        }
    }

    [Function("vxcl", "vectorexclude")]
    public class FunctionVectorExclude : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = shared.Cpu.PopValue() as Vector;
            var vector1 = shared.Cpu.PopValue() as Vector;

            if (vector1 != null && vector2 != null)
            {
                object result = new Vector(Vector3d.Exclude(vector1.ToVector3D(), vector2.ToVector3D()));
                shared.Cpu.PushStack(result);
            }
        }
    }

    [Function("vang", "vectorangle")]
    public class FunctionVectorAngle : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = shared.Cpu.PopValue() as Vector;
            var vector1 = shared.Cpu.PopValue() as Vector;

            if (vector1 != null && vector2 != null)
            {
                object result = Vector3d.Angle(vector1.ToVector3D(), vector2.ToVector3D());
                shared.Cpu.PushStack(result);
            }
        }
    }
}
