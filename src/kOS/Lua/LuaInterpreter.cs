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
using kOS.Safe.Persistence;

namespace kOS.Lua
{
    public class LuaInterpreter : IInterpreter, IFixedUpdateObserver
    {
        private NLua.Lua state;
        private KeraLua.Lua commandCoroutine;
        private bool commandPending = false;
        private static readonly Dictionary<IntPtr, ExecInfo> stateInfo = new Dictionary<IntPtr, ExecInfo>();
        private static int instructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
        public const string LuaVersion = "5.4";
        public static readonly string[] FilenameExtensions = new string[] { "lua" };
        public string Name => "lua";
        private SharedObjects Shared { get; }

        private class ExecInfo
        {
            public int InstructionsThisUpdate = 0;
            public bool StopExecution = false;
            public SharedObjects Shared;
            public ExecInfo(SharedObjects shared)
            {
                Shared = shared;
            }
        }

        public LuaInterpreter(SharedObjects shared)
        {
            Shared = shared;
        }

        public void Boot()
        {
            Shared.UpdateHandler.AddFixedObserver(this);
            if (state != null)
            {
                stateInfo.Remove(state.State.MainThread.Handle);
                Binding.DisposeStateBinding(state.State);
                state.Dispose();
            }
            state = new NLua.Lua();
            commandCoroutine = state.State.NewThread();
            commandCoroutine.SetHook(AfterEveryInstructionHook, LuaHookMask.Count, 1);
            stateInfo.Add(commandCoroutine.MainThread.Handle, new ExecInfo(Shared));
            
            using (var streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("kOS.Lua.init.lua"))) {
                try { state.DoString(streamReader.ReadToEnd()); }
                catch (Exception e) { Debug.Log(e); DisplayError(e.Message); }
            }

            Binding.BindToState(commandCoroutine, Shared);

            if (!Shared.Processor.CheckCanBoot()) return;

            VolumePath path = Shared.Processor.BootFilePath;
            // Check to make sure the boot file name is valid, and then that the boot file exists.
            if (path == null)
            {
                SafeHouse.Logger.Log("Boot file name is empty, skipping boot script");
            }
            else
            {
                // Boot is only called once right after turning the processor on,
                // the volume cannot yet have been changed from that set based on
                // Config.StartOnArchive, and Processor.CheckCanBoot() has already
                // handled the range check for the archive.
                Volume sourceVolume = Shared.VolumeMgr.CurrentVolume;
                var file = Shared.VolumeMgr.CurrentVolume.Open(path) as VolumeFile;
                if (file == null)
                {
                    SafeHouse.Logger.Log(string.Format("Boot file \"{0}\" is missing, skipping boot script", path));
                }
                else
                {
                    var content = file.ReadAll();
                    if (content == null)
                    {
                        DisplayError(string.Format("File '{0}' not found", path));
                        return;
                    }
                    ProcessCommand(content.String);
                }
            }
        }

        private static void AfterEveryInstructionHook(IntPtr L, IntPtr ar)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var execInfo = stateInfo[state.MainThread.Handle];
            if (++execInfo.InstructionsThisUpdate >= instructionsPerUpdate || execInfo.StopExecution || (execInfo.Shared.Cpu as LuaCPU).IsYielding())
            {
                state.Yield(0);
            }
        }

        public void ProcessCommand(string commandText)
        {
            if ((LuaStatus)commandCoroutine.ResetThread() != LuaStatus.OK || commandCoroutine.LoadString(commandText, "command") != LuaStatus.OK)
            {
                var err = commandCoroutine.ToString(-1);
                commandCoroutine.Pop(1);
                DisplayError(err);
            } else
            {
                commandPending = true;
            }
        }

        public bool IsCommandComplete(string commandText)
        {
            if (commandCoroutine.LoadString(commandText) == LuaStatus.ErrSyntax)
            {
                var err = commandCoroutine.ToString(-1);
                commandCoroutine.Pop(1);
                // if the error is not caused by leaving stuff like do('"{ open let it go to ProcessCommand to be displayed to the user
                return !err.EndsWith("<eof>");
            }
            commandCoroutine.Pop(1);
            return true;
        }

        public bool IsWaitingForCommand()
        {
            return !(Shared.Cpu as LuaCPU).IsYielding() && commandCoroutine.Status != LuaStatus.Yield;
        }

        public void StopExecution()
        {
            stateInfo[commandCoroutine.MainThread.Handle].StopExecution = true;
        }

        public int InstructionsThisUpdate()
        {   // ProcessElectricity() calls this after changing interpreter when stuff is not initialized yet
            if (commandCoroutine != null && stateInfo.TryGetValue(commandCoroutine.MainThread.Handle, out var info))
                return info.InstructionsThisUpdate;
            return 0;
        }

        public void KOSFixedUpdate(double dt)
        {
            (Shared.Cpu as LuaCPU).FixedUpdate();
            
            var execInfo = stateInfo[commandCoroutine.MainThread.Handle];
            if (execInfo.StopExecution)
            {   // true after StopExecution was called, reset thread to prevent execution of the same program
                execInfo.StopExecution = false;
                // sometimes Terminal sends an empty string to ProcessCommand() after you ctrl+c during execution.
                // This ignores commands that are sent in the same tick that StopExecution() was called
                commandPending = false;
                commandCoroutine.ResetThread();
            }
            instructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
            execInfo.InstructionsThisUpdate = 0;

            Shared.BindingMgr?.PreUpdate();

            // resumes the coroutine after it yielded due to running out of instructions or after ProcessCommand loaded a new command
            if (!(Shared.Cpu as LuaCPU).IsYielding() && (commandCoroutine.Status == LuaStatus.Yield || commandPending))
            {
                commandPending = false;
                LuaStatus status = commandCoroutine.Resume(state.State, 0);
                if (status != LuaStatus.OK & status != LuaStatus.Yield)
                {
                    commandCoroutine.ResetThread();
                    DisplayError(commandCoroutine.ToString(-1));
                }
            }
        }

        public void Dispose()
        {
            StopExecution();
            state.Dispose();
            Shared.UpdateHandler.RemoveFixedObserver(this);
        }

        public void DisplayError(string errorMessage)
        {
            Shared.Logger.Log("lua error: "+errorMessage);
            Shared.SoundMaker.BeginFileSound("error");
            Shared.Screen.Print(errorMessage);
        }
    }
}
