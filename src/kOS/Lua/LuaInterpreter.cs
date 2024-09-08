using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity;
using System.Net;
using System.IO;
using System.Reflection;
using System.CodeDom;
using NLua;
using KeraLua;
using kOS.Safe;

namespace kOS.Lua
{
    public class LuaInterpreter : IFixedUpdateObserver
    {
        private NLua.Lua state;
        private KeraLua.Lua commandCoroutine;
        private bool commandPending = false;

        protected SharedObjects Shared { get; private set; }

        public LuaInterpreter(SharedObjects shared)
        {
            Shared = shared;
            state = new NLua.Lua();
            state["Shared"] = Shared;
            state["FlightGlobals"] = UnityEngine.MonoBehaviour.FindObjectOfType<FlightGlobals>();
            using (var streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("kOS.Lua.init.lua"))) {
                try {
                    state.DoString(streamReader.ReadToEnd());
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
            Shared.UpdateHandler.AddFixedObserver(this);
        }

        static void YieldHook(System.IntPtr L, System.IntPtr ar)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            state.Yield(0);
        }

        // creates a new coroutine with the command and configures it to yield after 2000 instructions
        public void ProcessCommand(string command) // cre
        { // TODO: handle consecutive commands with some queue. Right now it just overwrites the previous command
            state.DoString($@"
commandCoroutine = coroutine.create(assert(load([=[{command}]=], ""chunk"", ""t"", _ENV)))
            "); // TODO: command injection from a file
            state.State.GetGlobal("commandCoroutine");
            commandCoroutine = state.State.ToThread(-1);
            state.State.Pop(1);
            commandCoroutine.SetHook(YieldHook, LuaHookMask.Count, 2000);
            commandPending = true;
        }

        public void KOSFixedUpdate(double dt)
        {
            // resumes the coroutine created by ProcessCommand after it was created and after it yielded due to running out of instructions
            if (commandCoroutine != null)
            {
                if (commandCoroutine.Status == LuaStatus.Yield | commandPending)
                {
                    commandPending= false;
                    LuaStatus status = commandCoroutine.Resume(state.State, 0);
                    if (status != LuaStatus.OK & status != LuaStatus.Yield)
                    {
                        Shared.Logger.Log(new Exception(commandCoroutine.ToString(-1)));
                    }
                }
            }
        }

        public void Dispose()
        {
            state.Dispose();
            Shared.UpdateHandler.RemoveFixedObserver(this);
        }
    }
}
