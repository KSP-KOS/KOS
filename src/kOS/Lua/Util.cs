using System;
using KeraLua;
using UnityEngine;

namespace kOS.Lua
{
    public static class Util
    {
        public static LuaType RawGetGlobal(KeraLua.Lua lua, string name)
        {
            lua.PushGlobalTable();
            lua.PushString(name);
            var type = lua.RawGet(-2);
            lua.Remove(-2);
            return type;
        }

        public static void LuaExceptionCatch(Action tryBody, KeraLua.Lua state) =>
            LuaExceptionCatch(() => { tryBody(); return null; }, state);   

        public static object LuaExceptionCatch(Func<object> tryBody, KeraLua.Lua state)
        {
            try { return tryBody(); }
            catch (Exception e)
            {
                Debug.Log(e);
                return state.Error(e.Message == ""? e.GetType().FullName : e.Message);
            }
        }
        
        public static void DumpStack(KeraLua.Lua state, string debugName = "", Binding.BindingData binding = null)
        {
            binding = binding ?? Binding.Bindings[state.MainThread.Handle];
            Debug.Log(debugName+"_________");
            for (int i = 0; i <= state.GetTop(); i++)
                Debug.Log(i+" "+state.TypeName(i)+" "+Binding.ToCSharpObject(state, i, binding));
            Debug.Log("____________________");
        }
    }
}