using System;
using System.Collections.Generic;
using System.Reflection;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using kOS.Safe.Utilities;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class DelegateSuffixResult : ISuffixResult 
    {
        private readonly Delegate del;
        private Structure value;

        public Delegate Del
        {
            get { return del; }
        }

        public Structure Value
        {
            get { return value; }
        }

        public DelegateSuffixResult(Delegate del)
        {
            this.del = del;
        }

        public bool HasValue
        {
            get { return value != null; }
        }

        public void Invoke(ICpu cpu)
        {
            MethodInfo methInfo = del.Method;
            ParameterInfo[] paramArray = methInfo.GetParameters();
            var args = new List<object>();
            var paramArrayArgs = new List<object>();

            // Will be true iff the lastmost parameter of the delegate is using the C# 'param' keyword and thus
            // expects the remainder of the arguments marshalled together into one array object.
            bool isParamArrayArg = false;

            CpuUtility.ReverseStackArgs(cpu, false);
            for (int i = 0; i < paramArray.Length; ++i)
            {
                object arg = cpu.PopValue();
                Type argType = arg.GetType();
                ParameterInfo paramInfo = paramArray[i];

                // If this is the lastmost parameter then it might be a 'param' array which expects all the rest of
                // the arguments to be collected together into one single array parameter when invoking the method:
                isParamArrayArg = (i == paramArray.Length - 1 && Attribute.IsDefined(paramInfo, typeof(ParamArrayAttribute)));

                if (arg != null && arg.GetType() == CpuUtility.ArgMarkerType)
                {
                    if (isParamArrayArg)
                        break; // with param arguments, you want to consume everything to the arg bottom - it's normal.
                    else
                        throw new KOSArgumentMismatchException(paramArray.Length, paramArray.Length - (i + 1));
                }

                // Either the expected type of this one parameter, or if it's a 'param' array as the last arg, then
                // the expected type of that array's elements:
                Type paramType = (isParamArrayArg ? paramInfo.ParameterType.GetElementType() : paramInfo.ParameterType);

                // Parameter type-safe checking:
                bool inheritable = paramType.IsAssignableFrom(argType);
                if (!inheritable)
                {
                    bool castError = false;
                    // If it's not directly assignable to the expected type, maybe it's "castable" to it:
                    try
                    {
                        arg = Convert.ChangeType(arg, Type.GetTypeCode(paramType));
                    }
                    catch (InvalidCastException)
                    {
                        throw new KOSCastException(argType, paramType);
                    }
                    catch (FormatException)
                    {
                        castError = true;
                    }
                    if (castError)
                    {
                        throw new Exception(string.Format("Argument {0}({1}) to method {2} should be {3} instead of {4}.", (paramArray.Length - i), arg, methInfo.Name, paramType.Name, argType));
                    }
                }

                if (isParamArrayArg)
                {
                    paramArrayArgs.Add(arg);
                    --i; // keep hitting the last item in the param list again and again until a forced break because of arg bottom marker.
                }
                else
                {
                    args.Add(arg);
                }
            }
            if (isParamArrayArg)
            {
                // collect the param array args that were at the end into the one single
                // array item that will be sent to the method when invoked:
                args.Add(paramArrayArgs.ToArray());
            }
            // Consume the bottom marker under the args, which had better be
            // immediately under the args we just popped, or the count was off.
            if (!isParamArrayArg) // A param array arg will have already consumed the arg bottom mark.
            {
                bool foundArgMarker = false;
                int numExtraArgs = 0;
                while (cpu.GetStackSize() > 0 && !foundArgMarker)
                {
                    object marker = cpu.PopValue();
                    if (marker != null && marker.GetType() == CpuUtility.ArgMarkerType)
                        foundArgMarker = true;
                    else
                        ++numExtraArgs;
                }
                if (numExtraArgs > 0)
                    throw new KOSArgumentMismatchException(paramArray.Length, paramArray.Length + numExtraArgs);
            }

            // Dialog.DynamicInvoke expects a null, rather than an array of zero length, when
            // there are no arguments to pass:
            object[] argArray = (args.Count > 0) ? args.ToArray() : null;

            try
            {
                // I could find no documentation on what DynamicInvoke returns when the delegate
                // is a function returning void.  Does it return a null?  I don't know.  So to avoid the
                // problem, I split this into these two cases:
                if (methInfo.ReturnType == typeof(void))
                {
                    del.DynamicInvoke(argArray);
                    value = null; // By adding this we can unconditionally assume all functions
                                 // have a return value to be used or popped away, even if "void".
                }
                // Convert a primitive return type to a structure.  This is done in the opcode, since
                // the opcode calls the deligate directly and cannot be (quickly) intercepted
                value = Structure.FromPrimitiveWithAssert(del.DynamicInvoke(argArray));
            }
            catch (TargetInvocationException e)
            {
                // Annoyingly, calling DynamicInvoke on a delegate wraps any exceptions the delegate throws inside
                // this TargetInvocationException, which hides them from the kOS user unless we do this:
                if (e.InnerException != null)
                    throw e.InnerException;
                throw;
            }
        }
        
        // Not something the user should ever see, but still useful for our debugging when we dump the stack:
        public override string ToString()
        {
            return string.Format("[DelegateSuffixResult Del={0}, Value={1}]", del, (HasValue ? value.ToString() : "<null>") );
        }
 
    }

}