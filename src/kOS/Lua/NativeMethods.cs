using System.Runtime.InteropServices;
using lua_State = System.IntPtr;

namespace kOS.Lua
{
    internal static class NativeMethods
    {
        #if __IOS__ || __TVOS__ || __WATCHOS__ || __MACCATALYST__
                private const string LuaLibraryName = "@rpath/liblua54.framework/liblua54";
        #elif __ANDROID__
                private const string LuaLibraryName = "liblua54.so";
        #elif __MACOS__ 
                private const string LuaLibraryName = "liblua54.dylib";
        #elif WINDOWS_UWP
                private const string LuaLibraryName = "lua54.dll";
        #else
                private const string LuaLibraryName = "lua54";
        #endif
        
        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_base(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_package(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_coroutine(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_table(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_io(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_os(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_string(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_math(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_utf8(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaopen_debug(lua_State luaState);
    }
}