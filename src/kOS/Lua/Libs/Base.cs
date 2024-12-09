using KeraLua;
using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using kOS.Safe.Persistence;

namespace kOS.Lua.Libs
{
    public static class Base
    {
        private static readonly RegList baseLib = new RegList
        {
            {"dofile", DoFile},
            {"load", Load},
            {"loadfile", LoadFile},
            {"print", Print},
            {"type", Type},
            {"warn", Warn},
            {"wait", Wait},
            {null, null}
        };

        public static int Open(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            
            // temporarily store the global table on the call stack
            state.PushGlobalTable();
            
            // temporarily replace global table for the native open method to dump its values there
            state.NewTable();
            state.RawSetInteger((int)LuaRegistry.Index, (int)LuaRegistryIndex.Globals);
            
            state.PushCFunction(NativeMethods.luaopen_base);
            /*
            LUAMOD_API int luaopen_base (lua_State *L) {
                /* open lib into global table * /
                lua_pushglobaltable(L);
                luaL_setfuncs(L, base_funcs, 0);
                /* set global _G * /
                lua_pushvalue(L, -1);
                lua_setfield(L, -2, LUA_GNAME);
                /* set global _VERSION * /
                lua_pushliteral(L, LUA_VERSION);
                lua_setfield(L, -2, "_VERSION");
                return 1;
            }
            */
            state.Call(0, 1);
            
            // restore the global table
            state.Rotate(-2, 1); // global table <-> luaopen_base table
            state.RawSetInteger((int)LuaRegistry.Index, (int)LuaRegistryIndex.Globals);

            // create LuaRegister list with whitelisted fields from luaopen_base table
            var nativeFuncs = new RegList
            {
                {"assert", null},
                {"collectgarbage", null},
                // {"dofile", luaB_dofile}, access to file system
                {"error", null},
                {"getmetatable", null},
                {"ipairs", null},
                // {"loadfile", luaB_loadfile}, access to file system, loading binary chunks
                // {"load", luaB_load}, access to file system, loading binary chunks
                {"next", null},
                {"pairs", null},
                {"pcall", null},
                // {"print", luaB_print}, not used
                // {"warn", luaB_warn}, not used
                {"rawequal", null},
                {"rawlen", null},
                {"rawget", null},
                {"rawset", null},
                {"select", null},
                {"setmetatable", null},
                {"tonumber", null},
                {"tostring", null},
                {"type", null},
                {"xpcall", null},
                /* placeholders */
                {"_G", null},
                {"_VERSION", null},
                {null, null}
            };
            for (int i = 0; i < nativeFuncs.Count - 1; i++)
            {
                var func = nativeFuncs[i];
                if (state.GetField(-1, func.name) == LuaType.Function)
                {
                    nativeFuncs[i] = new LuaRegister { name = func.name, function = state.ToCFunction(-1) };
                }
                state.Pop(1);
            }
            
            // add whitelisted fields to the global table
            state.PushGlobalTable();
            state.SetFuncs(nativeFuncs.ToArray(), 0);
            
            // add _G table
            state.PushGlobalTable();
            state.SetField(-2, "_G");
            
            // add _VERSION from the luaopen_base table
            state.GetField(-2, "_VERSION");
            state.SetField(-2, "_VERSION");
            
            // remove the luaopen_base table from the stack
            state.Remove(-2);
            
            // save the default type function at "_type" index
            state.GetField(-1, "type");
            state.SetField(-2, "_type");
            
            // add new functions
            state.SetFuncs(baseLib.ToArray(), 0);
            
            return 1;
        }
        
        private static int Type(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            if (state.Type(1) == LuaType.UserData)
            {
                var obj = Binding.Bindings[state.MainThread.Handle].Objects[state.ToUserData(1)];
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

        private static int Print(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var argCount = state.GetTop();
            var prints = new string[argCount];
            for (int i = 0; i < argCount; i++)
                prints[i] = state.ToString(i + 1);
            Binding.Bindings[state.MainThread.Handle].Shared.Screen.Print(string.Join("    ", prints));
            return 0;
        }
        
        private static int Warn(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var errorMessage = state.CheckString(1);
            var tracebackLevel = state.OptInteger(2, 0);
            if (tracebackLevel > 0)
            {
                state.Traceback(state, (int)tracebackLevel);
                errorMessage += "\n" + state.ToString(-1);
            }
            var shared = Binding.Bindings[state.MainThread.Handle].Shared;
            shared.SoundMaker.BeginFileSound("error");
            shared.Screen.Print(errorMessage);
            return 0;
        }

        private static int Load(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var chunk = state.CheckString(1);
            var chunkName = state.OptString(2, "chunk");
            if (state.LoadString(chunk, chunkName) != LuaStatus.OK)
            {
                state.PushNil();
                state.PushCopy(-2);
                return 2;
            }
            if (!state.IsNoneOrNil(4))
            {
                state.PushCopy(4);
                if (state.SetUpValue(-2, 1) == null)
                    state.Pop(1);
            }
            return 1;
        }

        private static int LoadFile(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.CheckString(1);
            var filePath = state.ToString(1);
            var shared = Binding.Bindings[state.MainThread.Handle].Shared;
            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(filePath);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);
            VolumeFile file = volume.Open(path) as VolumeFile;
            if (file == null)
            {
                state.PushNil();
                state.PushString($"File '{filePath}' not found");
                return 2;
            }
            if (state.LoadString(file.ReadAll().String, file.Path.ToString()) != LuaStatus.OK)
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
        
        private static int Wait(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.CheckNumber(1);
            var shared = Binding.Bindings[state.MainThread.Handle].Shared;
            shared.Cpu.YieldProgram(new YieldFinishedGameTimer(state.ToNumber(1)));
            return 0;
        }
    }
}