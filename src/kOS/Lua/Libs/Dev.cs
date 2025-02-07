using KeraLua;
using System;

namespace kOS.Lua.Libs
{
    public static class Dev
    {
        private static readonly RegList devLib = new RegList
        {
            {"log", Log},
            {"getregistry", GetRegistry},
            {"getupvalues", GetUpvalues},
            {null, null}
        };

        public static int Open(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.PushGlobalTable();
            state.SetFuncs(devLib.ToArray(), 0);
            return 1;
        }
        
        private static int Log(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var logMessage = state.CheckString(1);
            UnityEngine.Debug.Log(logMessage);
            return 0;
        }

        private static int GetRegistry(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.PushCopy((int)LuaRegistry.Index);
            return 1;
        }
            
        // gets a table with all upvalues on the first function argument
        // that are not a key in the second optional table argument
        // returns sequence table of tables with name, upvalue, index fields
        private static int GetUpvalues(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.CheckType(1, LuaType.Function);
            if (state.Type(2) == LuaType.None)
                state.NewTable();
            else
                state.CheckType(2, LuaType.Table);
            state.NewTable();
            for (int i = 1; ; i++)
            {
                var uvName = state.GetUpValue(1, i);
                if (uvName == null) break;
                state.PushCopy(-1);
                if (state.RawGet(2) == LuaType.Nil)
                {
                    state.Pop(1);
                    state.NewTable();
                    state.Rotate(-2, 1);
                    state.SetField(-2, "upvalue");
                    state.PushString(uvName);
                    state.SetField(-2, "name");
                    state.PushInteger(i);
                    state.SetField(-2, "index");
                    state.SetInteger(3, state.Length(3)+1);
                }
                else
                {
                    state.Pop(2);
                }
            }
            return 1;
        }
    }
}
