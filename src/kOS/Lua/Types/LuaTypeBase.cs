using System;

namespace kOS.Lua.Types
{
    public abstract class LuaTypeBase
    {
        public abstract Type[] BindingTypes { get; }
        public abstract string MetatableName { get; }
        
        private protected static void AddMethod(KeraLua.Lua state, string name, KeraLua.LuaFunction metaMethod)
        {
            state.PushString(name);
            state.PushCFunction(metaMethod);
            state.RawSet(-3);
        }
    }
}