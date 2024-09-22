using KeraLua;
using kOS.Binding;
using kOS.Safe;
using kOS.Safe.Binding;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using kOS.Safe.Function;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Smooth.Collections;
using Debug = UnityEngine.Debug;

namespace kOS.Lua
{
    internal static class Binding
    {
        private static readonly Dictionary<IntPtr, BindingData> bindings = new Dictionary<IntPtr, BindingData>();

        // the CSharp object to userdata binding model was adapted from NLua model
        // with some simplifications and changes to make it work on Structures
        private class BindingData
        {
            public readonly Dictionary<string, BoundVariable> variables;
            public readonly Dictionary<IntPtr, object> objects;
            public readonly Dictionary<object, IntPtr> userdataPtrs;
            public readonly Dictionary<string, SafeFunctionBase> functions;
            public readonly SafeSharedObjects Shared;
            
            public BindingData(SafeSharedObjects shared, Dictionary<string, BoundVariable> boundVariables, Dictionary<string, SafeFunctionBase> functions)
            {
                Shared = shared;
                variables = boundVariables;
                objects = new Dictionary<IntPtr, object>();
                userdataPtrs = new Dictionary<object, IntPtr>();
                this.functions = functions;
            }
        }

        public static void DisposeStateBinding(KeraLua.Lua state) => bindings.Remove(state.MainThread.Handle);

        public static void BindToState(KeraLua.Lua state, SharedObjects shared)
        {
            state = state.MainThread;
            bindings[state.Handle] = new BindingData(
                shared,
                (shared.BindingMgr as BindingManager).RawVariables,
                (shared.FunctionManager as FunctionManager).RawFunctions
            );
            state.PushCFunction(EnvIndex);
            state.SetGlobal("envIndex");
            state.PushCFunction(EnvNewIndex);
            state.SetGlobal("envNewIndex");
            state.GetGlobal("type");
            state.SetGlobal("_type");
            state.PushCFunction(UserdataType);
            state.SetGlobal("type");
            state.GetGlobal("print");
            state.SetGlobal("_print");
            state.PushCFunction(KosPrint);
            state.SetGlobal("print");
            int oldTop = state.GetTop();
            state.DoString(@"local mt = { __index = envIndex, __newindex = envNewIndex }; setmetatable(_ENV, mt)");
            CreateUserdataMapTable(state);
            if (state.NewMetaTable("Structure"))
            {
                state.PushString("__type");
                state.PushString("Structure");
                state.RawSet(-3);
                state.PushString("__index");
                state.PushCFunction(StructureIndex);
                state.RawSet(-3);
                state.PushString("__newindex");
                state.PushCFunction(StructureNewIndex);
                state.RawSet(-3);
                state.PushString("__pairs");
                state.PushCFunction(StructurePairs);
                state.RawSet(-3);
                state.PushString("__gc");
                state.PushCFunction(CollectObject);
                state.RawSet(-3);
                state.PushString("__tostring");
                state.PushCFunction(StructureToString);
                state.RawSet(-3);
            }
            if (state.NewMetaTable("KerboscriptFunction"))
            {
                state.PushString("__type");
                state.PushString("KerboscriptFunction");
                state.RawSet(-3);
                state.PushString("__call");
                state.PushCFunction(KSFunctionCall);
                state.RawSet(-3);
                state.PushString("__gc");
                state.PushCFunction(CollectObject);
                state.RawSet(-3);
                state.PushString("__tostring");
                state.PushCFunction(KSFunctionToString);
                state.RawSet(-3);
            }
            state.SetTop(oldTop);
        }
        private static void CreateUserdataMapTable(KeraLua.Lua state)
        {
            state.PushString("userdataAddressToUserdata");
            state.NewTable();
            state.NewTable();
            state.PushString("__mode");
            state.PushString("v");
            state.SetTable(-3);
            state.SetMetaTable(-2);
            state.SetTable((int)LuaRegistry.Index);
        }

        private static int KosPrint(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var argCount = state.GetTop();
            var prints = new string[argCount];
            for (int i = 0; i < argCount; i++)
                prints[i] = state.ToString(i + 1);
            bindings[state.MainThread.Handle].Shared.Screen.Print(string.Join("    ", prints));
            return 0;
        }

        private static int UserdataType(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            if (state.GetMetaField(1, "__type") == LuaType.String)
                return 1;
            state.PushString(state.TypeName(1));
            return 1;
        }

        private static int CollectObject(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            bindings.TryGetValue(state.MainThread.Handle, out var binding);
            if (binding == null) return 0; // happens after DisposeStateBinding() was called
            var userdataAddress = state.ToUserData(1);
            binding.objects.TryGetValue(userdataAddress, out var obj);
            if (obj == null) return 0; // read the note in PushObject() to know when this happens
            binding.userdataPtrs.Remove(obj);
            binding.objects.Remove(userdataAddress);
            return 0;
        }

        private static int StructureToString(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var structure = bindings[state.MainThread.Handle].objects[state.ToUserData(1)];
            if (structure is IEnumerable<Structure>)
            {   // make enum structures ToString() method show 1 base indexed values in lua
                // replaces "\n  [*number*]" with "\n  [*number+1*]"
                state.PushString(Regex.Replace(structure.ToString(), @"\n\s*\[([0-9]+)\]", (match) =>
                    Regex.Replace(match.Groups[0].Value, match.Groups[1].Value, (int.Parse(match.Groups[1].Value) + 1).ToString())
                ));
            }
            else
                state.PushString(structure.ToString());
            return 1;
        }
        
        private static int KSFunctionToString(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var function = bindings[state.MainThread.Handle].objects[state.ToUserData(1)];
            state.PushString(function.GetType().Name);
            return 1;
        }

        private static int EnvIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var index = state.ToString(2);
            var binding = bindings[state.MainThread.Handle];
            if (binding.variables.TryGetValue(index, out var boundVar))
            {
                try { return PushLuaType(state, Structure.ToPrimitive(boundVar.Value), binding); }
                catch (Exception e) { Debug.Log(e); return state.Error(e.Message); }
            }
            if (binding.functions.TryGetValue(index, out var function))
            {
                return PushLuaType(state, function, binding);
            }
            return 0;
        }

        private static int EnvNewIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            var index = state.ToString(2);
            if (binding.variables.TryGetValue(index, out var boundVar) && boundVar.Set != null)
            {
                try { boundVar.Value = ToCSharpObject(state, 3, binding); }
                catch (Exception e) { Debug.Log(e); return state.Error(e.Message); }
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
            object obj = binding.objects[state.ToUserData(1)];
            var structure = obj as Structure;
            if (structure == null)
                return state.Error(string.Format("attempt to index a {0} value", obj.GetType().Name));

            try { return PushSuffixResult(state, binding, structure, 2); }
            catch (Exception e) { Debug.Log(e); return state.Error(e.Message); }
        }

        private static int PushSuffixResult(KeraLua.Lua state, BindingData binding, Structure structure, int index)
        {
            object pushValue = null;
            if (state.TypeName(index) == "number" && structure is IIndexable indexable)
            {
                pushValue = Structure.ToPrimitive(indexable.GetIndex((int)state.ToInteger(index)-(structure is Lexicon? 0 : 1), true));
                return PushLuaType(state, pushValue, binding);
            }
            
            var result = structure.GetSuffix(state.ToString(index), true);
            if (result == null)
                return PushLuaType(state, null, binding);
            
            if (result.HasValue)
            {
                pushValue = Structure.ToPrimitive(result.Value);
            }
            else if (result is DelegateSuffixResult delegateResult && delegateResult.RawDelInfo.Parameters.Length == 0)
            {
                var callResult = delegateResult.RawCall(null);
                if (delegateResult.RawDelInfo.ReturnType == typeof(void))
                    delegateResult.RawSetValue(ScalarValue.Create(0)); // this is what kerboscript does
                else
                    delegateResult.RawSetValue(Structure.FromPrimitiveWithAssert(callResult));
                pushValue = Structure.ToPrimitive(delegateResult.Value);
            } else
            {
                pushValue = result as DelegateSuffixResult; // if its somehow not DelegateSuffixResult push null
            }
            return PushLuaType(state, pushValue, binding);
        }

        private static int StructureNewIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            object obj = binding.objects[state.ToUserData(1)];
            var structure = obj as Structure;
            if (structure == null)
                return state.Error(string.Format("attempt to index a {0} value", obj.GetType().Name));
            object value = ToCSharpObject(state, 3, binding);
            if (value != null)
            {
                if (structure is IIndexable && state.TypeName(2) == "number")
                {
                    int intIndex = (int)state.ToInteger(2);
                    try { (structure as IIndexable).SetIndex(intIndex, Structure.FromPrimitive(value) as Structure); }
                    catch (Exception e) { Debug.Log(e); return state.Error(e.Message); }
                    return 0;
                }
                var index = state.ToString(2);
                try { structure.SetSuffix(index, Structure.FromPrimitive(value)); }
                catch (Exception e) { Debug.Log(e); return state.Error(e.Message); }
            }
            return 0;
        }
        
        private static int StructurePairs(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            var structure = binding.objects[state.ToUserData(1)] as Structure;
            if (structure == null)
                return state.Error("pairs metamethod can only be called with a Structure type");

            state.PushInteger(1);
            var enumCount = (structure is IIndexable && structure is IEnumerable<Structure> enumerable)
                ? enumerable.Count()
                : 0;
            state.NewTable();
            var index = 1;
            for (; index <= enumCount; index++)
            {
                state.PushInteger(index);
                state.PushInteger(index);
                state.SetTable(-3);
            }
            foreach (var name in structure.GetSuffixNames())
            {
                state.PushInteger(index++);
                state.PushString(name);
                state.SetTable(-3);
            }

            state.PushCClosure(StructureNext, 2); // pass the starting index and table with index-suffix pairs
            state.PushCopy(1);
            return 2;
        }

        private static int StructureNext(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            var structure = binding.objects[state.ToUserData(1)] as Structure;
            if (structure == null)
                return state.Error("iterator can only be called with a Structure type");
            // ignore the second argument
            var currentIndex = state.ToInteger(KeraLua.Lua.UpValueIndex(1));
            state.PushCopy(KeraLua.Lua.UpValueIndex(2));
            state.PushInteger(currentIndex);
            state.GetTable(-2);
            
            try { PushSuffixResult(state, binding, structure, -1); }
            catch (Exception e) { Debug.Log(e); return state.Error(e.Message); }
            
            state.PushInteger(currentIndex+1);
            state.Copy(-1, KeraLua.Lua.UpValueIndex(1));
            state.Remove(-1);
            return 2;
        }

        private static int KSFunctionCall(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = bindings[state.MainThread.Handle];
            var ksFunction = binding.objects[state.ToUserData(1)];
            
            var stack = (binding.Shared.Cpu as LuaCPU).Stack;
            stack.Clear();
            stack.PushArgument(new KOSArgMarkerType());
            for (int i = 2; i <= state.GetTop(); i++)
                stack.PushArgument(ToCSharpObject(state, i, binding));
            
            if (ksFunction is SafeFunctionBase function)
            {
                try { function.Execute(binding.Shared); }
                catch (Exception e) { Debug.Log(e); return state.Error(e.Message); }
                return PushLuaType(state, Structure.ToPrimitive(function.ReturnValue), binding);
            }
            if (ksFunction is DelegateSuffixResult delegateResult)
            {
                try { delegateResult.Invoke(binding.Shared.Cpu); }
                catch (Exception e) { Debug.Log(e); return state.Error(e.Message); }
                return PushLuaType(state, Structure.ToPrimitive(delegateResult.Value), binding);
            }
            return state.Error(string.Format("attempt to call a non function {0} value", ksFunction.GetType().Name));
        }

        private static int PushLuaType(KeraLua.Lua state, object obj, BindingData binding)
        {
            if (obj == null)
                state.PushNil();
            else if (obj is double db)
                state.PushNumber(db);
            else if (obj is int i)
                state.PushInteger(i);
            else if (obj is string str)
                state.PushString(str);
            else if (obj is bool b)
                state.PushBoolean(b);
            else
                return PushObject(state, obj, binding, obj is Structure? "Structure" : "KerboscriptFunction");
            return 1;
        }

        private static object ToCSharpObject(KeraLua.Lua state, int index, BindingData binding = null)
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
                    return binding?.objects.GetValueOrDefault(state.ToUserData(index), null);
                default:
                    return null;
            }
        }

        private static int PushObject(KeraLua.Lua state, object obj, BindingData binding, string metatable)
        {
            state.GetMetaTable("userdataAddressToUserdata");
            if (binding.userdataPtrs.TryGetValue(obj, out IntPtr userdataAddress)) // Object already in the list of object userdata? Push the userdata
            {
                // Note: starting with lua5.1 the garbage collector may remove weak reference items (such as our userdataAddressToUserdata values) when the initial GC sweep 
                // occurs, but the actual call of the __gc finalizer for that object may not happen until a little while later.  During that window we might call
                // this routine and find the element missing from userdataAddressToUserdata, but CollectObject() has not yet been called.  In that case, we go ahead and
                // do the same thing CollectObject() does and remove from out object maps
                state.PushLightUserData(userdataAddress);
                if (state.RawGet(-2) != LuaType.Nil)
                {   // if found the objects userdata return it
                    state.Remove(-2);
                    return 1;
                }
                state.Remove(-1);	// remove the nil value
                binding.objects.Remove(userdataAddress); // Remove from both our maps and fall out to create a new userdata
                binding.userdataPtrs.Remove(obj);
            }

            userdataAddress = state.NewUserData(0);
            state.GetMetaTable(metatable);
            state.SetMetaTable(-2);
            state.PushLightUserData(userdataAddress);
            state.PushCopy(-2);
            state.RawSet(-4); // add userdata on top of the stack to the userdataAddressToUserdata metatable at userdataAddress key
            state.Remove(-2);

            binding.objects[userdataAddress] = obj;
            binding.userdataPtrs[obj] = userdataAddress;

            return 1;
        }
        
        private static void DumpStack(KeraLua.Lua state, string debugName = "", BindingData binding = null)
        {
            binding = binding ?? bindings[state.MainThread.Handle];
            Debug.Log(debugName+"_________");
            for (int i = 0; i <= state.GetTop(); i++)
                Debug.Log(i+" "+state.TypeName(i)+" "+ToCSharpObject(state, i, binding));
            Debug.Log("____________________");
        }
    }
}
