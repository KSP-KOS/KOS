using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using System;
using System.Linq;

namespace kOS.Safe.Function
{
    public abstract class SafeFunctionBase
    {
        public string FunctionName;

        /// <summary>
        /// ALL FUNCTIONS in kOS will always have exactly one return value.  We have no
        /// "void" functions, to keep the execution logic consistent and simple.  Therefore
        /// even a function that never explicitly picks a return value still gets one by
        /// default anyway that will be pushed on the stack after its Execute() is called.
        /// That is what this property is for.  If you wish your built-in function to put a
        /// specific return value on the stack set its ReturnValue property in its Execute()
        /// method, and the pushing of it onto the stack will be handled for you.  Don't push
        /// it onto the stack manually, as that would result in a double-push.
        /// If you decline to set ReturnValue, it will get a default value of zero anyway.
        /// </summary>
        public object ReturnValue
        {
            get
            {
                // Convert from primitive types to encapsulated types so that functions
                // do not explicitly need to return the encapsulated type.
                return Structure.FromPrimitive(internalReturn);
            }
            set
            {
                internalReturn = value;
            }
        }

        private object internalReturn = 0; // really should be 'null', but kerboscript can't deal with that.

        /// <summary>
        /// In the *extremely* rare case where a built-in function is NOT supposed to
        /// push a value onto the stack as its return value, and these are very uncommon,
        /// it should set this to false.  By default it will be true.
        /// Cases where this might occur are cases where the function will artificially mangle
        /// the execution order by jumping the instruction pointer to somewhere else, in a way
        /// other than with an actual proper jump or call opcode.  Right now the function run() is the only
        /// one that does this and needs the exception.<br/>
        /// Think very carefully before setting this to false in your function, and only do so if
        /// you really know what you're doing and have fully understood the system.<br/>
        /// If you set this to false, it means you are manually manipulating the stack to give it your own
        /// wierd return behavior.
        /// </summary>
        public bool UsesAutoReturn { get; set; }

        protected SafeFunctionBase()
        {
            ReturnValue = 0; // default return value ALL built-ins will have if they don't set it.
            UsesAutoReturn = true;
        }

        public abstract void Execute(SafeSharedObjects shared);

        protected double GetDouble(object argument)
        {
            try
            {
                return Convert.ToDouble(argument);
            }
            catch (Exception)
            {
                throw new KOSCastException(argument.GetType(), typeof(ScalarValue));
            }
        }

        protected int GetInt(object argument)
        {
            try
            {
                return Convert.ToInt32(argument);
            }
            catch (Exception)
            {
                throw new KOSCastException(argument.GetType(), typeof(ScalarValue));
            }
        }

        protected double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        protected double RadiansToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        /// <summary>
        /// A utility function that a function's Execute() must use after it has popped all the
        /// arguments it was expecting from the stack.  It will assert that all the arguments
        /// have been consumed exactly, and the next item on the stack is the arg bottom mark.
        /// It will consume the arg bottom mark as well.
        /// <br/>
        /// If the assert fails, an exception is thrown.
        /// </summary>
        /// <param name="shared"></param>
        protected void AssertArgBottomAndConsume(SafeSharedObjects shared)
        {
            object shouldBeBottom = shared.Cpu.PopArgumentStack();
            if (shouldBeBottom != null && shouldBeBottom.GetType() == OpcodeCall.ArgMarkerType)
                return; // Assert passed.

            throw new KOSArgumentMismatchException("Too many arguments were passed to " + FunctionName);
        }

        /// <summary>
        /// A utility function that a function's Execute() may use if it wishes to, to get a count of
        /// how many args passed to it that it has not yet consumed still remain on the stack.
        /// </summary>
        /// <param name="shared"></param>
        /// <returns>Number of args as yet unpopped.  returns zero if there are no args, or -1 if there's a bug and the argstart marker is missing.</returns>
        protected int CountRemainingArgs(SafeSharedObjects shared)
        {
            int depth = 0;
            bool found = false;
            bool stillInStack = true;
            while (stillInStack && !found)
            {
                object peekItem = shared.Cpu.PeekRawArgument(depth, out stillInStack);
                if (stillInStack && peekItem != null && peekItem.GetType() == OpcodeCall.ArgMarkerType)
                    found = true;
                else
                    ++depth;
            }
            if (found)
                return depth;
            else
                return -1;
        }

        /// <summary>
        /// A utility function that a function's Execute() should use in place of cpu.PopValue(),
        /// because it will assert that the value being popped is NOT an ARG_MARKER_STRING, and if it
        /// is, it will throw the appropriate error.
        /// </summary>
        /// <returns></returns>
        protected object PopValueAssert(SafeSharedObjects shared, bool barewordOkay = false)
        {
            object returnValue = shared.Cpu.PopValueArgument(barewordOkay);
            if (returnValue != null && returnValue.GetType() == OpcodeCall.ArgMarkerType)
                throw new KOSArgumentMismatchException("Too few arguments were passed to " + FunctionName);
            return returnValue;
        }

        /// <summary>
        /// A utility function that a function's Execute() should use in place of cpu.PopArgumentStack(),
        /// because it will assert that the value being popped is NOT an ARG_MARKER_STRING, and if it
        /// is, it will throw the appropriate error.
        /// </summary>
        /// <returns></returns>
        protected object PopStackAssert(SafeSharedObjects shared)
        {
            object returnValue = shared.Cpu.PopArgumentStack();
            if (returnValue != null && returnValue.GetType() == OpcodeCall.ArgMarkerType)
                throw new KOSArgumentMismatchException("Too few arguments were passed to " + FunctionName);
            return returnValue;
        }

        /// <summary>
        /// Identical to PopValueAssert, but with the additional step of coercing the result
        /// into a Structure to be sure, so it won't return primitives.
        /// </summary>
        /// <returns>value after coercion into a kOS Structure</returns>
        protected Structure PopStructureAssertEncapsulated(SafeSharedObjects shared, bool barewordOkay = false)
        {
            object returnValue = PopValueAssert(shared, barewordOkay);
            return Structure.FromPrimitiveWithAssert(returnValue);
        }

        protected string GetFuncName() => FunctionName;
    }
}