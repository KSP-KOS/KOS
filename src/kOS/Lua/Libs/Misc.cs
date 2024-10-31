using System;
using System.IO;
using System.Reflection;

namespace kOS.Lua.Libs
{
    public static class Misc
    {
        public static int Open(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            using (var streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("kOS.Lua.Libs.misc.lua")))
            {
                state.LoadString(streamReader.ReadToEnd());
            }
            state.Call(0, 1);

            state.GetField(-1, "init");
            state.Call(0, 0);
            
            return 1;
        }
    }
}
