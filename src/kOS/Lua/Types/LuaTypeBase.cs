using System;

namespace kOS.Lua.Types
{
    public abstract class LuaTypeBase
    {
        public abstract Type[] BindingTypes { get; }
        public abstract string MetatableName { get; }
    }
}