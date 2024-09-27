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
            state.SetWarningFunction(Warning, state.MainThread.Handle);
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

        private static void Warning(IntPtr ud, IntPtr msg, int tocont)
        {
            var shared = Binding.bindings[ud].Shared;
            (shared.Interpreter as LuaInterpreter).DisplayError(Marshal.PtrToStringAnsi(msg));
        }

        // private static int SetSteering(IntPtr L)
        // {
        //     var state = KeraLua.Lua.FromIntPtr(L);
        //     var binding = Binding.bindings[state.MainThread.Handle];
        //     var steeringManager = kOSVesselModule.GetInstance(binding.Shared.Vessel).GetFlightControlParameter("steering");
        //     return 0;
        // }
    }
}