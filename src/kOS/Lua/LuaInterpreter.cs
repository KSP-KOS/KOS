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
        private KeraLua.Lua callbacksCoroutine;
        private readonly Queue<CommandInfo> commandsQueue = new Queue<CommandInfo>();
        private static readonly Dictionary<IntPtr, ExecInfo> stateInfo = new Dictionary<IntPtr, ExecInfo>();
        private static int instructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
        public const string LuaVersion = "5.4";
        public static readonly string[] FilenameExtensions = new string[] { "lua" };
        public string Name => "lua";
        private SharedObjects Shared { get; }
        
        private class CommandInfo
        {
            public string Command;
            public string ChunkName;
            public CommandInfo(string command, string chunkName)
            {
                Command = command;
                ChunkName = chunkName;
            }
        }

        private class ExecInfo
        {
            public int InstructionsThisUpdate = 0;
            public int InstructionsDebt = 0;
            public bool BreakExecution = false;
            public readonly KeraLua.Lua CommandCoroutine;
            public readonly KeraLua.Lua CallbacksCoroutine;
            public readonly SharedObjects Shared;
            public ExecInfo(SharedObjects shared, KeraLua.Lua commandCoroutine, KeraLua.Lua callbacksCoroutine)
            {
                Shared = shared;
                CommandCoroutine = commandCoroutine;
                CallbacksCoroutine = callbacksCoroutine;
            }
        }

        public LuaInterpreter(SharedObjects shared)
        {
            Shared = shared;
        }

        public void Boot()
        {
            Dispose();
            Shared.UpdateHandler.AddFixedObserver(this);
            state = new NLua.Lua();
            commandCoroutine = state.State.NewThread();
            callbacksCoroutine = state.State.NewThread();
            commandCoroutine.SetHook(AfterEveryInstructionHook, LuaHookMask.Count, 1);
            callbacksCoroutine.SetHook(AfterEveryInstructionHook, LuaHookMask.Count, 1);
            stateInfo.Add(state.State.MainThread.Handle, new ExecInfo(Shared, commandCoroutine, callbacksCoroutine));
            
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
                    ProcessCommand(content.String, "boot");
                }
            }
        }

        private static void AfterEveryInstructionHook(IntPtr L, IntPtr ar)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var execInfo = stateInfo[state.MainThread.Handle];
            if (++execInfo.InstructionsThisUpdate >= instructionsPerUpdate || execInfo.BreakExecution 
                || (execInfo.CommandCoroutine.Handle == L && (execInfo.Shared.Cpu as LuaCPU).IsYielding()))
            {
                // it's possible for a C/CSharp function to call lua making a coroutine unable to yield because
                // of the "C-call boundary". If that is the case we increase InstructionsDebt and its paid up on next fixed updates
                if (state.IsYieldable) state.Yield(0);
                else execInfo.InstructionsDebt++;
            }
        }

        public void ProcessCommand(string commandText) => ProcessCommand(commandText, "command");

        private void ProcessCommand(string commandText, string commandName)
        {
            commandsQueue.Enqueue(new CommandInfo(commandText, commandName));
        }

        private bool LoadCommand(CommandInfo commandInfo)
        {
            if ((LuaStatus)commandCoroutine.ResetThread() != LuaStatus.OK || commandCoroutine.LoadString(commandInfo.Command, commandInfo.ChunkName) != LuaStatus.OK)
            {
                var err = commandCoroutine.ToString(-1);
                commandCoroutine.Pop(1);
                DisplayError(err);
                return false;
            }
            return true;
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
            return !(Shared.Cpu as LuaCPU).IsYielding() && callbacksCoroutine.Status != LuaStatus.Yield && commandCoroutine.Status != LuaStatus.Yield;
        }

        public void BreakExecution()
        {
            stateInfo[state.State.MainThread.Handle].BreakExecution = true;
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
            instructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
            execInfo.InstructionsThisUpdate = Math.Min(instructionsPerUpdate, execInfo.InstructionsDebt);
            execInfo.InstructionsDebt -= execInfo.InstructionsThisUpdate;
            if (execInfo.InstructionsThisUpdate >= instructionsPerUpdate) return;
            
            if (execInfo.BreakExecution)
            {   // true after BreakExecution was called, reset thread to prevent execution of the same program
                execInfo.BreakExecution = false;
                commandsQueue.Clear();
                commandCoroutine.ResetThread();
                callbacksCoroutine.ResetThread();
                if (callbacksCoroutine.GetGlobal("onBreakExecution") == LuaType.Function)
                {
                    if (callbacksCoroutine.LoadString("onBreakExecution()", "callback") == LuaStatus.OK)
                    {
                        var status = callbacksCoroutine.Resume(state.State, 0);
                        if (status != LuaStatus.OK && status != LuaStatus.Yield)
                            DisplayError(callbacksCoroutine.ToString(-1));
                    }
                }
            }

            Shared.BindingMgr?.PreUpdate();

            // if onFixedUpdate failed to execute due to running out of instructions we reset and start over.
            // it's up to lua side to figure out how to handle the reset in case it didn't finish the callback
            callbacksCoroutine.ResetThread();
            if (callbacksCoroutine.GetGlobal("onFixedUpdate") == LuaType.Function)
            {
                if (callbacksCoroutine.LoadString("onFixedUpdate()", "callback") == LuaStatus.OK)
                {
                    var status = callbacksCoroutine.Resume(state.State, 0);
                    if (status != LuaStatus.OK && status != LuaStatus.Yield)
                    {
                        DisplayError(callbacksCoroutine.ToString(-1)
                                     +"\nonFixedUpdate function errored and was set to nil."
                                     +"\nTo reset onFixedUpdate do 'onFixedUpdate = _onFixedUpdate'.");
                        callbacksCoroutine.PushNil();
                        callbacksCoroutine.SetGlobal("onFixedUpdate");
                    }
                }
            }
            
            if (execInfo.InstructionsThisUpdate >= instructionsPerUpdate) return;
            if (execInfo.InstructionsThisUpdate >= instructionsPerUpdate || (Shared.Cpu as LuaCPU).IsYielding()) return;


            // resumes the coroutine after it yielded due to running out of instructions
            // and/or executes queued commands until they run out or the coroutine yields
            while (commandCoroutine.Status == LuaStatus.Yield || commandsQueue.Count > 0)
            {
                if (commandCoroutine.Status == LuaStatus.Yield || LoadCommand(commandsQueue.Dequeue()))
                {
                    var status = commandCoroutine.Resume(state.State, 0);
                    if (status == LuaStatus.Yield)
                    {
                        return;
                    }
                    if (status != LuaStatus.OK)
                    {
                        DisplayError(commandCoroutine.ToString(-1));
                        commandCoroutine.ResetThread();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (state == null) return;
            Shared.UpdateHandler.RemoveFixedObserver(this);
            stateInfo.Remove(state.State.MainThread.Handle);
            commandsQueue.Clear();
            Binding.bindings.Remove(state.State.MainThread.Handle);
            state.Dispose();
            state = null;
        }

        public void DisplayError(string errorMessage)
        {
            Shared.Logger.Log("lua error: "+errorMessage);
            Shared.SoundMaker.BeginFileSound("error");
            Shared.Screen.Print(errorMessage);
        }
    }
}
