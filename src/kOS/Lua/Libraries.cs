using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using KeraLua;
using Debug = UnityEngine.Debug;

namespace kOS.Lua
{
    public static class Libraries
    {
        private static readonly RegList whitelistedLibraries = new RegList
        {
            {"_G", Libs.Base.Open},
            {"coroutine", NativeMethods.luaopen_coroutine},
            {"string", NativeMethods.luaopen_string},
            {"utf8", NativeMethods.luaopen_utf8},
            {"table", NativeMethods.luaopen_table},
            {"math", NativeMethods.luaopen_math},
        };

        private static readonly RegList luaLibraries = new RegList
        {
            {"callbacks", Libs.Callbacks.Open},
            {"misc", Libs.Misc.Open},
        };

        private static readonly RegList devLibraries = new RegList
        {
            {"debug", NativeMethods.luaopen_debug},
            {"dev", Libs.Dev.Open},
        };
        
        public static void Open(KeraLua.Lua state)
        {
            /*
            the goal here is to remove the ability of lua scripts to be malicious and make it simpler to keep it that way
            it is achieved by:
            1. Opening only whitelisted libraries
            2. Overwriting the registry global environment table and loaded libraries table with tables that
                include only whitelisted fields
            3. Recursively removing every field in the previous global environment table and the loaded libraries table
                for an extra layer of protection in case the table or its inner tables are saved anywhere else
                
            this means that even if some potentially dangerous lua library/function gets added it would need
            to be explicitly added in the whitelist to be accessible by lua scripts
            */
            
            // require dummy library to check if "_LOADED" registry key is used for loaded libraries(LUA_LOADED_TABLE definition)
            state.RequireF("_dummy", DummyOpen, false);
            state.Pop(1);
            if (state.GetField((int)LuaRegistry.Index, "_LOADED") != LuaType.Table)
                throw new Exception("Loaded libraries table was not found at *registry*._LOADED");
            state.Pop(1);
            
            // open whitelisted libraries
            foreach (var library in whitelistedLibraries)
            {
                state.PushCFunction(library.function);
                state.PushString(library.name);
                state.Call(1, 1);
                state.SetGlobal(library.name);
            }

            // push 3 values onto the stack: new env table, new loaded table, deepCleanTable function
            using (var streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("kOS.Lua.whitelist.lua")))
            {
                state.LoadString(streamReader.ReadToEnd());
            }
            state.Call(0, 3);
            
            // call the deepCleanTable function on the global table
            state.PushCopy(-1);
            if (state.RawGetInteger((int)LuaRegistry.Index, (int)LuaRegistryIndex.Globals) != LuaType.Table)
                throw new Exception("Global table was not found at *registry*[LuaRegistryIndex.Globals]");
            state.Call(1, 0);
            
            // call the deepCleanTable function on the loaded table
            state.GetField((int)LuaRegistry.Index, "_LOADED");
            state.Call(1, 0);
            
            // assign the loaded table to *registry*._LOADED
            state.SetField((int)LuaRegistry.Index, "_LOADED");
            
            // assign the env table to *registry*[LuaRegistryIndex.Globals]
            state.RawSetInteger((int)LuaRegistry.Index, (int)LuaRegistryIndex.Globals);
            state.GarbageCollector(LuaGC.Collect, 0);
            
            // open lua libraries
            foreach (var library in luaLibraries)
            {
                state.RequireF(library.name, library.function,true);
                state.Pop(1);
            }
            
            // open dev libraries past the whitelist if built in debug configuration
            #if DEBUG
            Debug.LogWarning("LUA DEV LIBRARIES OPENED");
            foreach (var library in devLibraries)
            {
                state.RequireF(library.name, library.function,true);
                state.Pop(1);
            }
            #endif
        }
        
        private static int DummyOpen(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.NewTable();
            return 1;
        }
    }
    
    public class RegList : List<LuaRegister>
    {
        public void Add(string key, LuaFunction value) => Add(new LuaRegister { name = key, function = value });
    }
}