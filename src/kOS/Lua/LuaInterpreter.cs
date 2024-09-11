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
using kOS.Safe.Utilities;
using Debug = UnityEngine.Debug;
using kOS.Safe.Screen;

namespace kOS.Lua
{
    public class LuaInterpreter : IInterpreter, IFixedUpdateObserver
    {
        private NLua.Lua state;
        private KeraLua.Lua commandCoroutine;
        private bool commandPending = false;
        private static int instructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
        private static int instructionsThisUpdate = 0;
        private const string version = "5.4";

        protected SharedObjects Shared { get; private set; }

        public LuaInterpreter(SharedObjects shared)
        {
            Shared = shared;
        }

        public void Boot()
        {
            state?.Dispose();
            state = new NLua.Lua();
            commandCoroutine = state.State.NewThread();
            state["Shared"] = Shared;
            state["FlightGlobals"] = UnityEngine.MonoBehaviour.FindObjectOfType<FlightGlobals>();
            using (var streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("kOS.Lua.init.lua"))) {
                try {
                    state.DoString(streamReader.ReadToEnd());
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
            // TODO: boot files

            Shared.Cpu.Boot(); // TODO: do lua only booting when we figure out what lua needs to run

            if (Shared.Terminal != null) Shared.Terminal.Reset();
            // Booting message
            if (Shared.Screen != null)
            {
                Shared.Screen.ClearScreen();
                string bootMessage = string.Format("kOS Operating System\nLua v{0}\n(manual at {1})\n \nProceed.\n", version, SafeHouse.DocumentationURL);
                Shared.Screen.Print(bootMessage);
            }
            Shared.UpdateHandler.AddFixedObserver(this);
        }

        public void Shutdown()
        {
            Shared.UpdateHandler.RemoveFixedObserver(this);
            BreakExecution(true);
        }

        // This function will be running in lua land so be careful about stuff being garbage collected
        // Setting instructionsThisUpdate and instructionsPerUpdate to static somehow prevents them from being collected
        private void YieldHook(System.IntPtr L, System.IntPtr ar)
        {
            if (instructionsThisUpdate++ >= instructionsPerUpdate)
            {
                KeraLua.Lua.FromIntPtr(L).Yield(0);
            }
        }

        public void ProcessCommand(string commandText)
        {
            // TODO: use the chunk just created by IsCommandComplete and use C api instead of dostring
            // reuse commandCoroutine?
            state.DoString($@"
commandCoroutine = coroutine.create(assert(load([=[{commandText}]=], ""chunk"", ""t"", _ENV)))
            ");
            state.State.GetGlobal("commandCoroutine");
            commandCoroutine.Dispose();
            commandCoroutine = state.State.ToThread(-1);
            state.State.Pop(1);

            // TODO: investigate performance and potentially find an alternative
            // also make sure calling the yieldhook doesnt count as more instructions and if it does subtract them from instructionsThisUpdate
            commandCoroutine.SetHook(YieldHook, LuaHookMask.Count, 1);
            commandPending = true;
        }

        public bool IsCommandComplete(string commandText)
        {
            var status = commandCoroutine.LoadString(commandText);
            if (status == LuaStatus.ErrSyntax)
            {
                var err = commandCoroutine.ToString(-1);
                return !err.EndsWith("<eof>");
            }
            return true; // TODO: do i need to pop?
        }

        public bool IsWaitingForCommand()
        {
            return commandCoroutine.Status != LuaStatus.Yield;
        }

        public void BreakExecution(bool manual)
        {
            commandCoroutine.Dispose();
            commandCoroutine = state.State.NewThread();
        }

        public int InstructionsThisUpdate()
        {
            //Debug.Log(Shared.Processor.Tag+" instructionsThisUpdate: "+instructionsThisUpdate);
            return instructionsThisUpdate;
        }

        public void KOSFixedUpdate(double dt)
        {
            instructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
            instructionsThisUpdate = 0;
            // resumes the coroutine created by ProcessCommand after it was created and after it yielded due to running out of instructions
            if (commandCoroutine.Status == LuaStatus.Yield || commandPending)
            {
                commandPending = false;
                LuaStatus status = commandCoroutine.Resume(state.State, 0);
                if (status != LuaStatus.OK & status != LuaStatus.Yield)
                {
                    Shared.Logger.Log(new Exception(commandCoroutine.ToString(-1)));
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
