using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using KeraLua;
using kOS.Safe;
using kOS.Safe.Utilities;
using kOS.Safe.Screen;
using kOS.Safe.Persistence;

namespace kOS.Lua
{
    public class LuaInterpreter : IInterpreter, IFixedUpdateObserver, IUpdateObserver
    {
        private KeraLua.Lua state;
        private KeraLua.Lua commandCoroutine;
        private KeraLua.Lua fixedUpdateCoroutine;
        private KeraLua.Lua updateCoroutine;
        private static readonly Dictionary<IntPtr, ExecInfo> stateInfo = new Dictionary<IntPtr, ExecInfo>();
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
            public readonly Queue<CommandInfo> CommandsQueue = new Queue<CommandInfo>();
            public int InstructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
            public int InstructionsThisUpdate = 0;
            public bool BreakExecution = false;
            public int BreakExecutionCount = 0;
            public readonly KeraLua.Lua CommandCoroutine;
            public readonly SharedObjects Shared;
            public ExecInfo(SharedObjects shared, KeraLua.Lua commandCoroutine)
            {
                Shared = shared;
                CommandCoroutine = commandCoroutine;
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
            Shared.UpdateHandler.AddObserver(this);
            state = new KeraLua.Lua(false);
            state.Encoding = Encoding.UTF8;
            
            state.RequireF("_G", LibraryOpenMethods.luaopen_base, true);
            state.RequireF("coroutine", LibraryOpenMethods.luaopen_coroutine, true);
            state.RequireF("string", LibraryOpenMethods.luaopen_string, true);
            state.RequireF("utf8", LibraryOpenMethods.luaopen_utf8, true);
            state.RequireF("table", LibraryOpenMethods.luaopen_table, true);
            state.RequireF("math", LibraryOpenMethods.luaopen_math, true);
            
            commandCoroutine = state.NewThread();
            fixedUpdateCoroutine = state.NewThread();
            updateCoroutine = state.NewThread();
            commandCoroutine.SetHook(AfterEveryInstructionHook, LuaHookMask.Count, 1);
            fixedUpdateCoroutine.SetHook(AfterEveryInstructionHook, LuaHookMask.Count, 1);
            updateCoroutine.SetHook(AfterEveryInstructionHook, LuaHookMask.Count, 1);
            stateInfo.Add(state.MainThread.Handle, new ExecInfo(Shared, commandCoroutine));
            
            using (var streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("kOS.Lua.init.lua"))) {
                ProcessCommand(streamReader.ReadToEnd(), "init");
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
                var file = Shared.VolumeMgr.CurrentVolume.Open(path) as VolumeFile;
                if (file == null)
                {
                    SafeHouse.Logger.Log(string.Format("Boot file \"{0}\" is missing, skipping boot script", path));
                }
                else
                {
                    ProcessCommand($"dofile(\"{file.Path}\")", "boot");
                }
            }
        }

        private static void AfterEveryInstructionHook(IntPtr L, IntPtr ar)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var execInfo = stateInfo[state.MainThread.Handle];
            if (++execInfo.InstructionsThisUpdate >= execInfo.InstructionsPerUpdate || execInfo.BreakExecution 
                || (execInfo.CommandCoroutine.Handle == L && (execInfo.Shared.Cpu as LuaCPU).IsYielding()))
            {
                // it's possible for a C/CSharp function to call lua making a coroutine unable to yield because
                // of the "C-call boundary".
                if (state.IsYieldable)
                    state.Yield(0);
            }
        }

        public void ProcessCommand(string commandText) => ProcessCommand(commandText, "command");

        private void ProcessCommand(string commandText, string commandName)
        {
            if (state == null) return;
            var execInfo = stateInfo[state.MainThread.Handle];
            execInfo.CommandsQueue.Enqueue(new CommandInfo(commandText, commandName));
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
            return !(Shared.Cpu as LuaCPU).IsYielding() && fixedUpdateCoroutine.Status != LuaStatus.Yield
                                                        && updateCoroutine.Status != LuaStatus.Yield
                                                        && commandCoroutine.Status != LuaStatus.Yield;
        }

        public void BreakExecution()
        {
            if (state == null) return;
            var execInfo = stateInfo[state.MainThread.Handle];
            execInfo.BreakExecution = true;
            execInfo.BreakExecutionCount++;
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
            Shared.BindingMgr?.PreUpdate();
            
            // run commands with remaining instructions from previous onFixedUpdate, onUpdate callbacks
            // reset InstructionsThisUpdate
            // run onFixedUpdate callback
            // if KOSUpdate got called run onUpdate callback

            var execInfo = stateInfo[commandCoroutine.MainThread.Handle];
            execInfo.InstructionsPerUpdate = SafeHouse.Config.LuaInstructionsPerUpdate;
            
            if (fixedUpdateCoroutine.Status != LuaStatus.Yield && updateCoroutine.Status != LuaStatus.Yield)
                execInfo.BreakExecutionCount = 0;
            
            if (execInfo.BreakExecutionCount >= 3)
            {
                Shared.SoundMaker.BeginFileSound("beep");
                Shared.Screen.Print("Ctrl+C was pressed 3 times while the processor was using all of the available instructions so "+
                                    "update callbacks were set to nil. To reset callbacks do:\n\"setUpdateCallbacks()\".");
                state.PushNil();
                state.SetGlobal("onFixedUpdate");
                state.PushNil();
                state.SetGlobal("onUpdate");
                execInfo.BreakExecutionCount = 0;
            }
            
            // running commands here but resetting InstructionsThisUpdate after, so commands have the lowest priority
            // here InstructionsThisUpdate is more like InstructionsThatUpdate
            if (execInfo.InstructionsThisUpdate < execInfo.InstructionsPerUpdate && !(Shared.Cpu as LuaCPU).IsYielding())
            {
                // resumes the coroutine after it yielded due to running out of instructions
                // and/or executes queued commands until they run out or the coroutine yields
                while (commandCoroutine.Status == LuaStatus.Yield || execInfo.CommandsQueue.Count > 0)
                {
                    if (commandCoroutine.Status == LuaStatus.Yield || LoadCommand(execInfo.CommandsQueue.Dequeue()))
                    {
                        var status = commandCoroutine.Resume(state, 0);
                        if (status == LuaStatus.Yield) break;
                        if (status != LuaStatus.OK)
                        {
                            DisplayError(commandCoroutine.ToString(-1), commandCoroutine);
                            commandCoroutine.ResetThread();
                        }
                    }
                }
            }

            execInfo.InstructionsThisUpdate = 0;
            
            if (execInfo.BreakExecution)
            {   // true after BreakExecution was called, reset thread to prevent execution of the same program
                execInfo.BreakExecution = false;
                execInfo.CommandsQueue.Clear();
                commandCoroutine.ResetThread();
                fixedUpdateCoroutine.ResetThread();
                updateCoroutine.ResetThread();
                if (fixedUpdateCoroutine.GetGlobal("onBreakExecution") == LuaType.Function)
                {
                    if (fixedUpdateCoroutine.LoadString("onBreakExecution()", "breakExecution") == LuaStatus.OK)
                    {
                        var status = fixedUpdateCoroutine.Resume(state, 0);
                        if (status != LuaStatus.OK && status != LuaStatus.Yield)
                            DisplayError(fixedUpdateCoroutine.ToString(-1), fixedUpdateCoroutine);
                    }
                }
            }

            // if onFixedUpdate failed to execute due to running out of instructions we reset and start over.
            // it's up to lua side to figure out how to handle the reset in case it didn't finish the callback
            fixedUpdateCoroutine.ResetThread();
            if (fixedUpdateCoroutine.GetGlobal("onFixedUpdate") == LuaType.Function)
            {
                if (fixedUpdateCoroutine.LoadString($"onFixedUpdate({dt})", "fixedUpdate") == LuaStatus.OK)
                {
                    var status = fixedUpdateCoroutine.Resume(state, 0);
                    if (status != LuaStatus.OK && status != LuaStatus.Yield)
                    {
                        DisplayError(fixedUpdateCoroutine.ToString(-1)
                                     +"\nonFixedUpdate function errored and was set to nil."
                                     +"\nTo reset onFixedUpdate do 'onFixedUpdate = _onFixedUpdate'.", fixedUpdateCoroutine);
                        fixedUpdateCoroutine.PushNil();
                        fixedUpdateCoroutine.SetGlobal("onFixedUpdate");
                    }
                }
            }
        }

        public void KOSUpdate(double dt)
        {
            if (dt == 0) return; // don't run when the game is paused
            var execInfo = stateInfo[commandCoroutine.MainThread.Handle];
            
            if (execInfo.InstructionsThisUpdate >= execInfo.InstructionsPerUpdate) return;
            
            updateCoroutine.ResetThread();
            if (updateCoroutine.GetGlobal("onUpdate") == LuaType.Function)
            {
                if (updateCoroutine.LoadString($"onUpdate({dt})", "update") == LuaStatus.OK)
                {
                    var status = updateCoroutine.Resume(state, 0);
                    if (status != LuaStatus.OK && status != LuaStatus.Yield)
                    {
                        DisplayError(updateCoroutine.ToString(-1)
                                     +"\nonUpdate function errored and was set to nil."
                                     +"\nTo reset onUpdate do 'onUpdate = _onUpdate'.", updateCoroutine);
                        updateCoroutine.PushNil();
                        updateCoroutine.SetGlobal("onUpdate");
                    }
                }
            }
        }

        public void Dispose()
        {
            if (state == null) return;
            Shared.UpdateHandler.RemoveFixedObserver(this);
            Shared.UpdateHandler.RemoveObserver(this);
            var stateHandle = state.MainThread.Handle;
            state.Dispose();
            stateInfo.Remove(stateHandle);
            Binding.bindings.Remove(stateHandle);
            state = null;
        }

        private void DisplayError(string errorMessage, KeraLua.Lua state = null)
        {
            if (state != null)
            {
                state.Traceback(state);
                errorMessage += "\n" + state.ToString(-1);
            }
            Shared.Logger.Log("lua error: "+errorMessage);
            Shared.SoundMaker.BeginFileSound("error");
            Shared.Screen.Print(errorMessage);
        }
    }
}
