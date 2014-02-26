using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Function
{
    [FunctionAttribute("abs")]
    public class FunctionAbs : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Abs(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("mod")]
    public class FunctionMod : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double dividend = GetDouble(shared.Cpu.PopValue());
            double divisor = GetDouble(shared.Cpu.PopValue());
            double result = dividend % divisor;
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("floor")]
    public class FunctionFloor : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Floor(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("ceiling")]
    public class FunctionCeiling : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Ceiling(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("roundnearest")]
    public class FunctionRoundNearest : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Round(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("round")]
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

    [FunctionAttribute("sqrt")]
    public class FunctionSqrt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Sqrt(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("ln")]
    public class FunctionLn : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Log(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("log10")]
    public class FunctionLog10 : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Log10(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("min")]
    public class FunctionMin : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object argument1 = shared.Cpu.PopValue();
            object argument2 = shared.Cpu.PopValue();
            
            Compilation.Calculator calculator = Compilation.Calculator.GetCalculator(argument1, argument2);
            object result = calculator.Min(argument1, argument2);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("max")]
    public class FunctionMax : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object argument1 = shared.Cpu.PopValue();
            object argument2 = shared.Cpu.PopValue();

            Compilation.Calculator calculator = Compilation.Calculator.GetCalculator(argument1, argument2);
            object result = calculator.Max(argument1, argument2);
            shared.Cpu.PushStack(result);
        }
    }
}
