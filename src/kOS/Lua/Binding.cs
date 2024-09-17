using KeraLua;
using kOS.Binding;
using kOS.Safe.Binding;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace kOS.Lua
{
    internal class Binding
    {
        private static readonly Dictionary<IntPtr, BindingData> bindings = new Dictionary<IntPtr, BindingData>();

        private class BindingData
        {
            public readonly Dictionary<string, BoundVariable> variables;
            public readonly Dictionary<IntPtr, object> objects;
            public readonly Dictionary<object, IntPtr> userdataPtrs;
            public BindingData(Dictionary<string, BoundVariable> boundVariables)
            {
                variables = boundVariables;
                objects = new Dictionary<IntPtr, object>();
                userdataPtrs = new Dictionary<object, IntPtr>();
            }
        }

        public static void BindToState(KeraLua.Lua state, SharedObjects shared)
        {
            state = state.MainThread;
            bindings[state.Handle] = new BindingData((shared.BindingMgr as BindingManager).RawVariables);
            state.PushCFunction(EnvIndex);
            state.SetGlobal("envIndex");
            state.PushCFunction(EnvNewIndex);
            state.SetGlobal("envNewIndex");
            int oldTop = state.GetTop();
            state.DoString(@"local mt = { __index = envIndex, __newindex = envNewIndex }; setmetatable(_ENV, mt)");
            CreateUserdataMapTable(state);
            if (state.NewMetaTable("Structure"))
            {
                state.PushString("__index");
                state.PushCFunction(StructureIndex);
                state.RawSet(-3);
                state.PushString("__newindex");
                state.PushCFunction(StructureNewIndex);
                state.RawSet(-3);
                state.PushString("__call");
                state.PushCFunction(StructureCall);
                state.RawSet(-3);
                state.PushString("__gc");
                state.PushCFunction(CollectObject);
                state.RawSet(-3);
                state.PushString("__tostring");
                state.PushCFunction(ObjectToString);
                state.RawSet(-3);
            }
            state.SetTop(oldTop);
        }
        private static void CreateUserdataMapTable(KeraLua.Lua state)
        {
            state.PushString("userdataAdressToUserdata");
            state.NewTable();
            state.NewTable();
            state.PushString("__mode");
            state.PushString("v");
            state.SetTable(-3);
            state.SetMetaTable(-2);
            state.SetTable((int)LuaRegistry.Index);
        }

        private static int CollectObject(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            var userdataAdress = state.ToUserData(1);
            var obj = binding.objects[userdataAdress];
            binding.userdataPtrs.Remove(obj);
            binding.objects.Remove(userdataAdress);
            return 0;
        }

        private static int ObjectToString(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.PushString(bindings[state.MainThread.Handle].objects[state.ToUserData(1)].ToString());
            return 1;
        }

        private static int EnvIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var index = state.ToString(2);
            var binding = bindings[state.MainThread.Handle];
            if (!binding.variables.TryGetValue(index, out var boundVar))
            {
                state.PushNil();
                return 1;
            }
            try { return PushLuaType(state, Structure.ToPrimitive(boundVar.Value), binding); }
            catch (Exception e) { return state.Error(e.Message); }
        }

        private static int EnvNewIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            var index = state.ToString(2);
            if (binding.variables.TryGetValue(index, out var boundVar) && boundVar.Set != null)
            {
                try { boundVar.Value = ToCSharpObject(state, 3, binding); }
                catch (Exception e) { return state.Error(e.Message); }
            }
            else
            {
                state.RawSet(1);
            }
            return 0;
        }

        private static int StructureIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            var structure = (Structure)binding.objects[state.ToUserData(1)];
            object pushValue = null;
            if (structure is IIndexable && state.TypeName(2) == "number")
            {
                int intIndex = (int)state.ToInteger(2);
                try { pushValue = Structure.ToPrimitive((structure as IIndexable).GetIndex(intIndex)); }
                catch (Exception e) { return state.Error(e.Message); }
                return PushLuaType(state, pushValue, binding);
            }
            var index = state.ToString(2);
            if (structure.HasSuffix(index))
            {
                var result = structure.GetSuffix(index);
                if (result.HasValue)
                {
                    pushValue = Structure.ToPrimitive(result.Value);
                } else
                {
                    var delegateResult = result as DelegateSuffixResult;
                    if (delegateResult.RawDelInfo.Parameters.Length == 0)
                    {
                        try { delegateResult.RawSetValue(Structure.FromPrimitiveWithAssert(delegateResult.RawCall(null))); }
                        catch (Exception e) { return state.Error(e.Message); }
                        pushValue = Structure.ToPrimitive(delegateResult.Value); // TODO: maybe skip the conversions?
                    } else
                    {
                        pushValue = delegateResult;
                    }
                }
            }
            return PushLuaType(state, pushValue, binding);
        }

        private static int StructureNewIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            var structure = (Structure)binding.objects[state.ToUserData(1)];
            object value = ToCSharpObject(state, 3, binding);
            if (value != null)
            {
                if (structure is IIndexable && state.TypeName(2) == "number")
                {
                    int intIndex = (int)state.ToInteger(2);
                    try { (structure as IIndexable).SetIndex(intIndex, Structure.FromPrimitive(value) as Structure); }
                    catch (Exception e) { return state.Error(e.Message); }
                    return 0;
                }
                var index = state.ToString(2);
                try { structure.SetSuffix(index, Structure.FromPrimitive(value)); }
                catch (Exception e) { return state.Error(e.Message); }
            }
            return 0;
        }

        private static int StructureCall(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            var structure = binding.objects[state.ToUserData(1)];
            if (!(structure is DelegateSuffixResult delegateResult)) {
                return state.Error(string.Format("attempt to call a structure {0} value", structure.GetType().Name));
            }
            // the next entire bit is copied from DelegateSuffixResult.Invoke method with a few changes
            // that make it access arguments from the lua stack instead of kerboscript stack
            try
            {
                var delInfo = delegateResult.RawDelInfo;
                var args = new List<object>();
                var paramArrayArgs = new List<Structure>();

                // Will be true iff the lastmost parameter of the delegate is using the C# 'param' keyword and thus
                // expects the remainder of the arguments marshalled together into one array object.
                bool isParamArrayArg = false;

                //CpuUtility.ReverseStackArgs(cpu, false);
                int popIndex = 2;
                for (int i = 0; i < delInfo.Parameters.Length; ++i)
                {
                    DelegateParameter paramInfo = delInfo.Parameters[i];

                    //object arg = cpu.PopValueArgument();
                    object arg = popIndex <= state.GetTop()? ToCSharpObject(state, popIndex++, binding) : new KOSArgMarkerType();
                    Type argType = arg.GetType();
                    isParamArrayArg = i == delInfo.Parameters.Length - 1 && delInfo.Parameters[i].IsParams;

                    if (arg != null && arg.GetType() == CpuUtility.ArgMarkerType)
                    {
                        if (isParamArrayArg)
                            break; // with param arguments, you want to consume everything to the arg bottom - it's normal.
                        else
                            throw new KOSArgumentMismatchException(delInfo.Parameters.Length, delInfo.Parameters.Length - (i + 1));
                    }

                    // Either the expected type of this one parameter, or if it's a 'param' array as the last arg, then
                    // the expected type of that array's elements:
                    Type paramType = (paramInfo.IsParams ? paramInfo.ParameterType.GetElementType() : paramInfo.ParameterType);

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
                            throw new Exception(string.Format("Argument {0}({1}) to method {2} should be {3} instead of {4}.", (delInfo.Parameters.Length - i), arg, delInfo.Name, paramType.Name, argType));
                        }
                    }

                    if (isParamArrayArg)
                    {
                        paramArrayArgs.Add(Structure.FromPrimitiveWithAssert(arg));
                        --i; // keep hitting the last item in the param list again and again until a forced break because of arg bottom marker.
                    }
                    else
                    {
                        args.Add(Structure.FromPrimitiveWithAssert(arg));
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
                    //while (cpu.GetArgumentStackSize() > 0 && !foundArgMarker)
                    while (state.GetTop()-popIndex >= 0 && !foundArgMarker)
                    {
                        //object marker = cpu.PopValueArgument();
                        object marker = popIndex <= state.GetTop()? ToCSharpObject(state, popIndex++, binding) : new KOSArgMarkerType();
                        if (marker != null && marker.GetType() == CpuUtility.ArgMarkerType)
                            foundArgMarker = true;
                        else
                            ++numExtraArgs;
                    }
                    if (numExtraArgs > 0)
                        throw new KOSArgumentMismatchException(delInfo.Parameters.Length, delInfo.Parameters.Length + numExtraArgs);
                }

                // Delegate.DynamicInvoke expects a null, rather than an array of zero length, when
                // there are no arguments to pass:
                object[] argArray = (args.Count > 0) ? args.ToArray() : null;

                //object val = call(argArray);
                object val = delegateResult.RawCall(argArray);
                if (delInfo.ReturnType == typeof(void))
                {
                    //value = ScalarValue.Create(0);
                    delegateResult.RawSetValue(ScalarValue.Create(0));
                }
                else
                {
                    delegateResult.RawSetValue(Structure.FromPrimitiveWithAssert(val));
                }
                return PushLuaType(state, Structure.ToPrimitive(delegateResult.Value), binding);
            } catch (Exception e)
            {
                return state.Error(e.Message);
            }
        }

        private static int PushLuaType(KeraLua.Lua state, object obj, BindingData binding)
        {
            if (obj == null)
            {
                state.PushNil();
            }
            else if (obj is double db)
            {
                state.PushNumber(db);
            }
            else if (obj is int i)
            {
                state.PushInteger(i);
            }
            else if (obj is string str)
            {
                state.PushString(str);
            }
            else if (obj is bool b)
            {
                state.PushBoolean(b);
            }
            else
            {
                PushObject(state, obj, binding);
            }
            return 1;
        }

        private static object ToCSharpObject(KeraLua.Lua state, int index, BindingData binding)
        {
            switch (state.TypeName(index))
            {
                case "number":
                    return state.ToNumber(index);
                case "string":
                    return state.ToString(index);
                case "boolean":
                    return state.ToBoolean(index);
                case "userdata":
                    return binding.objects.GetValueOrDefault(state.ToUserData(index), null);
                default:
                    return null;
            }
        }

        private static int PushObject(KeraLua.Lua state, object obj, BindingData binding)
        {
            state.GetMetaTable("userdataAdressToUserdata");
            if (binding.userdataPtrs.TryGetValue(obj, out IntPtr userdataAdress)) // Object already in the list of object userdata? Push the userdata
            {
                // Note: starting with lua5.1 the garbage collector may remove weak reference items (such as our userdataAdressToUserdata values) when the initial GC sweep 
                // occurs, but the actual call of the __gc finalizer for that object may not happen until a little while later.  During that window we might call
                // this routine and find the element missing from userdataAdressToUserdata, but CollectObject() has not yet been called.  In that case, we go ahead and
                // do the same thing CollectObject() does and remove from out object maps
                state.PushLightUserData(userdataAdress);
                if (state.RawGet(-2) != LuaType.Nil) return 1; // if found the objects userdata return it

                state.Remove(-1);	// remove the nil value

                binding.objects.Remove(userdataAdress); // Remove from both our maps and fall out to create a new userdata
                binding.userdataPtrs.Remove(obj);
            }

            userdataAdress = state.NewUserData(0);
            state.GetMetaTable("Structure");
            state.SetMetaTable(-2);
            state.PushLightUserData(userdataAdress);
            state.PushCopy(-2);
            state.RawSet(-4); // add userdata on top of the stack to the userdataAdressToUserdata metatable at userdataAdress key

            binding.objects[userdataAdress] = obj;
            binding.userdataPtrs[obj] = userdataAdress;

            return 1;
        }
    }
}
