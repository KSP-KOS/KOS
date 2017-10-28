using System;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Function
{
    [Function("list")]
    public class FunctionList : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            Structure[] argArray = new Structure[CountRemainingArgs(shared)];
            for (int i = argArray.Length - 1 ; i >= 0 ; --i)
                argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
            AssertArgBottomAndConsume(shared);
            var listValue = new ListValue(argArray.ToList());
            ReturnValue = listValue;
        }
    }

    [Function("queue")]
    public class FunctionQueue : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            Structure[] argArray = new Structure[CountRemainingArgs(shared)];
            for (int i = argArray.Length - 1 ; i >= 0 ; --i)
                argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
            AssertArgBottomAndConsume(shared);
            var queueValue = new QueueValue(argArray.ToList());
            ReturnValue = queueValue;
        }
    }

    [Function("stack")]
    public class FunctionStack : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            Structure[] argArray = new Structure[CountRemainingArgs(shared)];
            for (int i = argArray.Length - 1 ; i >= 0 ; --i)
                argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
            AssertArgBottomAndConsume(shared);
            var stackValue = new StackValue(argArray.ToList());
            ReturnValue = stackValue;
        }
    }

    [Function("uniqueset")]
    public class FunctionSet : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            Structure[] argArray = new Structure[CountRemainingArgs(shared)];
            for (int i = argArray.Length - 1 ; i >= 0 ; --i)
                argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
            AssertArgBottomAndConsume(shared);
            var setValue = new UniqueSetValue(argArray.ToList());
            ReturnValue = setValue;
        }
    }

    [Function("range")]
    public class FunctionRange : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            // Default values for parameters
            int from = RangeValue.DEFAULT_START;
            int to = RangeValue.DEFAULT_STOP;
            int step = RangeValue.DEFAULT_STEP;

            int argCount = CountRemainingArgs(shared);
            // assign parameter values from the stack, pop them in reverse order
            switch (argCount)
            {
            case 1:
                to = GetInt(PopStructureAssertEncapsulated(shared));
                break;
            case 2:
                to = GetInt(PopStructureAssertEncapsulated(shared));
                from = GetInt(PopStructureAssertEncapsulated(shared));
                break;
            case 3:
                step = GetInt(PopStructureAssertEncapsulated(shared));
                to = GetInt(PopStructureAssertEncapsulated(shared));
                from = GetInt(PopStructureAssertEncapsulated(shared));
                break;
            default:
                throw new KOSArgumentMismatchException(new int[] { 1, 2, 3 }, argCount, "Thrown from function RANGE()");
            }
            AssertArgBottomAndConsume(shared);

            ReturnValue = new RangeValue(from, to, step);
        }
    }

    [Function("lex", "lexicon")]
    public class FunctionLexicon : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {

            Structure[] argArray = new Structure[CountRemainingArgs(shared)];
            for (int i = argArray.Length - 1 ; i >= 0 ; --i)
                argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
            AssertArgBottomAndConsume(shared);
            var lexicon = new Lexicon(argArray.ToList());
            ReturnValue = lexicon;
        }
    }

    [Function("constant")]
    public class FunctionConstant : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            AssertArgBottomAndConsume(shared); // no args
            ReturnValue = new ConstantValue();
        }
    }
}
