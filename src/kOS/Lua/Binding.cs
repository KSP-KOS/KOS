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
using kOS.Lua.Types;
using kOS.Suffixed;
using Smooth.Collections;
using Debug = UnityEngine.Debug;
using TimeSpan = kOS.Suffixed.TimeSpan;

namespace kOS.Lua
{
    public static class Binding
    {
        public static readonly Dictionary<IntPtr, BindingData> bindings = new Dictionary<IntPtr, BindingData>();
        private static readonly HashSet<Type> uniqueTypes = new HashSet<Type>()
        {   // Types of objects that you want to create new lua instances of when pushing onto the stack,
            // but that have overridden Equals and GetHashCode methods so BindingData.Objects dictionary
            // picks "equal" already created object. For example "v1 = v(0,0,0); v2 = v(0,0,0); v1.x = 1; print(v2.x);"
            // would print 1 because both v1 and v2 would be the same lua object, which is what we are avoiding here.
            typeof(Vector), typeof(Direction), typeof(TimeSpan), typeof(TimeStamp)
        };

        // the CSharp object to userdata binding model was adapted from NLua model
        // with some simplifications and changes to make it work on Structures
        public class BindingData
        {
            public readonly Dictionary<string, BoundVariable> Variables;
            public readonly Dictionary<string, SafeFunctionBase> Functions;
            public readonly Dictionary<IntPtr, object> Objects = new Dictionary<IntPtr, object>();
            public readonly Dictionary<object, IntPtr> UserdataPtrs = new Dictionary<object, IntPtr>();
            public readonly LuaTypeBase[] Types;
            public readonly SharedObjects Shared;
            
            public BindingData(KeraLua.Lua state, SharedObjects shared, Dictionary<string, BoundVariable> boundVariables, Dictionary<string, SafeFunctionBase> functions)
            {
                Shared = shared;
                Variables = boundVariables;
                Functions = functions;
                Types = new LuaTypeBase[]
                {
                    new KSStructure(state),
                    new KSFunction(state),
                };
            }
        }

        public static void BindToState(KeraLua.Lua state, SharedObjects shared)
        {
            state = state.MainThread;
            bindings[state.Handle] = new BindingData(
                state,
                shared,
                (shared.BindingMgr as BindingManager).RawVariables,
                (shared.FunctionManager as FunctionManager).RawFunctions
            );
            
            LuaFunctions.Add(state);
            
            state.PushCFunction(EnvIndex);
            state.SetGlobal("envIndex");
            state.PushCFunction(EnvNewIndex);
            state.SetGlobal("envNewIndex");
            state.GetGlobal("type");
            state.SetGlobal("_type");
            state.PushCFunction(TypeFunction);
            state.SetGlobal("type");
            int oldTop = state.GetTop();
            state.DoString(@"local mt = { __index = envIndex, __newindex = envNewIndex }; setmetatable(_ENV, mt)");
            
            state.PushString("userdataAddressToUserdata");
            state.NewTable();
            state.NewTable();
            state.PushString("__mode");
            state.PushString("v");
            state.SetTable(-3);
            state.SetMetaTable(-2);
            state.SetTable((int)LuaRegistry.Index);
            
            state.SetTop(oldTop);
        }

        public static int CollectObject(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            bindings.TryGetValue(state.MainThread.Handle, out var binding);
            if (binding == null) return 0; // happens after DisposeStateBinding() was called
            var userdataAddress = state.ToUserData(1);
            binding.Objects.TryGetValue(userdataAddress, out var obj);
            if (obj == null) return 0; // read the note in PushObject() to know when this happens
            binding.UserdataPtrs.Remove(obj);
            binding.Objects.Remove(userdataAddress);
            return 0;
        }

        public static int ObjectToString(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var obj = Binding.bindings[state.MainThread.Handle].Objects[state.ToUserData(1)];
            state.PushString(obj.ToString());
            return 1;
        }
        
        private static int TypeFunction(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            if (state.Type(1) == LuaType.UserData)
            {
                var obj = bindings[state.MainThread.Handle].Objects[state.ToUserData(1)];
                if (obj is Structure structure)
                {
                    state.PushString(structure.KOSName);
                    return 1;
                }
            }
            if (state.GetMetaField(1, "__type") == LuaType.String)
                return 1;
            state.PushString(state.TypeName(1));
            return 1;
        }

        private static int EnvIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var index = state.ToString(2);
            var binding = bindings[state.MainThread.Handle];
            if (binding.Variables.TryGetValue(index, out var boundVar))
            {
                try { return PushLuaType(state, Structure.ToPrimitive(boundVar.Value), binding); }
                catch (Exception e) { Debug.Log(e); return state.Error(e.Message); }
            }
            if (binding.Functions.TryGetValue(index, out var function))
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
            if (binding.Variables.TryGetValue(index, out var boundVar) && boundVar.Set != null)
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

        public static object ToCSharpObject(KeraLua.Lua state, int index, BindingData binding = null)
        {
            switch (state.Type(index))
            {
                case LuaType.Number:
                    return state.ToNumber(index);
                case LuaType.String:
                    return state.ToString(index);
                case LuaType.Boolean:
                    return state.ToBoolean(index);
                case LuaType.UserData:
                    return binding?.Objects.GetValueOrDefault(state.ToUserData(index), null);
                default:
                    return null;
            }
        }
        
        public static int PushLuaType(KeraLua.Lua state, object obj, BindingData binding)
        {
            switch (obj)
            {
                case null:
                    state.PushNil();
                    break;
                case double db:
                    state.PushNumber(db);
                    break;
                case int i:
                    state.PushInteger(i);
                    break;
                case string str:
                    state.PushString(str);
                    break;
                case bool b:
                    state.PushBoolean(b);
                    break;
                default:
                {
                    foreach (var type in binding.Types)
                        if (type.BindingTypes.Any(t => t.IsInstanceOfType(obj)))
                            return PushObject(state, obj, binding, type.MetatableName);
                    state.PushNil();
                    break;
                }
            }
            return 1;
        }

        private static int PushObject(KeraLua.Lua state, object obj, BindingData binding, string metatable)
        {
            var isUnique = uniqueTypes.Contains(obj.GetType());
            state.GetMetaTable("userdataAddressToUserdata");
            if (!isUnique && binding.UserdataPtrs.TryGetValue(obj, out IntPtr userdataAddress)) // Object already in the list of object userdata? Push the userdata
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
                binding.Objects.Remove(userdataAddress); // Remove from both our maps and fall out to create a new userdata
                binding.UserdataPtrs.Remove(obj);
            }

            userdataAddress = state.NewUserData(0);
            state.GetMetaTable(metatable);
            state.SetMetaTable(-2);
            state.PushLightUserData(userdataAddress);
            state.PushCopy(-2);
            state.RawSet(-4); // add userdata on top of the stack to the userdataAddressToUserdata metatable at userdataAddress key
            state.Remove(-2);

            binding.Objects[userdataAddress] = obj;
            if (!isUnique)
                binding.UserdataPtrs[obj] = userdataAddress;

            return 1;
        }
        
        public static void DumpStack(KeraLua.Lua state, string debugName = "", BindingData binding = null)
        {
            binding = binding ?? bindings[state.MainThread.Handle];
            Debug.Log(debugName+"_________");
            for (int i = 0; i <= state.GetTop(); i++)
                Debug.Log(i+" "+state.TypeName(i)+" "+ToCSharpObject(state, i, binding));
            Debug.Log("____________________");
        }
    }
}
