using KeraLua;
using kOS.Module;
using System;
using System.Runtime.InteropServices;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using kOS.Safe.Persistence;
using Debug = UnityEngine.Debug;

namespace kOS.Lua
{
    public static class LuaFunctions
    {
        public static void Add(KeraLua.Lua state)
        {
            AddFunction(state, "type", Type);
            AddFunction(state, "print", KosPrint);
            AddFunction(state, "warn", Warn);
            AddFunction(state, "wait", Wait);
            AddFunction(state, "loadfile", LoadFile);
            AddFunction(state, "dofile", DoFile);
            AddFunction(state, "getchar", GetChar);
        }

        private static void AddFunction(KeraLua.Lua state, string name, LuaFunction function, bool saveOverwrittenValue = true)
        {
            if (saveOverwrittenValue)
            {
                if (state.GetGlobal(name) != LuaType.Nil)
                    state.SetGlobal("_" + name);
                else
                    state.Pop(1);
            }
            state.PushCFunction(function);
            state.SetGlobal(name);
        }
        
        private static int Type(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            if (state.Type(1) == LuaType.UserData)
            {
                var obj = Binding.bindings[state.MainThread.Handle].Objects[state.ToUserData(1)];
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
        
        private static int Warn(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.CheckString(1);
            var shared = Binding.bindings[state.MainThread.Handle].Shared;
            shared.SoundMaker.BeginFileSound("error");
            shared.Screen.Print(state.ToString(1));
            return 0;
        }

        private static int Wait(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.CheckNumber(1);
            var shared = Binding.bindings[state.MainThread.Handle].Shared;
            shared.Cpu.YieldProgram(new YieldFinishedGameTimer(state.ToNumber(1)));
            return 0;
        }

        private static int LoadFile(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.CheckString(1);
            var filePath = state.ToString(1);
            var mode = state.OptString(2, "bt");
            var shared = Binding.bindings[state.MainThread.Handle].Shared;
            var file = shared.VolumeMgr.CurrentVolume.Open(filePath) as VolumeFile;
            if (file == null)
            {
                state.PushNil();
                state.PushString($"File '{filePath}' not found");
                return 2;
            }
            if (state.LoadBuffer(state.Encoding.GetBytes(file.ReadAll().String), file.Path.ToString(), mode) != LuaStatus.OK)
            {
                state.PushNil();
                state.PushCopy(-2);
                return 2;
            }
            if (state.GetTop() >= 4)
            {   // if there was a third argument set it as the _ENV value for the loaded function
                state.PushCopy(3);
                if (state.SetUpValue(-2, 1) == null)
                    state.Pop(1);
            }
            return 1;
        }

        private static int DoFile(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var argCount = state.GetTop();
            LoadFile(L);
            if (state.IsString(-1))
                return state.Error(state.ToString(-1));
            state.CallK(0, -1, argCount, DoFileContinuation);
            return DoFileContinuation(L, (int)LuaStatus.OK, (IntPtr)argCount);
        }

        private static int DoFileContinuation(IntPtr L, int status, IntPtr ctx)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var argCount = (int)ctx;
            return state.GetTop()-argCount;
        }
        
        public static int GetChar(IntPtr L)
        {
            return GetCharContinuation(L, 0, IntPtr.Zero);
        }

        private static int GetCharContinuation(IntPtr L, int status, IntPtr ctx)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var shared = Binding.bindings[state.MainThread.Handle].Shared;
            var q = shared.Screen.CharInputQueue;
            if (q.Count == 0)
                state.YieldK(0, 0, GetCharContinuation);
            state.PushString(q.Dequeue().ToString());
            return 1;
        }
    }
}