using System;
using System.Collections.Generic;
using System.Text;
using KeraLua;
using kOS.Safe;
using kOS.Safe.Utilities;
using kOS.Safe.Screen;
using kOS.Safe.Persistence;

namespace kOS.Lua
{
    public class LuaInterpreter : IInterpreter, IFixedUpdateObserver, IUpdateObserver
    {
        public const string LuaVersion = "5.4";
        public static readonly string[] FilenameExtensions = { "lua" };
        public string Name => "lua";
        private SharedObjects Shared { get; }
        private KeraLua.Lua state;
        private KeraLua.Lua commandCoroutine;
        private KeraLua.Lua fixedUpdateCoroutine;
        private KeraLua.Lua updateCoroutine;
        /// Relevant information for the instruction hook meant to be accessed with the lua state handle in a static context 
        private static readonly Dictionary<IntPtr, InstructionHookInfo> stateHookInfo = new Dictionary<IntPtr, InstructionHookInfo>();
        private readonly Queue<CommandInfo> commandsQueue = new Queue<CommandInfo>();
        private int breakExecutionCount;
        private int? idleInstructions;
        
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

        private class InstructionHookInfo
        {
            public int InstructionsPerUpdate;
            public int InstructionsThisUpdate;
            public bool BreakExecution;
            public readonly SharedObjects Shared;
            public InstructionHookInfo(SharedObjects shared)
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
            Dispose();
            Shared.UpdateHandler.AddFixedObserver(this);
            Shared.UpdateHandler.AddObserver(this);
            state = new KeraLua.Lua(false);
            state.Encoding = Encoding.UTF8;
            
            commandCoroutine = state.NewThread();
            fixedUpdateCoroutine = state.NewThread();
            updateCoroutine = state.NewThread();
            commandCoroutine.SetHook(AfterEveryInstructionHook, LuaHookMask.Count, 1);
            fixedUpdateCoroutine.SetHook(AfterEveryInstructionHook, LuaHookMask.Count, 1);
            updateCoroutine.SetHook(AfterEveryInstructionHook, LuaHookMask.Count, 1);
            stateHookInfo.Add(state.MainThread.Handle, new InstructionHookInfo(Shared));
            
            Libraries.Open(state, Shared);
            
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
            var hookInfo = stateHookInfo[state.MainThread.Handle];
            if (++hookInfo.InstructionsThisUpdate >= hookInfo.InstructionsPerUpdate || hookInfo.BreakExecution 
                || (hookInfo.Shared.Cpu as LuaCPU).IsYielding())
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
            return !(Shared.Cpu as LuaCPU).IsYielding() && fixedUpdateCoroutine.Status != LuaStatus.Yield
                                                        && commandCoroutine.Status != LuaStatus.Yield;
        }

        public void BreakExecution()
        {
            if (state == null) return;
            var hookInfo = stateHookInfo[state.MainThread.Handle];
            hookInfo.BreakExecution = true;
            breakExecutionCount++;
        }

        public int InstructionsThisUpdate()
        {
            if (state != null && stateHookInfo.TryGetValue(state.MainThread.Handle, out var hookInfo))
                return hookInfo.InstructionsThisUpdate;
            return 0;
        }

        public int ECInstructionsThisUpdate()
        {
            if (state != null && stateHookInfo.TryGetValue(state.MainThread.Handle, out var hookInfo))
                return Math.Max(hookInfo.InstructionsThisUpdate - idleInstructions ?? 0, 0);
            return 0;
        }

        public void KOSFixedUpdate(double dt)
        {
            (Shared.Cpu as LuaCPU).FixedUpdate();
            Shared.BindingMgr?.PreUpdate();
            
            var hookInfo = stateHookInfo[commandCoroutine.MainThread.Handle];
            hookInfo.InstructionsPerUpdate = SafeHouse.Config.LuaInstructionsPerUpdate;
            hookInfo.InstructionsThisUpdate = 0;
            
            if (hookInfo.BreakExecution)
            {   // true after BreakExecution was called, reset thread to prevent execution of the same program
                hookInfo.BreakExecution = false;
                commandsQueue.Clear();
                commandCoroutine.ResetThread();
                fixedUpdateCoroutine.ResetThread();
                updateCoroutine.ResetThread();
                if (fixedUpdateCoroutine.GetGlobal("breakexecution") == LuaType.Function)
                {
                    var status = fixedUpdateCoroutine.Resume(state, 0);
                    if (status != LuaStatus.OK && status != LuaStatus.Yield)
                    {
                        DisplayError(fixedUpdateCoroutine.ToString(-1), fixedUpdateCoroutine);
                        fixedUpdateCoroutine.Pop(1);
                    }
                }
                else fixedUpdateCoroutine.Pop(1);
            }

            fixedUpdateCoroutine.ResetThread();
            if (fixedUpdateCoroutine.GetGlobal("fixedupdate") == LuaType.Function && !(Shared.Cpu as LuaCPU).IsYielding())
            {
                fixedUpdateCoroutine.PushNumber(dt);
                var status = fixedUpdateCoroutine.Resume(state, 1);
                if (status != LuaStatus.OK && status != LuaStatus.Yield)
                {
                    DisplayError(fixedUpdateCoroutine.ToString(-1)
                                 +"\nfixedupdate function errored and was set to nil."
                                 +"\nTo reset fixedupdate do 'fixedupdate = callbacks.fixedupdate'.", fixedUpdateCoroutine);
                    fixedUpdateCoroutine.Pop(1);
                    fixedUpdateCoroutine.PushNil();
                    fixedUpdateCoroutine.SetGlobal("fixedupdate");
                }
            }
            else fixedUpdateCoroutine.Pop(1);
            
            if (idleInstructions == null)
                idleInstructions = hookInfo.InstructionsThisUpdate;

            if (hookInfo.InstructionsThisUpdate < hookInfo.InstructionsPerUpdate)
                breakExecutionCount = 0;
            
            if (breakExecutionCount >= 3)
            {
                Shared.SoundMaker.BeginFileSound("beep");
                Shared.Screen.Print("Ctrl+C was pressed 3 times while the processor was using all of the available instructions so "+
                                    "fixedupdate function was set to nil. To reset the default function do:\n\"callbacks.init()\".");
                state.PushNil();
                state.SetGlobal("fixedupdate");
                breakExecutionCount = 0;
            }
            
            if (hookInfo.InstructionsThisUpdate < hookInfo.InstructionsPerUpdate && !(Shared.Cpu as LuaCPU).IsYielding())
            {
                // resumes the coroutine after it yielded due to running out of instructions
                // and/or executes queued commands until they run out or the coroutine yields
                while (commandCoroutine.Status == LuaStatus.Yield || commandsQueue.Count > 0)
                {
                    if (commandCoroutine.Status == LuaStatus.Yield || LoadCommand(commandsQueue.Dequeue()))
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
        }

        public void KOSUpdate(double dt)
        {
            if (dt == 0) return; // don't run when the game is paused
            var hookInfo = stateHookInfo[commandCoroutine.MainThread.Handle];
            
            if (hookInfo.InstructionsThisUpdate >= hookInfo.InstructionsPerUpdate) return;
            
            updateCoroutine.ResetThread();
            if (updateCoroutine.GetGlobal("update") == LuaType.Function && !(Shared.Cpu as LuaCPU).IsYielding())
            {
                updateCoroutine.PushNumber(dt);
                var status = updateCoroutine.Resume(state, 1);
                if (status != LuaStatus.OK && status != LuaStatus.Yield)
                {
                    DisplayError(updateCoroutine.ToString(-1)
                                 +"\nupdate function errored and was set to nil."
                                 +"\nTo reset update do 'update = callbacks.update'.", updateCoroutine);
                    updateCoroutine.Pop(1);
                    updateCoroutine.PushNil();
                    updateCoroutine.SetGlobal("update");
                }
            }
            else updateCoroutine.Pop(1);
        }

        public void Dispose()
        {
            if (state == null) return;
            Shared.UpdateHandler.RemoveFixedObserver(this);
            Shared.UpdateHandler.RemoveObserver(this);
            var stateHandle = state.MainThread.Handle;
            state.Dispose();
            stateHookInfo.Remove(stateHandle);
            Binding.Bindings.Remove(stateHandle);
            state = null;
            commandsQueue.Clear();
            breakExecutionCount = 0;
            idleInstructions = null;
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
