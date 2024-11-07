using KeraLua;
using System;
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

        private static readonly string packagesDirectory = Assembly.GetExecutingAssembly().Location.Replace("kOS.dll",
            "LuaPackages" + Path.DirectorySeparatorChar);
        private static readonly string libraryExtension =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dll" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "dylib" :
            "so";

        private static LuaFunction _loadlib;
        private static PackageConfig _packageConfig;

        public static int Open(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            
            /*
            Rewritten package library with limited functionality to make library safe.
            Only 'require' and 'loadlib' functions are taken from the native package library.
            The unsafe loadlib function is only used internally to look for C functions in a library.
            functionality removed:
            changing cpath, C searchers will only look in packagesDirectory
            loadlib, searchpath function
            */
            
            // call luaopen_package with a new fake global table
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
            state.RawSetInteger((int)LuaRegistry.Index, (int)LuaRegistryIndex.Globals);
            
            state.GetField(-1, "require");
            var require = state.ToCFunction(-1);
            state.Pop(2); // pop require and the fake global table
            
            state.GetField(-1, "loadlib");
            _loadlib = state.ToCFunction(-1);
            state.Pop(1); // pop loadlib
            
            // get package config
            state.GetField(-1, "config");
            var config = state.ToString(-1);
            string[] configValues = config.Split('\n');
            _packageConfig = new PackageConfig
            {
                DIRSEP = configValues[0], // /
                PATHSEP = configValues[1], // ;
                PATHMARK = configValues[2], // ?
                EXECDIR = configValues[3], // !
                IGMARK = configValues[4], // -
            };
            state.Pop(2); // pop config string and package table
            
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
            state.PushString(config);
            state.SetField(-2, "config");
            
            // add path string
            var dirSep = VolumePath.PathSeparator;
            // "0:/?.lua;0:/?/init.lua;/?.lua;/?/init.lua;?.lua;?/init.lua"
            state.PushString(
                $"0:{dirSep}{_packageConfig.PATHMARK}.lua{_packageConfig.PATHSEP}"+
                $"0:{dirSep}{_packageConfig.PATHMARK}{dirSep}init.lua{_packageConfig.PATHSEP}"+
                $"{dirSep}{_packageConfig.PATHMARK}.lua{_packageConfig.PATHSEP}"+
                $"{dirSep}{_packageConfig.PATHMARK}{dirSep}init.lua{_packageConfig.PATHSEP}"+
                $"{_packageConfig.PATHMARK}.lua{_packageConfig.PATHSEP}{_packageConfig.PATHMARK}{dirSep}init.lua");
            state.SetField(-2, "path");
            
            // add registry preload table
            state.GetField(LuaRegistry.Index, "_PRELOAD");
            state.SetField(-2, "preload");
            
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
            var volumeManager = Binding.bindings[state.MainThread.Handle].Shared.VolumeMgr;
            var errorMessage = "";
            foreach(var pathTemplate in paths.Split(_packageConfig.PATHSEP.ToCharArray()))
            {
                var path = pathTemplate.Replace(_packageConfig.PATHMARK, name);
                var globalPath = Binding.LuaExceptionCatch(() => volumeManager.GlobalPathFromObject(path), state) as GlobalPath;
                var file = Binding.LuaExceptionCatch(() => volumeManager.GetVolumeFromPath(globalPath).Open(globalPath), state) as VolumeFile;
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
            var errorMessage = "";
            foreach (var name in names)
            {
                foreach (var packagePath in Directory.GetFiles(packagesDirectory))
                {
                    var packageName = Path.GetFileName(packagePath);
                    if (name == packageName)
                    {
                        return packagePath;
                    }
                }
                errorMessage += $"no file '{packagesDirectory + name}'\n";
            }
            state.PushString(errorMessage);
            return null;
        }

        private static int LoadFunc(KeraLua.Lua state, string filename, string name)
        {
            var modname = name.Replace('.', '_');
            if (modname.Contains(_packageConfig.IGMARK))
            {
                var stat = LookForFunc(state, filename, "luaopen_" + modname.Split(_packageConfig.IGMARK.ToCharArray())[0]);
                if (stat != 2) return stat;
                return LookForFunc(state, filename, "luaopen_" + modname.Split(_packageConfig.IGMARK.ToCharArray())[1]);
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
            string[] names =
            {
                name + "." + libraryExtension,
                "loadall." + libraryExtension,
            };
            var filename = FindPackage(state, names);
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
            string[] names =
            {
                name.Split('.')[0] + "." + libraryExtension,
                "loadall." + libraryExtension,
            };
            var filename = FindPackage(state, names);
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
            public string DIRSEP;
            public string PATHSEP;
            public string PATHMARK;
            public string EXECDIR;
            public string IGMARK;
        }
    }
}