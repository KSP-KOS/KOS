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
            {"package", Libs.Package.Open},
            {"coroutine", NativeMethods.luaopen_coroutine},
            {"string", NativeMethods.luaopen_string},
            {"utf8", NativeMethods.luaopen_utf8},
            {"table", NativeMethods.luaopen_table},
            {"math", NativeMethods.luaopen_math},
        };

        private static readonly RegList devLibraries = new RegList
        {
            {"debug", NativeMethods.luaopen_debug},
            {"dev", Libs.Dev.Open},
        };
        
        public static void Open(KeraLua.Lua state, SharedObjects shared)
        {
            /*
            the goal here is to remove the ability of lua scripts to be malicious and make it simpler to keep it that way
            it is achieved by:
            1. Opening only whitelisted libraries
            2. Recursively removing every field in the registry table
            3. Adding whitelisted fields to the registry table
                
            this means that even if some potentially dangerous lua library/function gets added it would need
            to be explicitly added in the whitelist to be accessible by lua scripts
            */
            
            // open whitelisted libraries
            foreach (var library in whitelistedLibraries)
            {
                state.PushCFunction(library.function);
                state.PushString(library.name);
                state.Call(1, 1);
                state.SetGlobal(library.name);
            }

            // whitelist the registry table
            using (var streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("kOS.Lua.whitelist.lua")))
            {
                state.LoadString(streamReader.ReadToEnd());
            }
            state.PushCopy((int)LuaRegistry.Index);
            state.PushCFunction(SetUpvalue);
            state.Call(2, 0);
            
            state.GarbageCollector(LuaGC.Collect, 0);
            
            Binding.BindToState(state, shared);
            
            // open lua libraries
            var modulesDir =
                Path.Combine(new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent.Parent.FullName, 
                "PluginData", "LuaModules");
            var luaModules = Directory.GetFiles(modulesDir, "*.lua", SearchOption.AllDirectories);
            foreach (var luaModule in luaModules)
            {
                using (var streamReader = new StreamReader(luaModule))
                {
                    var moduleName = Path.GetFileNameWithoutExtension(luaModule);
                    
                    // call the module file
                    if (state.LoadString(streamReader.ReadToEnd()) != LuaStatus.OK ||
                        state.PCall(0, 1, 0) != LuaStatus.OK)
                    {
                        shared.SoundMaker.BeginFileSound("error");
                        shared.Screen.Print($"error loading module '{moduleName}':\n" + state.ToString(-1));
                        state.Pop(1);
                        continue;
                    }
                    
                    // call "init" field on the module if it exists
                    if (state.Type(-1) == LuaType.Table)
                    {
                        if (state.GetField(-1, "init") == LuaType.Function)
                        {
                            if (state.PCall(0, 0, 0) != LuaStatus.OK)
                            {
                                shared.SoundMaker.BeginFileSound("error");
                                shared.Screen.Print($"error in 'init' function of module '{moduleName}':\n" + state.ToString(-1));
                                state.Pop(1);
                            }
                        }
                        else
                        {
                            state.Pop(1);
                        }
                    }

                    // add the module to the global table and the loaded table
                    state.PushCopy(-1);
                    state.SetGlobal(moduleName);
                    state.GetField(LuaRegistry.Index, "_LOADED");
                    state.Rotate(-2, 1);
                    state.SetField(-2, moduleName);
                    state.Pop(1);
                }
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

        private static int SetUpvalue(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.CheckType(1, LuaType.Function);
            var n = (int)state.CheckInteger(2);
            state.CheckAny(3);
            state.SetUpValue(1, n);
            return 0;
        }
    }
    
    public class RegList : List<LuaRegister>
    {
        public void Add(string key, LuaFunction value) => Add(new LuaRegister { name = key, function = value });
    }
}