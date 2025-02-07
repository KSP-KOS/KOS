using KeraLua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using kOS.Safe.Persistence;
using Debug = UnityEngine.Debug;

namespace kOS.Lua.Libs
{
    public static class Package
    {
        private static readonly LuaFunction[] searchers =
        {
            PreloadSearcher,
            LuaSearcher,
            CSearcher,
            CRootSearcher
        };

        private static readonly string osName = 
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" :
            "Linux";
        private static readonly string modulesDirectory =
            Path.Combine(new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent.Parent.FullName, 
                "PluginData", "LuaModules", osName);
        private static readonly string[] libraryExtensions =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new [] { "dll" } :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new [] { "so", "dylib", "framework" } :
            new [] { "so" };
        private static readonly PackageConfig config = new PackageConfig
        {
            DIRSEP = VolumePath.PathSeparator,
            PATHSEP = ';',
            PATHMARK = '?',
            EXECDIR = '!',
            IGMARK = '-',
        };
        // "0:/?.lua;0:/?/init.lua;/?.lua;/?/init.lua;?.lua;?/init.lua"
        private static readonly string luaSearchPath = 
            $"0:{config.DIRSEP}{config.PATHMARK}.lua{config.PATHSEP}"+
            $"0:{config.DIRSEP}{config.PATHMARK}{config.DIRSEP}init.lua{config.PATHSEP}"+
            $"{config.DIRSEP}{config.PATHMARK}.lua{config.PATHSEP}"+
            $"{config.DIRSEP}{config.PATHMARK}{config.DIRSEP}init.lua{config.PATHSEP}"+
            $"{config.PATHMARK}.lua{config.PATHSEP}{config.PATHMARK}{config.DIRSEP}init.lua";
        private static LuaFunction _loadlib;

        public static int Open(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            
            /*
            Rewritten package library with limited functionality to make library safe.
            Only 'require' and 'loadlib' functions are taken from the native package library.
            The unsafe loadlib function is only used internally to look for C functions in a library.
            functionality removed:
            changing cpath, C searchers will only look in modulesDirectory
            loadlib, searchpath function
            */
            
            // swap the global table and call luaopen_package
            state.PushGlobalTable();
            state.NewTable();
            state.PushCopy(-1);
            state.RawSetInteger((int)LuaRegistry.Index, (int)LuaRegistryIndex.Globals);
            state.PushCFunction(NativeMethods.luaopen_package);
            /*
               LUAMOD_API int luaopen_package (lua_State *L) {
                 createclibstable(L);
                 luaL_newlib(L, pk_funcs);  /* create 'package' table * /
                 createsearcherstable(L);
                 /* set paths * /
                 setpath(L, "path", LUA_PATH_VAR, LUA_PATH_DEFAULT);
                 setpath(L, "cpath", LUA_CPATH_VAR, LUA_CPATH_DEFAULT);
                 /* store config information * /
                 lua_pushliteral(L, LUA_DIRSEP "\n" LUA_PATH_SEP "\n" LUA_PATH_MARK "\n"
                                    LUA_EXEC_DIR "\n" LUA_IGMARK "\n");
                 lua_setfield(L, -2, "config");
                 /* set field 'loaded' * /
                 luaL_getsubtable(L, LUA_REGISTRYINDEX, LUA_LOADED_TABLE);
                 lua_setfield(L, -2, "loaded");
                 /* set field 'preload' * /
                 luaL_getsubtable(L, LUA_REGISTRYINDEX, LUA_PRELOAD_TABLE);
                 lua_setfield(L, -2, "preload");
                 lua_pushglobaltable(L);
                 lua_pushvalue(L, -2);  /* set 'package' as upvalue for next lib * /
                 luaL_setfuncs(L, ll_funcs, 1);  /* open lib into global table * /
                 lua_pop(L, 1);  /* pop global table * /
                 return 1;  /* return 'package' table * /
               }
            */
            state.Call(0, 1);
            // global table, fake global table, package table
            state.Insert(-3);
            state.Insert(-2);
            // package table, fake global table, global table
            state.RawSetInteger((int)LuaRegistry.Index, (int)LuaRegistryIndex.Globals); // restore the real global table
            
            state.GetField(-1, "require");
            var require = state.ToCFunction(-1);
            state.Pop(2); // pop require and the fake global table
            
            state.GetField(-1, "loadlib");
            _loadlib = state.ToCFunction(-1);
            state.Pop(2); // pop loadlib and package table
            
            // new package table
            state.NewTable();
            
            // add require to the global table with a nil upvalue that later will be set to the whitelisted package table
            state.PushNil();
            state.PushCClosure(require, 1);
            state.SetGlobal("require");
            
            // add searchers table
            state.NewTable();
            var i = 1;
            foreach (var searcher in searchers)
            {
                state.PushNil();
                state.PushCClosure(searcher, 1);
                state.RawSetInteger(-2, i++);
            }
            state.SetField(-2, "searchers");
            
            // add config
            state.PushString(config.DIRSEP+"\n"+config.PATHSEP+"\n"+config.PATHMARK+"\n"+config.EXECDIR+"\n"+config.IGMARK);
            state.SetField(-2, "config");
            
            // add path string
            state.PushString(luaSearchPath);
            state.SetField(-2, "path");
            
            // loaded and preload tables are getting added in whitelist.lua
            
            return 1;
        }

        private static int PreloadSearcher(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var name = state.CheckString(1);
            state.GetField(LuaRegistry.Index, "_PRELOAD");
            if (state.GetField(-1, name) == LuaType.Nil)
            {
                state.PushString($"no field package.preload['{name}']");
                return 1;
            }
            state.PushString(":preload:");
            return 2;
        }

        private static int LuaSearcher(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var name = state.CheckString(1);
            if (state.GetField((int)LuaRegistry.Index-1, "path") != LuaType.String)
                return state.Error("'package.path' must be a string");
            var paths = state.ToString(-1);
            var volumeManager = Binding.Bindings[state.MainThread.Handle].Shared.VolumeMgr;
            var errorMessage = "";
            foreach(var pathTemplate in paths.Split(config.PATHSEP))
            {
                var path = pathTemplate.Replace(config.PATHMARK.ToString(), name);
                var globalPath = Util.LuaExceptionCatch(() => volumeManager.GlobalPathFromObject(path), state) as GlobalPath;
                var file = Util.LuaExceptionCatch(() => volumeManager.GetVolumeFromPath(globalPath).Open(globalPath), state) as VolumeFile;
                if (file == null)
                {
                    errorMessage += $"no file '{path}'\n";
                    continue;
                }
                if (state.LoadString(file.ReadAll().String, file.Path.ToString()) != LuaStatus.OK)
                    return state.Error($"error loading module '{name}' from file '{globalPath}':\n\t{state.ToString(-1)}");
                state.PushString(globalPath.ToString());
                return 2;
            }
            state.PushString(errorMessage);
            return 1;
        }

        private static string FindPackage(KeraLua.Lua state, string[] names)
        {
            var packages = new Dictionary<string, string>();
            if (Directory.Exists(modulesDirectory))
            {
                foreach (var packagePath in Directory.GetFiles(modulesDirectory))
                {
                    packages.Add(Path.GetFileName(packagePath), packagePath);
                }
            }
            var errorMessage = "";
            foreach (var name in names)
            {
                foreach (var extension in libraryExtensions)
                {
                    var fileName = name + "." + extension;
                    if (packages.TryGetValue(fileName, out var packagePath))
                    {
                        return packagePath;
                    }
                    errorMessage += $"no file '{Path.Combine(modulesDirectory, fileName)}'\n";
                }
            }
            state.PushString(errorMessage);
            return null;
        }

        private static int LoadFunc(KeraLua.Lua state, string filename, string name)
        {
            var modname = name.Replace('.', '_');
            if (modname.Contains(config.IGMARK.ToString()))
            {
                var stat = LookForFunc(state, filename, "luaopen_" + modname.Split(config.IGMARK)[0]);
                if (stat != 2) return stat;
                return LookForFunc(state, filename, "luaopen_" + modname.Split(config.IGMARK)[1]);
            }
            return LookForFunc(state, filename, "luaopen_" + modname);
        }

        private static int LookForFunc(KeraLua.Lua state, string filename, string sym)
        {
            var precallTop = state.GetTop();
            state.PushCFunction(_loadlib);
            state.PushString(filename);
            state.PushString(sym);
            state.Call(2, -1);
            var stat = 0;
            if (state.GetTop() - precallTop == 3) // if error
            {
                stat = state.ToString(-1) == "open" ? 1 : 2;
                state.Pop(1);
                state.Remove(-2);
            }
            return stat;
        }
        
        private static int CSearcher(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var name = state.CheckString(1);
            var filename = FindPackage(state, new [] { name, "loadall" });
            if (filename == null) return 1;
            var success = 0 == LoadFunc(state, filename, name);
            if (!success)
                return state.Error($"error loading module '{name}' from file '{filename}':\n\t{state.ToString(-1)}");
            state.PushString(filename);
            return 2;
        }
        
        private static int CRootSearcher(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var name = state.CheckString(1);
            if (!name.Contains(".")) return 0;
            var filename = FindPackage(state, new [] { name.Split('.')[0], "loadall" });
            if (filename == null) return 1;
            var status = LoadFunc(state, filename, name);
            if (status == 1)
                return state.Error($"error loading module '{name}' from file '{filename}':\n\t{state.ToString(-1)}");
            if (status == 2)
            {
                state.PushString($"no module '{name}' in file '{name.Split('.')[0]}'");
                return 1;
            }
            state.PushString(filename);
            return 2;
        }
        
        private struct PackageConfig
        {
            public char DIRSEP;
            public char PATHSEP;
            public char PATHMARK;
            public char EXECDIR;
            public char IGMARK;
        }
    }
}