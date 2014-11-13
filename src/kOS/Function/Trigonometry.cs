using System;
using kOS.Safe.Function;

namespace kOS.Function
{
    [Function("sin")]
    public class FunctionSin : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double degrees = GetDouble(shared.Cpu.PopValue());
            double radians = DegreesToRadians(degrees);
            double result = Math.Sin(radians);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("cos")]
    public class FunctionCos : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double degrees = GetDouble(shared.Cpu.PopValue());
            double radians = DegreesToRadians(degrees);
            double result = Math.Cos(radians);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("tan")]
    public class FunctionTan : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double degrees = GetDouble(shared.Cpu.PopValue());
            double radians = DegreesToRadians(degrees);
            double result = Math.Tan(radians);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("arcsin")]
    public class FunctionArcSin : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = RadiansToDegrees(Math.Asin(argument));
            shared.Cpu.PushStack(result);
        }
    }

    [Function("arccos")]
    public class FunctionArcCos : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = RadiansToDegrees(Math.Acos(argument));
            shared.Cpu.PushStack(result);
        }
    }

    [Function("arctan")]
    public class FunctionArcTan : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = RadiansToDegrees(Math.Atan(argument));
            shared.Cpu.PushStack(result);
        }
    }

    [Function("arctan2")]
    public class FunctionArcTan2 : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double x = GetDouble(shared.Cpu.PopValue());
            double y = GetDouble(shared.Cpu.PopValue());
            double result = RadiansToDegrees(Math.Atan2(y, x));
            shared.Cpu.PushStack(result);
        }
    }

    [Function("anglediff")]
    public class FunctionAngleDiff : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double ang2 = GetDouble(shared.Cpu.PopValue());
            double ang1 = GetDouble(shared.Cpu.PopValue());
            double result = kOS.Utilities.Utils.DegreeFix( ang2 - ang1, -180 );
            shared.Cpu.PushStack(result);
        }
    }
}
