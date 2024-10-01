using KeraLua;
using kOS.Module;
using System;
using System.Runtime.InteropServices;
using Debug = UnityEngine.Debug;

namespace kOS.Lua
{
    public static class LuaFunctions
    {
        public static void Add(KeraLua.Lua state)
        {
            state.GetGlobal("print");
            state.SetGlobal("_print");
            state.PushCFunction(KosPrint);
            state.SetGlobal("print");
            state.GetGlobal("warn");
            state.SetGlobal("_warn");
            state.PushCFunction(Warning);
            state.SetGlobal("warn");
        }

        private static int KosPrint(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var argCount = state.GetTop();
            var prints = new string[argCount];
            for (int i = 0; i < argCount; i++)
                prints[i] = state.ToString(i + 1);
            Binding.bindings[state.MainThread.Handle].Shared.Screen.Print(string.Join("    ", prints));
            return 0;
        }
        
        private static int Warning(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.CheckString(1);
            var shared = Binding.bindings[state.MainThread.Handle].Shared;
            shared.SoundMaker.BeginFileSound("error");
            shared.Screen.Print(state.ToString(1));
            return 0;
        }
    }
}