using kOS.Safe.Binding;
using kOS.Safe.Callback;
using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Debug = kOS.Safe.Utilities.Debug;
using kOS.Safe.Persistence;

namespace kOS.Safe.Execution
{
    public class CPU : ICpu
    {
        private struct PopContextNotifyeeContainer : IEquatable<PopContextNotifyeeContainer>
        {
            public readonly WeakReference popContextNotifyee;
            private int innerHashCode;

            public PopContextNotifyeeContainer(IPopContextNotifyee notifyee)
            {
                popContextNotifyee = new WeakReference(notifyee);
                innerHashCode = notifyee.GetHashCode();
            }
            public override int GetHashCode()
            {
                return innerHashCode;
            }

            public override bool Equals(object other)
            {
                if (other is PopContextNotifyeeContainer)
                    return Equals((PopContextNotifyeeContainer)other);
                return false;
            }
            public bool Equals(PopContextNotifyeeContainer other)
            {
                if (popContextNotifyee.Target == null)
                    return false;

                if (other.popContextNotifyee.Target == null)
                    return false;

                return popContextNotifyee.Target == other.popContextNotifyee.Target;
            }
        }

        private readonly IStack stack;
        private readonly VariableScope globalVariables;


        private class YieldFinishedWithPriority
        {
            public YieldFinishedDetector detector;
            public InterruptPriority priority;
        }
        private List<YieldFinishedWithPriority> yields;

        private double currentTime;
        private readonly SafeSharedObjects shared;
        private readonly List<ProgramContext> contexts;
        private ProgramContext currentContext;
        private VariableScope savedPointers;
        private int instructionsPerUpdate;

        public int InstructionsThisUpdate { get; private set; }

        // statistics
        private double totalCompileTime;
        private Stopwatch instructionWatch = new Stopwatch();
        private Stopwatch updateWatch = new Stopwatch();
        private Stopwatch executionWatch = new Stopwatch();
        private Stopwatch compileWatch = new Stopwatch();
        private readonly StringBuilder executeLog = new StringBuilder();

        private Dictionary<InterruptPriority, ExecutionStatBlock> executionStats = new Dictionary<InterruptPriority, ExecutionStatBlock>();

        public int InstructionPointer
        {
            get { return currentContext.InstructionPointer; }
            set { currentContext.InstructionPointer = value; }
        }
        public InterruptPriority CurrentPriority
        {
            get { return currentContext.CurrentPriority; }
            set { currentContext.CurrentPriority = value; }
        }
        public int NextTriggerInstanceId
        {
            get { return currentContext.NextTriggerInstanceId; }
        }

        public double SessionTime { get { return currentTime; } }
        
        public List<string> ProfileResult { get; private set; }

        /// <summary>
        /// It's quite bad to abort the PopContext activity partway through while the CPU is
        /// trying to clean up from a program crash or break, so this advertises when that's the case
        /// so other parts of the system can decide not to use exceptions when in this fragile state:
        /// </summary>
        /// <value><c>true</c> if this CPU is popping context; otherwise, <c>false</c>.</value>
        public bool IsPoppingContext { get; private set; }

        /// <summary>
        /// The objects which have chosen to register themselves as IPopContextNotifyees
        /// to be told when popping a context (ending a program).
        /// </summary>
        private HashSet<PopContextNotifyeeContainer> popContextNotifyees;
        private int popContextNotifyeesCleanupCounter = 0;

        public CPU(SafeSharedObjects shared)
        {
            this.shared = shared;
            this.shared.Cpu = this;
            stack = new Stack();
            globalVariables = new VariableScope(0, null);
            contexts = new List<ProgramContext>();
            yields = new List<YieldFinishedWithPriority>();
            if (this.shared.UpdateHandler != null) this.shared.UpdateHandler.AddFixedObserver(this);
            popContextNotifyees = new HashSet<PopContextNotifyeeContainer>();
        }

        public void Boot()
        {
            // break all running programs
            currentContext = null;
            contexts.Clear();            
            if (shared.GameEventDispatchManager != null) shared.GameEventDispatchManager.Clear();
            PushInterpreterContext();
            CurrentPriority = InterruptPriority.Normal;
            currentTime = 0;
            // clear stack (which also orphans all local variables so they can get garbage collected)
            stack.Clear();
            // clear global variables
            globalVariables.Clear();
            // clear interpreter
            if (shared.Interpreter != null) shared.Interpreter.Reset();
            // load functions
            if (shared.FunctionManager != null) shared.FunctionManager.Load();
            // load bindings
            if (shared.BindingMgr != null) shared.BindingMgr.Load();

            // Booting message
            if (shared.Screen != null)
            {
                shared.Screen.ClearScreen();
                string bootMessage = string.Format("kOS Operating System\n" + "KerboScript v{0}\n(manual at {1})\n \n" + "Proceed.\n",
                                                   SafeHouse.Version, SafeHouse.DocumentationURL);
                List<string> nags = Debug.GetPendingNags();
                if (nags.Count > 0)
                {
                    bootMessage +=
                        "##################################################\n" +
                        "#               NAG MESSAGES                     #\n" +
                        "##################################################\n" +
                        "# Further details about these important messages #\n" +
                        "# can be found in the KSP error log.  If you see #\n" +
                        "# this, then read the error log.  It's important.#\n" +
                        "--------------------------------------------------\n";

                    bootMessage = nags.Aggregate(bootMessage, (current, msg) => current + (msg + "\n"));
                    bootMessage += "##################################################\n";
                    shared.Processor.SetMode(Module.ProcessorModes.OFF);
                }
                shared.Screen.Print(bootMessage);
            }

            if (!shared.Processor.CheckCanBoot()) return;

            VolumePath path = shared.Processor.BootFilePath;
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
                Volume sourceVolume = shared.VolumeMgr.CurrentVolume;
                var file = shared.VolumeMgr.CurrentVolume.Open(path);
                if (file == null)
                {
                    SafeHouse.Logger.Log(string.Format("Boot file \"{0}\" is missing, skipping boot script", path));
                }
                else
                {
                    var bootContext = "program";
                    shared.ScriptHandler.ClearContext(bootContext);
                    IProgramContext programContext = SwitchToProgramContext();
                    programContext.Silent = true;

                    string bootCommand = string.Format("run \"{0}\".", file.Path);

                    var options = new CompilerOptions
                    {
                        LoadProgramsInSameAddressSpace = true,
                        FuncManager = shared.FunctionManager,
                        BindManager = shared.BindingMgr,
                        AllowClobberBuiltins = SafeHouse.Config.AllowClobberBuiltIns,
                        IsCalledFromRun = false
                    };

                    YieldProgram(YieldFinishedCompile.RunScript(new BootGlobalPath(bootCommand), 1, bootCommand, bootContext, options));
                    
                }
            }
        }

        private void PushInterpreterContext()
        {
            var interpreterContext = new ProgramContext(true);
            // initialize the context with an empty program
            interpreterContext.AddParts(new List<CodePart>());
            PushContext(interpreterContext);
        }

        private void PushContext(ProgramContext context)
        {
            SafeHouse.Logger.Log("Pushing context staring with: " + context.GetCodeFragment(0).FirstOrDefault());
            SaveAndClearPointers();
            contexts.Add(context);
            currentContext = contexts.Last();
            CurrentPriority = InterruptPriority.Normal;
            shared.GameEventDispatchManager.SetDispatcherFor(currentContext);

            if (contexts.Count > 1)
            {
                shared.Interpreter.SetInputLock(true);
            }
        }

        private void PopContext()
        {
            SafeHouse.Logger.Log("Popping context " + contexts.Count);
            IsPoppingContext = true;
            if (contexts.Any())
            {
                // remove the last context
                ProgramContext contextRemove = contexts.Last();
                NotifyPopContextNotifyees(contextRemove);
                contextRemove.ClearTriggers();
                contexts.Remove(contextRemove);
                shared.GameEventDispatchManager.RemoveDispatcherFor(currentContext);
                contextRemove.DisableActiveFlyByWire(shared.BindingMgr);
                SafeHouse.Logger.Log("Removed Context " + contextRemove.GetCodeFragment(0).FirstOrDefault());

                if (contexts.Any())
                {
                    currentContext = contexts.Last();
                    shared.GameEventDispatchManager.SetDispatcherFor(currentContext);
                    currentContext.EnableActiveFlyByWire(shared.BindingMgr);
                    RestorePointers();
                    SafeHouse.Logger.Log("New current context " + currentContext.GetCodeFragment(0).FirstOrDefault());
                }
                else
                {
                    currentContext = null;
                    shared.GameEventDispatchManager.Clear();
                }

                if (contexts.Count == 1)
                {
                    shared.Interpreter.SetInputLock(false);
                }
            }
            IsPoppingContext = false;
        }

        /// <summary>
        /// Used when an object wants the CPU to call its OnPopContext() callback
        /// whenever the CPU ends a program context.  Notice that the CPU will
        /// only use a WEAK refernece to store this, so that registering yourself
        /// as a notifyee here does not stop you from being orphaned and garbage
        /// collected if you would normally do so.  It's only if your object happens
        /// to still be alive when the program context ends that you'll get called
        /// by the CPU.  Use this if you have some important cleanup work you'd like
        /// to do when the program dies, or if your object is one that would become
        /// useless and invalid when the program context ends so you need to shut
        /// yourself down when that happens.
        /// </summary>
        /// <param name="notifyee">Notifyee object that has an OnPopContext() callback.</param>
        public void AddPopContextNotifyee(IPopContextNotifyee notifyee)
        {
            PopContextNotifyeeContainer container = new PopContextNotifyeeContainer(notifyee);
            popContextNotifyees.Add(container);

            popContextNotifyeesCleanupCounter++;
            if (popContextNotifyeesCleanupCounter > 10000)
            {
                popContextNotifyeesCleanupCounter = 0;
                popContextNotifyees = new HashSet<PopContextNotifyeeContainer>(popContextNotifyees.Where((c) => c.popContextNotifyee.IsAlive && c.popContextNotifyee.Target != null));
            }
        }

        public void RemovePopContextNotifyee(IPopContextNotifyee notifyee)
        {
            PopContextNotifyeeContainer container = new PopContextNotifyeeContainer(notifyee);
            popContextNotifyees.Remove(container);
        }

        private void NotifyPopContextNotifyees(IProgramContext context)
        {
            // Notify them all:
            foreach (PopContextNotifyeeContainer container in popContextNotifyees)
            {
                WeakReference current = container.popContextNotifyee;
                if (current.IsAlive) // Avoid resurrecting it if it's gone, and don't call its hook.
                {
                    IPopContextNotifyee notifyee = current.Target as IPopContextNotifyee;
                    if (!notifyee.OnPopContext(context))
                        current.Target = null; // mark for removal below, because the notifyee wants us to
                }
            }

            // Remove the ones flagged for removal or that are stale anyway:
            popContextNotifyees.RemoveWhere((c) => !c.popContextNotifyee.IsAlive || c.popContextNotifyee.Target == null);
        }

        public void PushNewScope(Int16 scopeId, Int16 parentScopeId)
        {
            VariableScope parentScope = parentScopeId == 0 ? globalVariables : stack.FindScope(parentScopeId);
            stack.PushScope(new VariableScope(scopeId, parentScope));
        }

        /// <summary>
        /// Push a single thing onto the scope stack.  If it's a context record it will encode the current
        /// priority into it as the ComeFromPriority.
        /// </summary>
        public void PushScopeStack(object thing)
        {
            SubroutineContext context = thing as SubroutineContext;
            if (context != null)
                context.CameFromPriority = CurrentPriority;
            stack.PushScope(thing);
        }

        /// <summary>
        /// Pop one or more things from the scope stack, only returning the
        /// finalmost thing popped.  (i.e if you pop 3 things then you get:
        /// pop once and throw away, pop again and throw away, pop again and return the popped thing.)
        /// </summary>
        public object PopScopeStack(int howMany)
        {
            object returnVal = new int(); // bogus return val if given a bogus "pop zero things" request.
            while (howMany > 0)
            {
                returnVal = stack.PopScope();
                SubroutineContext context = returnVal as SubroutineContext;
                if (context != null)
                    CurrentPriority = context.CameFromPriority;
                --howMany;
            }

            return returnVal;
        }

        /// <summary>
        /// Find the closest-to-top subroutine context and return its
        /// CameFromPriority.  Returns current priority if not in a subroutine.
        /// </summary>
        /// <returns></returns>
        private InterruptPriority CurrentCameFromPriority()
        {
            bool done = false;
            for (int depth = 0; !done; ++depth)
            {
                object stackItem = stack.PeekScope(depth);
                if (stackItem == null)
                {
                    done = true;
                }
                else
                {
                    SubroutineContext context = stackItem as SubroutineContext;
                    if (context != null)
                        return context.CameFromPriority;
                }
            }
            return CurrentPriority; // fallback if none found.
        }

        private void PopFirstContext()
        {
            while (contexts.Count > 1)
            {
                PopContext();
            }
        }

        /// <summary>
        /// Build a clone of the current state of the scope stack, for the sake of capturing a closure.
        /// </summary>
        /// <returns>A stripped down copy of the stack with just the relevant closure frames in it.</returns>
        public List<VariableScope> GetCurrentClosure()
        {
            var closureList = new List<VariableScope>();

            var currentScope = GetCurrentScope();
            while (currentScope != null)
            {
                // The closure's variable scopes need to be marked as such, so the
                // 'popscope' opcode knows to pop them off in one go when it hits
                // them on the stack:
                currentScope.IsClosure = true;
                closureList.Add(currentScope);

                currentScope = currentScope.ParentScope;
            }

            return closureList;
        }

        public IProgramContext GetCurrentContext()
        {
            return currentContext;
        }

        /// <summary>
        /// Build a delegate call for the given function entry point, in which it will capture a closure of the current
        /// runtime scoping state to be used when that function gets called later by OpcodeCall:
        /// </summary>
        /// <param name="entryPoint">Integer location in memory to jump to to start the call</param>
        /// <param name="withClosure">Should the closure be captured for this delegate or ignored</param>
        /// <returns>The delegate object you can store in a variable.</returns>
        public IUserDelegate MakeUserDelegate(int entryPoint, bool withClosure)
        {
            return new UserDelegate(this, currentContext, entryPoint, withClosure);
        }

        // only two contexts exist now, one for the interpreter and one for the programs
        public IProgramContext GetInterpreterContext()
        {
            return contexts[0];
        }

        public IProgramContext SwitchToProgramContext()
        {
            if (contexts.Count == 1)
            {
                PushContext(new ProgramContext(false));
            }
            return currentContext;
        }

        public Opcode GetCurrentOpcode()
        {
            return currentContext.Program[currentContext.InstructionPointer];
        }

        public Opcode GetOpcodeAt(int instructionPtr)
        {
            if (instructionPtr < 0 || instructionPtr >= currentContext.Program.Count)
            {
                return new OpcodeBogus();
            }
            return currentContext.Program[instructionPtr];
        }

        private void SaveAndClearPointers()
        {
            // Any global variable that ends in an asterisk (*) is a system pointer
            // that shouldn't be inherited by other program contexts.  These sorts of
            // variables should only exist for the current program context.
            // This method stashes all such variables in a storage area for the program
            // context, then clears them.  The stash can be used later by RestorePointers()
            // to bring them back into existence when coming back to this program context again.
            // Pointer variables include:
            //   IP jump location for subprograms.
            //   IP jump location for functions.
            savedPointers = new VariableScope(0, null);
            var pointers = new List<KeyValuePair<string, Variable>>(globalVariables.Locals.Where(entry => StringUtil.EndsWith(entry.Key, "*")));

            foreach (var entry in pointers)
            {
                savedPointers.Add(entry.Key, entry.Value);
                globalVariables.Remove(entry.Key);
            }
            SafeHouse.Logger.Log(string.Format("Saving and removing {0} pointers", pointers.Count));
        }

        private void RestorePointers()
        {
            // Pointer variables that were stashed by SaveAndClearPointers() get brought
            // back again by this method when returning to the previous programming
            // programming context.

            var restoredPointers = 0;
            var deletedPointers = 0;

            foreach (KeyValuePair<string, Variable> item in savedPointers.Locals)
            {
                if (globalVariables.Contains(item.Key))
                {
                    // If the pointer exists it means it was redefined from inside a program
                    // and it's going to be invalid outside of it, so just to be sure, remove
                    // it entirely in preparation for restoring the old one:
                    globalVariables.Remove(item.Key);
                    deletedPointers++;
                }
                globalVariables.Add(item.Key, item.Value);
                restoredPointers++;
            }

            SafeHouse.Logger.Log(string.Format("Deleting {0} pointers and restoring {1} pointers", deletedPointers, restoredPointers));
        }

        public void RunProgram(List<Opcode> program)
        {
            RunProgram(program, false);
        }

        public void RunProgram(List<Opcode> program, bool silent)
        {
            if (!program.Any()) return;

            var newContext = new ProgramContext(false, program) { Silent = silent };
            PushContext(newContext);
        }

        public void BreakExecution(bool manual)
        {
            SafeHouse.Logger.Log(string.Format("Breaking Execution {0} Contexts: {1}", manual ? "Manually" : "Automatically", contexts.Count));
            if (contexts.Count == 0)
            {
                // Skip most of what this method does, since there's no execution to break.
                // This case should only be posisble if BreakExecution() is called while the
                // CPU is off or power starved, as can happen during OnLoad().
            }
            else if (contexts.Count > 1)
            {
                AbortAllYields();

                if (SafeHouse.Config.ShowStatistics)
                    CalculateProfileResult();

                if (manual)
                {
                    PopFirstContext();
                    shared.Screen.Print("Program aborted.");
                    shared.SoundMaker.StopAllVoices(); // stop voices if execution was manually broken, but not if the program ends normally
                    PrintStatistics();
                    stack.Clear();
                }
                else
                {
                    bool silent = currentContext.Silent;
                    PopContext();
                    if (contexts.Count == 1 && !silent)
                    {
                        shared.Screen.Print("Program ended.");
                        PrintStatistics();
                        stack.Clear();
                    }
                }
                CurrentPriority = InterruptPriority.Normal;
            }
            else
            {
                if (manual)
                    currentContext.ClearTriggers(); // Removes the interpreter's triggers on Control-C and the like, but not on errors.
                SkipCurrentInstructionId();
                CurrentPriority = InterruptPriority.Normal;
            }
            ResetStatistics();
        }

        /// <summary>
        /// Call when you want to suspend execution of the Opcodes until some
        /// future condition becomes true.  The CPU will call yieldTracker.Begin()
        /// right away, and then after that call yieldTracker.IsFinished() again and
        /// again until it returns true.  Until IsFinished() returns true, the CPU
        /// will not advance any further into the program in its current "mode".
        /// Note that the CPU will track "trigger" and "mainline" code separately for
        /// this purpose.  Waiting in mainline code will still allow triggers to run.
        /// </summary>
        /// <param name="yieldTracker"></param>
        public void YieldProgram(YieldFinishedDetector yieldTracker)
        {
            yields.Add(new YieldFinishedWithPriority() { detector = yieldTracker, priority = CurrentPriority });
            yieldTracker.creationTimeStamp = currentTime;
            yieldTracker.Begin(shared);
        }

        private bool IsYielding()
        {
            int numStillBlocking = 0;

            // Iterating backward because items will be getting deleted as we
            // iterate over the list and this way the index doesn't become wrong
            // as we do so.  (also, those deletions are why foreach() isn't used here):
            for (int i = yields.Count - 1; i >= 0; --i)
            {
                YieldFinishedWithPriority yielder = yields[i];
                // A yield blockage that is blocking mainline code while
                // we are in a trigger isn't relevant.  Only check the ones
                // that are blocking current priority or higher:
                if (yielder.priority >= CurrentPriority)
                {
                    YieldFinishedDetector detector = yielder.detector;
                    if (detector.creationTimeStamp != currentTime && detector.IsFinished())
                        yields.RemoveAt(i);
                    else
                        ++numStillBlocking;
                }
            }
            return (numStillBlocking > 0);
        }
        
        private void AbortAllYields()
        {
            yields.Clear();
        }
        
        public void PushArgumentStack(object item)
        {
            stack.PushArgument(item);
        }

        public object PopArgumentStack()
        {
            return stack.PopArgument();
        }

        /// <summary>Throw exception if the user delegate is not one the CPU can call right now.</summary>
        /// <param name="userDelegate">The userdelegate being checked</param>
        /// <exception cref="KOSInvalidDelegateContextException">thrown if the cpu is in a state where it can't call this delegate.</exception>
        public void AssertValidDelegateCall(IUserDelegate userDelegate)
        {
            if (userDelegate.ProgContext != currentContext)
            {
                string currentContextName;
                if (currentContext == contexts[0])
                {
                    currentContextName = "the interpreter";
                }
                else
                {
                    currentContextName = "a program";
                }

                string delegateContextName;
                if (userDelegate.ProgContext == contexts[0])
                {
                    delegateContextName = "the interpreter";
                }
                else if (currentContext == contexts[0])
                {
                    delegateContextName = "a program";
                }
                else
                {
                    delegateContextName = "a different program from a previous run";
                }

                throw new KOSInvalidDelegateContextException(currentContextName, delegateContextName);
           }
        }

        /// <summary>
        /// Allow kOS code to lower the CPU priority (only the CPU is allowed to raise
        /// it via its interrupts system, but code is allowed to lower it if it wants).
        /// The new priority will be equal to whatever the priority was of the code
        /// that got interrupted to get here.  (If priority 10 code gets interrupted
        /// by priority 20 code, and that priority 20 code calls PrevPriority(), then
        /// it will drop to priority 10 because that was the priority of whomever got
        /// interrupted to get here.)
        /// </summary>
        public void DropBackPriority()
        {
            CurrentPriority = CurrentCameFromPriority();
        }

        /// <summary>
        /// Return the subroutine call trace of how the code got to where it is right now.
        /// </summary>
        /// <returns>The first item in the list is the current instruction pointer.
        /// The rest of the items in the list after that are the instruction pointers of the Opcodecall instructions
        /// that got us to here.</returns>
        public List<int> GetCallTrace()
        {
            List<int> trace = stack.GetCallTrace();
            trace.Insert(0, currentContext.InstructionPointer); // perpend current IP
            return trace;
        }

        /// <summary>
        /// Get the value of a variable or create it at global scope if not found.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private Variable GetOrCreateVariable(string identifier)
        {
            Variable variable = GetVariable(identifier, false, true);
            if (variable == null)
            {
                variable = new Variable { Name = identifier };
                AddVariable(variable, identifier, false);
            }
            return variable;
        }

        /// <summary>
        /// Test if an identifier is a variable you can get the value of
        /// at the moment (var name exists and is in scope).  Return
        /// true if you can, false if you can't.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public bool IdentifierExistsInScope(string identifier)
        {
            Variable dummyVal = GetVariable(identifier, false, true);
            return (dummyVal != null);
        }

        public string DumpVariables()
        {
            var msg = new StringBuilder();
            msg.AppendLine("============== STACK VARIABLES ===============");
            DumpStack();
            msg.AppendLine("============== GLOBAL VARIABLES ==============");
            foreach (var entry in globalVariables.Locals)
            {
                string line;
                try
                {
                    line = entry.Key;
                    var v = entry.Value;
                    if (v == null || v.Value == null)
                        line += " is <null>";
                    else
                        line += " is a " + v.Value.GetType().FullName + " with value = " + v.Value;
                }
                catch (Exception e)
                {
                    // This is necessary because of the deprecation exceptions that
                    // get raised by FlightStats when you try to print all of them out:
                    line = entry.Key + " is <value caused exception>\n    " + e.Message;
                }
                msg.AppendLine(line);
            }
            SafeHouse.Logger.Log(msg.ToString());
            return "Variable dump is in the output log";
        }

        public string DumpStack()
        {
            return stack.Dump();
        }

        private VariableScope GetCurrentScope()
        {
            VariableScope currentScope = stack.GetCurrentScope();
            if (currentScope == null)
            {
                currentScope = globalVariables;
            }
            return currentScope;
        }

        public SubroutineContext GetCurrentSubroutineContext()
        {
            return stack.GetCurrentSubroutineContext();
        }

        /// <summary>
        /// Find any trigger call contexts that are on the callstack to be executed
        /// that match the given trigger.
        /// </summary>
        /// <returns>List of matching trigger call contexts.  Zero length if none.</returns>
        /// <param name="trigger">Trigger.</param>
        public List<SubroutineContext> GetTriggerCallContexts(TriggerInfo trigger)
        {
            return stack.GetTriggerCallContexts(trigger);
        }

        /// <summary>
        /// Get the variable's contents, performing a lookup through all nesting levels
        /// up to global.
        /// </summary>
        /// <param name="identifier">variable to look for</param>
        /// <param name="barewordOkay">Is it acceptable for the variable to
        ///   not exist, in which case its bare name will be returned as the value.</param>
        /// <param name="failOkay">Is it acceptable for the variable to
        ///   not exist, in which case a null will be returned as the value.</param>
        /// <returns>the value that was found</returns>
        private Variable GetVariable(string identifier, bool barewordOkay = false, bool failOkay = false)
        {
            identifier = identifier.ToLower();
            Variable value = GetCurrentScope().GetNested(identifier);
            if (value != null)
            {
                return value;
            }

            if (barewordOkay)
            {
                string strippedIdent = identifier.TrimStart('$');
                return new Variable { Name = strippedIdent, Value = new StringValue(strippedIdent) };
            }
            if (failOkay)
                return null;
            // In the case where we were looking for a function pointer but didn't find one, and would
            // have failed with exception, then it's still acceptable to find a hit that isn't a function
            // pointer (has no trailing asterisk '*') but only if it's a delegate of some sort:
            if (StringUtil.EndsWith(identifier, "*"))
            {
                string trimmedTail = identifier.TrimEnd('*');
                Variable retryVal = GetVariable(trimmedTail, barewordOkay, failOkay);
                string trimmedLeader = trimmedTail.TrimStart('$');
                if (retryVal.Value is KOSDelegate)
                    return retryVal;
                throw new KOSNotInvokableException(trimmedLeader);
            }
            throw new KOSUndefinedIdentifierException(identifier.TrimStart('$'), "");
        }

        /// <summary>
        /// Make a new variable at either the local depth or the
        /// global depth depending.
        /// throws exception if it already exists as a boundvariable at the desired
        /// scope level, unless overwrite = true.
        /// </summary>
        /// <param name="variable">variable to add</param>
        /// <param name="identifier">name of variable to add</param>
        /// <param name="local">true if you want to make it at local depth</param>
        /// <param name="overwrite">true if it's okay to overwrite an existing variable</param>
        public void AddVariable(Variable variable, string identifier, bool local, bool overwrite = false)
        {
            if (!StringUtil.StartsWith(identifier, "$"))
            {
                identifier = "$" + identifier;
            }

            VariableScope currentScope = local ? GetCurrentScope() : globalVariables;

            Variable existing = currentScope.GetLocal(identifier);

            if (existing != null)
            {
                if (existing.Value is BoundVariable)
                    if (!overwrite)
                        throw new KOSIdentiferClashException(identifier);
                currentScope.Remove(identifier);
            }

            currentScope.Add(identifier, variable);
        }

        public bool VariableIsRemovable(Variable variable)
        {
            return !(variable is BoundVariable);
        }

        /// <summary>
        /// Removes a variable, following current scoping rules, removing
        /// the innermost scope of the variable that is found.<br/>
        /// <br/>
        /// If the variable cannot be found, it fails silently without complaint.
        /// </summary>
        /// <param name="identifier">varible to remove.</param>
        public void RemoveVariable(string identifier)
        {
            VariableScope currentScope = GetCurrentScope();
            Variable variable = currentScope.RemoveNestedUserVar(identifier);
            if (variable != null)
            {
                // Tell Variable to orphan its old value now.  Faster than relying
                // on waiting several seconds for GC to eventually call ~Variable()
                variable.Value = null;
            }
        }

        /// <summary>
        ///   Given a value which may or may not be a variable name, return the value
        ///   back.  If it's not a variable, return it as-is.  If it's a variable,
        ///   look it up and return that.
        /// </summary>
        /// <param name="testValue">the object which might be a variable name</param>
        /// <param name="barewordOkay">
        ///   Is this a case in which it's acceptable for the
        ///   variable not to exist, and if it doesn't exist then the variable name itself
        ///   is the value?
        /// </param>
        /// <returns>The value after the steps described have been performed.</returns>
        public object GetValue(object testValue, bool barewordOkay = false)
        {
            // $cos     cos named variable
            // cos()    cos trigonometric function
            // cos      string literal "cos"

            // If it's a variable, meaning it starts with "$" but
            // does NOT have a value like $<.....>, which are special
            // flags used internally:
            var identifier = testValue as string;
            if (identifier == null ||
                identifier.Length <= 1 ||
                identifier[0] != '$' ||
                identifier[1] == '<')
            {
                return testValue;
            }

            Variable variable = GetVariable(identifier, barewordOkay);
            return variable.Value;
        }

        /// <summary>
        /// Try to make a new local variable at the localmost scoping level and
        /// give it a starting value.  It errors out of there is already one there
        /// by the same name.<br/>
        /// <br/>
        /// This does NOT scan up the scoping stack like SetValue() does.
        /// It operates at the local level only.<br/>
        /// <br/>
        /// This is the normal way to make a new local variable.  You cannot make a
        /// local variable without attempting to give it a value.
        /// </summary>
        /// <param name="identifier">variable name to attempt to store into</param>
        /// <param name="value">value to put into it</param>
        public void SetNewLocal(string identifier, object value)
        {
            VariableScope currentScope = GetCurrentScope();

            Variable variable = currentScope.GetLocal(identifier);
            if (variable == null)
            {
                variable = new Variable { Name = identifier };
                AddVariable(variable, identifier, true);
            }
            variable.Value = value;
        }

        /// <summary>
        /// Make a new global variable at the localmost scoping level and
        /// give it a starting value, or overwrite an existing variable
        /// at the localmost level with a starting value.<br/>
        /// <br/>
        /// This does NOT scan up the scoping stack like SetValue() does.
        /// It operates at the global level only.<br/>
        /// </summary>
        /// <param name="identifier">variable name to attempt to store into</param>
        /// <param name="value">value to put into it</param>
        public void SetGlobal(string identifier, object value)
        {
            // Attempt to get it as a global.  Make a new one if it's not found.
            // This preserves the "bound-ness" of the variable if it's a
            // BoundVariable, whereas unconditionally making a new Variable wouldn't:
            Variable variable = globalVariables.GetLocal(identifier);
            if (variable == null)
            {
                variable = new Variable { Name = identifier };
                AddVariable(variable, identifier, false, true);
            }
            variable.Value = value;
        }

        /// <summary>
        /// Try to set the value of the identifier at the localmost
        /// level possible, by scanning up the scope stack to find
        /// the local-most level at which the identifier is a variable,
        /// and assigning it the value there.<br/>
        /// <br/>
        /// If no such value is found, all the way up to the global level,
        /// then it resorts to making a global variable with the name and using that.<br/>
        /// <br/>
        /// This is the normal way to make a new global variable.  You cannot make a
        /// global variable without attempting to give it a value.
        /// </summary>
        /// <param name="identifier">variable name to attempt to store into</param>
        /// <param name="value">value to put into it</param>
        public void SetValue(string identifier, object value)
        {
            Variable variable = GetOrCreateVariable(identifier);
            variable.Value = value;
        }

        /// <summary>
        /// Try to set the value of the identifier at the localmost
        /// level possible, by scanning up the scope stack to find
        /// the local-most level at which the identifier is a variable,
        /// and assigning it the value there.<br/>
        /// <br/>
        /// If no such value is found, an error is thrown.  It only stores into
        /// variables that already exist, refusing to create new variables.<br/>
        /// <br/>
        /// </summary>
        /// <param name="identifier">variable name to attempt to store into</param>
        /// <param name="value">value to put into it</param>
        public void SetValueExists(string identifier, object value)
        {
            Variable variable = GetVariable(identifier);
            variable.Value = value;
        }

        /// <summary>
        /// Pop a value off the argument stack, and if it's a variable name then get its value,
        /// else just return it as it is.
        /// </summary>
        /// <param name="barewordOkay">Is this a context in which it's acceptable for
        ///   a variable not existing error to occur (in which case the identifier itself
        ///   should therefore become a string object returned)?</param>
        /// <returns>value off the stack</returns>
        public object PopValueArgument(bool barewordOkay = false)
        {
            return GetValue(PopArgumentStack(), barewordOkay);
        }

        /// <summary>
        /// Peek at a value atop the argument stack without popping it, and if it's a variable name then get its value,
        /// else just return it as it is.<br/>
        /// <br/>
        /// NOTE: Evaluating variables when you don't really need to is pointlessly expensive, as it
        /// needs to walk the scoping stack to exhaust a search.  If you don't need to evaluate variables,
        /// then consider using PeekRaw() instead.<br/>
        /// </summary>
        /// <param name="digDepth">Peek at the element this far down the stack (0 means top, 1 means just under the top, etc)</param>
        /// <param name="barewordOkay">Is this a context in which it's acceptable for
        ///   a variable not existing error to occur (in which case the identifier itself
        ///   should therefore become a string object returned)?</param>
        /// <returns>value off the stack</returns>
        public object PeekValueArgument(int digDepth, bool barewordOkay = false)
        {
            return GetValue(stack.PeekArgument(digDepth), barewordOkay);
        }

        /// <summary>
        /// Identical to PopValue(), except that it guarantees the return value is either already a Structure,
        /// or it converts it into a Structure if it's a primitive.
        /// It bombs out with an exception if it can't be converted thusly.
        /// <br/>
        /// Use this in places where the stack value *must* come out as an encapsulated value and something
        /// has gone seriously wrong if it can't.  This applies to cases where you are attempting to store
        /// its value inside a user's variable, mostly.
        /// </summary>
        /// <param name="barewordOkay">Is this a context in which it's acceptable for
        ///   a variable not existing error to occur (in which case the identifier itself
        ///   should therefore become a string object returned)?</param>
        /// <returns>value off the stack</returns>
        public Structure PopStructureEncapsulatedArgument(bool barewordOkay = false)
        {
            return Structure.FromPrimitiveWithAssert( PopValueArgument(barewordOkay) );
        }

        /// <summary>
        /// Identical to PeekValue(), except that it guarantees the return value is either already a Structure,
        /// or it converts it into a Structure if it's a primitive.
        /// It bombs out with an exception if it can't be converted thusly.
        /// <br/>
        /// Use this in places where the stack value *must* come out as an encapsulated value and something
        /// has gone seriously wrong if it can't.  This applies to cases where you are attempting to store
        /// its value inside a user's variable, mostly.
        /// </summary>
        /// <param name="digDepth">Peek at the element this far down the stack (0 means top, 1 means just under the top, etc)</param>
        /// <param name="barewordOkay">Is this a context in which it's acceptable for
        ///   a variable not existing error to occur (in which case the identifier itself
        ///   should therefore become a string object returned)?</param>
        /// <returns>value off the stack</returns>
        public Structure PeekStructureEncapsulatedArgument(int digDepth, bool barewordOkay = false)
        {
            return Structure.FromPrimitiveWithAssert(PeekValueArgument(digDepth, barewordOkay));
        }

        /// <summary>
        /// Identical to GetValue(), except that it guarantees the return value is either already a Structure,
        /// or it converts it into a Structure if it's a primitive.
        /// It bombs out with an exception if it can't be converted thusly.
        /// <br/>
        /// Use this in places where the stack value *must* come out as an encapsulated value and something
        /// has gone seriously wrong if it can't.  This applies to cases where you are attempting to store
        /// its value inside another user's variable, mostly.
        /// <br/>
        /// Hypothetically this should never really be required, as the value is coming FROM a user varible
        /// in the first place.
        /// </summary>
        /// <param name="testValue">the object which might be a variable name</param>
        /// <param name="barewordOkay">
        ///   Is this a case in which it's acceptable for the
        ///   variable not to exist, and if it doesn't exist then the variable name itself
        ///   is the value?
        /// </param>
        /// <returns>The value after the steps described have been performed.</returns>
        public Structure GetStructureEncapsulatedArgument(Structure testValue, bool barewordOkay = false)
        {
            return Structure.FromPrimitiveWithAssert(GetValue(testValue, barewordOkay));
        }

        /// <summary>
        /// Identical to PopStructureEncapsulated(), except that it doesn't complain if the
        /// result can't be converted to a Structure.  It's acceptable for it to not be
        /// a Structure, in which case the original object is returned as-is.
        /// <br/>
        /// Use this in places where the stack value *should* come out as an encapsulated value if it can,
        /// but there are some valid cases where it might not be.
        /// </summary>
        /// <param name="barewordOkay">Is this a context in which it's acceptable for
        ///   a variable not existing error to occur (in which case the identifier itself
        ///   should therefore become a string object returned)?</param>
        /// <returns>value off the stack</returns>
        public object PopValueEncapsulatedArgument(bool barewordOkay = false)
        {
            return Structure.FromPrimitive( PopValueArgument(barewordOkay) );
        }

        /// <summary>
        /// Identical to PeekStructureEncapsulated(), except that it doesn't complain if the
        /// result can't be converted to a Structure.  It's acceptable for it to not be
        /// a Structure, in which case the original object is returned as-is.
        /// <br/>
        /// Use this in places where the stack value *should* come out as an encapsulated value if it can,
        /// but there are some valid cases where it might not be.
        /// </summary>
        /// <param name="digDepth">Peek at the element this far down the stack (0 means top, 1 means just under the top, etc)</param>
        /// <param name="barewordOkay">Is this a context in which it's acceptable for
        ///   a variable not existing error to occur (in which case the identifier itself
        ///   should therefore become a string object returned)?</param>
        /// <returns>value off the stack</returns>
        public object PeekValueEncapsulatedArgument(int digDepth, bool barewordOkay = false)
        {
            return Structure.FromPrimitive(PeekValueArgument(digDepth, barewordOkay));
        }

        /// <summary>
        /// Peek at a value atop the argument stack without popping it, and without evaluating it to get the variable's
        /// value.  (i.e. if the thing in the stack is $foo, and the variable foo has value 5, you'll get the string
        /// "$foo" returned, not the integer 5).
        /// </summary>
        /// <param name="digDepth">Peek at the element this far down the stack (0 means top, 1 means just under the top, etc)</param>
        /// <param name="checkOkay">Tells you whether or not the stack was exhausted.  If it's false, then the peek went too deep.</param>
        /// <returns>value off the stack</returns>
        public object PeekRawArgument(int digDepth, out bool checkOkay)
        {
            object returnValue;
            checkOkay = stack.PeekCheckArgument(digDepth, out returnValue);
            return returnValue;
        }

        /// <summary>
        /// Peek at a value atop the scope stack without popping it.
        /// </summary>
        /// <param name="digDepth">Peek at the element this far down the stack (0 means top, 1 means just under the top, etc)</param>
        /// <param name="checkOkay">Tells you whether or not the stack was exhausted.  If it's false, then the peek went too deep.</param>
        /// <returns>value off the stack</returns>
        public object PeekRawScope(int digDepth, out bool checkOkay)
        {
            object returnValue;
            checkOkay = stack.PeekCheckScope(digDepth, out returnValue);
            return returnValue;
        }

        public int GetArgumentStackSize()
        {
            return stack.GetArgumentStackSize();
        }


        /// <summary>
        /// Schedules a trigger function call to occur soon.  There are two ways you can
        /// do this: (A) On the next Cpu.KOSFixedUpdate() before it will happen,
        /// or (B) When the next Opcode was about to execute.  This is decided by the 'immediate' argument.
        /// If multiple such function calls get inserted between ticks, they will behave like
        /// a nested stack of function calls.  Mainline code will not continue until all such
        /// functions have finished at least once.  This type of trigger must be a function that
        /// takes zero parameters and returns a BooleanValue.  If its return is true, it will be
        /// automatically scheduled to run again by being re-inserted with a new AddTrigger()
        /// when it finishes.  If its return is false, it won't fire off again.
        /// </summary>
        /// <param name="triggerFunctionPointer">The entry point of this trigger function.</param>
        /// <param name="priority">The priority that this trigger will interrupt with.</param> 
        /// <param name="instanceId">pass in TriggerInfo.NextInstance if you desire the ability for
        /// more than one instance of a trigger to exist for this same triggerFunctionPointer.  Pass
        /// a zero to indicate you want to prevent multiple instances of triggers from this same
        /// entry point to be invokable.</param> 
        /// <param name="immediate">Trigger should happen immediately on next opcode instead of waiting till next fixeupdate</parem>
        /// <param name="closure">The closure the trigger should be called with.  If this is
        /// null, then the trigger will only be able to see global variables reliably.</param>
        /// <returns>A TriggerInfo structure describing this new trigger, which probably isn't very useful
        /// tp the caller in most circumstances where this is a fire-and-forget trigger.</returns>
        public TriggerInfo AddTrigger(int triggerFunctionPointer, InterruptPriority priority, int instanceId, bool immediate, List<VariableScope> closure)
        {
            TriggerInfo triggerRef = new TriggerInfo(currentContext, triggerFunctionPointer, priority, instanceId, closure);
            if (immediate)
                currentContext.AddImmediateTrigger(triggerRef);
            else
                currentContext.AddPendingTrigger(triggerRef);
            return triggerRef;
        }

        /// <summary>
        /// Schedules a trigger function call to occur soon.  There are two ways you can
        /// do this: (A) On the next Cpu.KOSFixedUpdate() before it will happen,
        /// or (B) When the next Opcode was about to execute.  This is decided by the 'immediate' argument.
        /// If multiple such function calls get inserted between ticks, they will behave like
        /// a nested stack of function calls.  Mainline code will not continue until all such
        /// functions have finished at least once.<br/>
        /// <br/>
        /// This type of trigger must be a UserDelegate
        /// function which was created using the CPU's current Program Contect.  If it was created
        /// using a different program context that the one that is currently executing (i.e. if it's
        /// a delegate from a program that has ended now), then this method will refuse to insert it
        /// and it won't run.  In this case a null TriggerInfo will be returned.<br/>
        /// <br/>
        /// For "fire and forget" callback hook functions that "return void" and you don't care about
        /// their return value, you can just ignore whether or not the AddTrigger worked and not care
        /// about the cases where it silently fails because the program got aborted.  The fact that the
        /// callback won't execute only matters when you were expecting to read its return value.
        /// </summary>
        /// <param name="del">A UserDelegate that was created using the CPU's current program context.</param>
        /// <param name="priority">The priority that this trigger will interrupt with.</param> 
        /// <param name="instanceId">pass in TriggerInfo.NextInstance if you desire the ability for
        /// more than one instance of a trigger to exist for this same triggerFunctionPointer.  Pass
        /// a zero to indicate you want to prevent multiple instances of triggers from this same
        /// entry point to be invokable.</param> 
        /// <param name="immediate">Trigger should happen immediately on next opcode instead of waiting till next fixeupdate</parem>
        /// <param name="args">The list of arguments to pass to the UserDelegate when it gets called.</param>
        /// <returns>A TriggerInfo structure describing this new trigger.  It can be used to monitor
        /// the progress of the function call: To see if it has had a chance to finish executing yet,
        /// and to see what its return value was if it has finished.  Will be null if the UserDelegate was
        /// for an "illegal" program context.  Null returns are used instead of throwing an exception
        /// because this condition is expected to occur often when a program just ended that had callback hooks
        /// in it.</returns>
        public TriggerInfo AddTrigger(UserDelegate del, InterruptPriority priority, int instanceId, bool immediate, List<Structure> args)
        {
            if (del.ProgContext != currentContext)
                return null;
            TriggerInfo callbackRef = new TriggerInfo(currentContext, del.EntryPoint, priority, instanceId, del.Closure, del.GetMergedArgs(args));
            if (immediate)
                currentContext.AddImmediateTrigger(callbackRef);
            else
                currentContext.AddPendingTrigger(callbackRef);
            return callbackRef;
        }

        /// <summary>
        /// Schedules a trigger function call to occur soon.  There are two ways you can
        /// do this: (A) On the next Cpu.KOSFixedUpdate() before it will happen,
        /// or (B) When the next Opcode was about to execute.  This is decided by the 'immediate' argument.
        /// If multiple such function calls get inserted between ticks, they will behave like
        /// a nested stack of function calls.  Mainline code will not continue until all such
        /// functions have finished at least once.<br/>
        /// <br/>
        /// This type of trigger must be a UserDelegate
        /// function which was created using the CPU's current Program Contect.  If it was created
        /// using a different program context that the one that is currently executing (i.e. if it's
        /// a delegate from a program that has ended now), then this method will refuse to insert it
        /// and it won't run.  In this case a null TriggerInfo will be returned.
        /// <br/>
        /// For "fire and forget" callback hook functions that "return void" and you don't care about
        /// their return value, you can just ignore whether or not the AddTrigger worked and not care
        /// about the cases where it silently fails because the program got aborted.  The fact that the
        /// callback won't execute only matters when you were expecting to read its return value.
        /// </summary>
        /// <param name="del">A UserDelegate that was created using the CPU's current program context.</param>
        /// <param name="priority">The priority that this trigger will interrupt with.</param> 
        /// <param name="instanceID">pass in TriggerInfo.NextInstance if you desire the ability for
        /// more than one instance of a trigger to exist for this same UserDelegate.  Pass
        /// a zero to indicate you want to prevent multiple instances of triggers from this same
        /// Delegate to be invokable.</param> 
        /// <param name="immediate">Trigger should happen immediately on next opcode instead of waiting till next fixeupdate</parem>
        /// <param name="args">A parms list of arguments to pass to the UserDelegate when it gets called.</param>
        /// <returns>A TriggerInfo structure describing this new trigger.  It can be used to monitor
        /// the progress of the function call: To see if it has had a chance to finish executing yet,
        /// and to see what its return value was if it has finished.  Will be null if the UserDelegate was
        /// for an "illegal" program context.  Null returns are used instead of throwing an exception
        /// because this condition is expected to occur often when a program is ended that had callback hooks
        /// in it.</returns>
        public TriggerInfo AddTrigger(UserDelegate del, InterruptPriority priority, int instanceId, bool immediate, params Structure[] args)
        {
            if (del.ProgContext != currentContext)
                return null;
            TriggerInfo triggerRef = AddTrigger(del, priority, instanceId, immediate, new List<Structure>(args));
            return triggerRef;
        }

        /// <summary>
        /// Schedules a trigger function call to occur soon.  There are two ways you can
        /// do this: (A) On the next Cpu.KOSFixedUpdate() before it will happen,
        /// or (B) When the next Opcode was about to execute.  This is decided by the 'immediate' argument.
        /// If multiple such function calls get inserted between ticks, they will behave like
        /// a nested stack of function calls.  Mainline code will not continue until all such
        /// functions have finished at least once.<br/>
        /// <br/>
        /// This is used for cases where you already built a TriggerInfo yourself and are inserting it,
        /// or have a handle on a TriggerInfo you got as a return from a previous AddTrigger() call and
        /// want to re-insert it to schedule another call.<br/>
        /// If the TriggerInfo you pass in was built for a different ProgramContext than the one that
        /// is currently running, then this will return null and refuse to do anything.
        /// </summary>
        /// <param name="immediate">Trigger should happen immediately on next opcode instead of waiting till next fixeupdate</parem>
        /// <returns>To be in agreement with how the other AddTrigger() methods work, this returns
        /// a TriggerInfo which is just the same one you passed in.  It will return a null, however,
        /// in cases where the TriggerInfo you passed in is for a different ProgramContext.</returns>
        public TriggerInfo AddTrigger(TriggerInfo trigger, bool immediate)
        {
            if (trigger.ContextId != currentContext.ContextId)
                return null;
            if (immediate)
                currentContext.AddImmediateTrigger(trigger);
            else
                currentContext.AddPendingTrigger(trigger);
            return trigger;
        }

        /// <summary>
        /// Removes a trigger looking like this if one exists.
        /// </summary>
        /// <param name="triggerFunctionPointer">Trigger's entry point (instruction pointer)</param>
        /// <param name="instanceId">If nonzero, only remove the trigger if it has this Id.  If zero, 
        /// then remove all triggers with this entry point, regardless of their instance Id.</param>
        public void RemoveTrigger(int triggerFunctionPointer, int instanceId)
        {
            currentContext.RemoveTrigger(new TriggerInfo(currentContext, triggerFunctionPointer, 0/*dummy*/, instanceId, null));
        }

        public void RemoveTrigger(TriggerInfo trigger)
        {
            currentContext.RemoveTrigger(trigger);
        }

        /// <summary>
        /// Cancels any pending calls to triggers that match the criteria.
        /// </summary>
        /// <param name="triggerFunctionPointer">Trigger's entry point (instruction pointer)</param>
        /// <param name="instanceId">If nonzero, only affect the trigger if it has this Id.  If zero, 
        /// then affect all triggers with this entry point, regardless of their instance Id.</param>
        public void CancelCalledTriggers(int triggerFunctionPointer, int instanceId)
        {
            CancelCalledTriggers(new TriggerInfo(currentContext, triggerFunctionPointer, 0 /*dummy*/, instanceId, null));
        }

        public void CancelCalledTriggers(TriggerInfo trigger)
        {
            // Inform any already existing calls to the trigger that they should cancel themselves
            // if they support the cancellation logic (if they pay attention to OpcodeTestCancelled).
            List<SubroutineContext> calls = GetTriggerCallContexts(trigger);
            for (int i = 0; i < calls.Count; ++i)
            {
                calls[i].Cancel();
            }
        }

        public void KOSFixedUpdate(double deltaTime)
        {
            bool showStatistics = SafeHouse.Config.ShowStatistics;

            // If the script changes config value, it doesn't take effect until next update:
            instructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
            InstructionsThisUpdate = 0;
                
            updateWatch.Reset();
            executionWatch.Reset();
            instructionWatch.Reset();
            if (!compileWatch.IsRunning)
            {
                compileWatch.Reset();
            }
            updateWatch.Start();

            currentTime = shared.UpdateHandler.CurrentFixedTime;

            try
            {
                PreUpdateBindings();

                if (currentContext != null && currentContext.Program != null)
                {
                    if (showStatistics)
                    {
                        executionWatch.Start();
                        instructionWatch.Start();
                    }
                    ContinueExecution(showStatistics);
                    if (showStatistics)
                    {
                        executionWatch.Stop();
                    }
                }
                if (showStatistics)
                {
                }

                PostUpdateBindings();
            }
            catch (Exception e)
            {
                if (shared.Logger != null)
                {
                    shared.Logger.Log(e);
                    SafeHouse.Logger.Log(stack.Dump());
                }
                if (shared.SoundMaker != null)
                {
                    // Stop all voices any time there is an error, both at the interpreter and in a program
                    shared.SoundMaker.StopAllVoices();
                }

                if (contexts.Count == 1)
                {
                    // interpreter context
                    SkipCurrentInstructionId();
                    stack.Clear(); // Get rid of this interpreter command's cruft.

                    // If it threw exception during a trigger with higher priority (like lock steering) before
                    // reaching its OpcodeReturn, it's important to drop the interpreter context's priority
                    // back down so interrupts will work correctly again.  Unlike with a *Program*, with the
                    // interpreter we're re-using the same programcontext after the crash:
                    CurrentPriority = InterruptPriority.Normal;
                }
                else
                {
                    // break execution of all programs and pop interpreter context
                    PopFirstContext();
                    stack.Clear(); // If breaking all execution, get rid of the cruft here too.
                }
            }
            updateWatch.Stop();

            foreach (ExecutionStatBlock statBlock in executionStats.Values)
                statBlock.EndOneUpdate();
        }

        private void PreUpdateBindings()
        {
            if (shared.BindingMgr != null)
            {
                shared.BindingMgr.PreUpdate();
            }
        }

        private void PostUpdateBindings()
        {
            if (shared.BindingMgr != null)
            {
                shared.BindingMgr.PostUpdate();
            }
        }

        private void ProcessTriggers()
        {
            if (currentContext.ActiveTriggerCount() <= 0) return;
            int oldCount = currentContext.Program.Count;

            int currentInstructionPointer = currentContext.InstructionPointer;
            var triggersToBeExecuted = new List<TriggerInfo>();

            // Recurring triggers tend to only get put on the callstack one at a time because
            // when they are the same priority they won't enter the callstack till the
            // previous one is done.  Therefore even though this is going on a stack, it
            // still is getting inserted in LIFO order:
            for (int index = 0 ; index < currentContext.ActiveTriggerCount() ; ++index)
            {
                TriggerInfo trigger = currentContext.GetTriggerByIndex(index);

                // If the program is ended from within a trigger, the trigger list will be empty and the pointer
                // will be invalid.  Only execute the trigger if it still exists, AND if it's of a higher priority
                // than the current CPU priority level.  (If it's the same or less priority as the curent CPU priority,
                // then leave it in the list to be added later once we return back to a lower priority that allows it.)
                if (currentContext.ContainsTrigger(trigger) && (trigger.Priority == InterruptPriority.NoChange || trigger.Priority > CurrentPriority))
                {
                    if (trigger is NoDelegate)
                    {
                        // Don't bother calling it.  Just declare it to be "done" with its default value.
                        trigger.FinishCallback(new ScalarIntValue(0));
                        // hypothetically this case shouldn't happen because our own code shouldn't
                        // be adding triggers for the NoDelegate.  This is a fallback case to not
                        // blow up when we forget to do that check.
                    }
                    else
                    {
                        // Insert a faked function call as if the trigger had been called from just
                        // before whatever opcode was about to get executed, by pusing a context
                        // record like OpcodeCall would do, and moving the IP to the
                        // first line of the trigger, like OpcodeCall would do.
                        SubroutineContext contextRecord =
                            new SubroutineContext(currentInstructionPointer, trigger);
                        PushScopeStack(contextRecord);

                        // Push the closure's scope record, if there is one, just after the function return context got put on the stack.
                        if (trigger.Closure != null)
                            for (int i = trigger.Closure.Count - 1 ; i >= 0 ; --i)
                                PushScopeStack(trigger.Closure[i]);

                        PushArgumentStack(new KOSArgMarkerType());

                        if (trigger.IsCSharpCallback)
                            for (int argIndex = trigger.Args.Count - 1; argIndex >= 0 ; --argIndex)
                                PushArgumentStack(trigger.Args[argIndex]);

                        triggersToBeExecuted.Add(trigger);

                        currentInstructionPointer = trigger.EntryPoint;

                        // elevate priority to the priority of the trigger:
                        if (trigger.Priority != InterruptPriority.NoChange)
                            CurrentPriority = trigger.Priority;

                        // Triggers can chain in this loop if more than one fire off at once - the second trigger
                        // will look like it was a function that was called from the start of the first trigger.
                        // The third trigger will look like a function that was called from the start of the second, etc.
                    }
                }
            }
            
            // Remove all triggers that will fire.  Any trigger that wants to
            // re-enable itself will do so by returning a boolean true, which
            // will tell OpcodeReturn that it needs to re-add the trigger.
            foreach (TriggerInfo trigger in triggersToBeExecuted)
            {
                RemoveTrigger(trigger);
            }

            currentContext.InstructionPointer = currentInstructionPointer;
        }

        private void ContinueExecution(bool doProfiling)
        {
            var executeNext = true;
            int howManyNormalPriority = 0;

            executeLog.Remove(0, executeLog.Length); // In .net 2.0, StringBuilder had no Clear(), which is what this is simulating.
            while (InstructionsThisUpdate < instructionsPerUpdate &&
                   executeNext &&
                   currentContext != null)
            {
                // ProcessTriggers is in this loop because opcodes can result in changes that
                // cause callbacks to be invoked, and this can make those callback invocations
                // happen immediately on the next opcode:
                ProcessTriggers();

                if (IsYielding())
                {
                    executeNext = false;
                }
                else
                {
                    ++InstructionsThisUpdate;
                    executeNext = ExecuteInstruction(currentContext, doProfiling);
                    if (CurrentPriority == InterruptPriority.Normal)
                        ++howManyNormalPriority;
                }
            }
            // Do this once more after the loop, just in case the very last Opcode in the update
            // caused a callback that was meant to occur immediately in the current priority level
            // using InterruptPriority.NoChange.  It's important to get that call pushed onto the
            // callstack now at the current priority, before the next update might escalate the
            // priority with a trigger.
            ProcessTriggers();

            currentContext.ActivatePendingTriggersAbovePriority(CurrentPriority);

            if (executeLog.Length > 0)
                SafeHouse.Logger.Log(executeLog.ToString());
        }

        private bool ExecuteInstruction(ProgramContext context, bool doProfiling)
        {
            Opcode opcode = context.Program[context.InstructionPointer];
            if (SafeHouse.Config.DebugEachOpcode)
            {
                executeLog.Append(string.Format("Executing Opcode {0:0000}/{1:0000} {2} {3}\n", context.InstructionPointer, context.Program.Count, opcode.Label, opcode));
                executeLog.Append(string.Format("Prior to exeucting, stack looks like this:\n{0}\n", DumpStack()));
            }
            try
            {
                opcode.AbortContext = false;
                opcode.AbortProgram = false;

                opcode.Execute(this);

                // This will count *all* the time between the end of the prev instruction and now:
                instructionWatch.Stop();
                if (doProfiling)
                {
                    opcode.ProfileTicksElapsed += instructionWatch.ElapsedTicks;
                    opcode.ProfileExecutionCount++;
                }
                if (doProfiling || SafeHouse.Config.ShowStatistics)
                {
                    // Add the time this took to the exeuction stats for current priority level:
                    if (! executionStats.ContainsKey(CurrentPriority))
                        executionStats[CurrentPriority] = new ExecutionStatBlock();
                    executionStats[CurrentPriority].LogOneInstruction(instructionWatch.ElapsedTicks);
                }

                // start the *next* instruction's timer right after this instruction ended
                instructionWatch.Reset();
                if (doProfiling || SafeHouse.Config.ShowStatistics)
                {
                    instructionWatch.Start();
                }

                if (opcode.AbortProgram)
                {
                    BreakExecution(false);
                    SafeHouse.Logger.Log("Execution Broken");
                    return false;
                }

                if (opcode.AbortContext)
                {
                    return false;
                }

                int prevPointer = context.InstructionPointer;
                context.InstructionPointer += opcode.DeltaInstructionPointer;
                if (context.InstructionPointer < 0 || context.InstructionPointer >= context.Program.Count)
                {
                    throw new KOSBadJumpException(
                        context.InstructionPointer, string.Format("after executing {0:0000} {1} {2}", prevPointer, opcode.Label, opcode));
                }
                return true;
            }
            catch (Exception)
            {
                // exception will skip the normal printing of the log buffer,
                // so print what we have so far before throwing up the exception:
                if (executeLog.Length > 0)
                    SafeHouse.Logger.Log(executeLog.ToString());
                throw;
            }
        }

        private void SkipCurrentInstructionId()
        {
            if (currentContext.InstructionPointer >= (currentContext.Program.Count - 1)) return;

            GlobalPath currentSourcePath = currentContext.Program[currentContext.InstructionPointer].SourcePath;

            while (currentContext.InstructionPointer < currentContext.Program.Count &&
                   currentSourcePath.Equals(currentContext.Program[currentContext.InstructionPointer].SourcePath))
            {
                currentContext.InstructionPointer++;
            }
        }

        public void CallBuiltinFunction(string functionName)
        {
            shared.FunctionManager.CallFunction(functionName);
        }

        public bool BuiltInExists(string functionName)
        {
            return shared.FunctionManager.Exists(functionName);
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (shared.BindingMgr == null) return;

            shared.BindingMgr.ToggleFlyByWire(paramName, enabled);
            currentContext.ToggleFlyByWire(paramName, enabled);
        }

        public void SelectAutopilotMode(string autopilotMode)
        {
            shared.BindingMgr.SelectAutopilotMode(autopilotMode);
        }

        public List<string> GetCodeFragment(int contextLines)
        {
            return currentContext.GetCodeFragment(contextLines);
        }

        public string StatisticsDump(bool doProfiling)
        {
            if (!SafeHouse.Config.ShowStatistics) return "";

            StringBuilder sb = new StringBuilder();
            string formatterHeader = "{0,14},{1,7},{2,10},{3,7},{4,7},{5,7},{6,7}\n";
            string formatterValues = "{0,14},{1,7:D},{2,10:F2},{3,7:F0},{4,7:F3},{5,7:D},{6,7:F3}\n";
            sb.Append(string.Format("Total compile time: {0:F3}ms\n", totalCompileTime));
            sb.Append(string.Format(formatterHeader, "Interrupt", "Total", "Total", "Mean", "Mean", "Max", "Max"));
            sb.Append(string.Format(formatterHeader, "Priority", "inst", "ms", "instr/", "ms/", "instr/", "ms/"));
            sb.Append(string.Format(formatterHeader, "", "", "", "update", "update", "update", "update"));
            sb.Append(string.Format(formatterHeader, "-------------", "------", "------", "------", "------", "------", "------"));

            int overallInstr = 0;
            double overallMillis = 0.0;
            double overallMeanInstr = 0.0;
            double weightedSumMeanInstr = 0.0;
            double overallMeanMillis = 0.0;
            double weightedSumMeanMillis = 0.0;
            long overallMaxInstr = 0;
            double overallMaxMillis = 0.0;

            foreach (InterruptPriority pri in executionStats.Keys)
            {
                ExecutionStatBlock stats = executionStats[pri];
                stats.SealHangingUpdateIfAny(); // In case it aborted funny and didn't finish the last update.

                sb.Append(string.Format(formatterValues,
                    pri.ToString(),
                    stats.TotalInstructions,
                    stats.TotalMilliseconds,
                    stats.MeanInstructionsPerUpdate,
                    stats.MeanMillisecondsPerUpdate,
                    stats.MaxInstructionsPerUpdate,
                    stats.MaxMillisecondsInOneUpdate
                ));

                overallInstr += stats.TotalInstructions;
                overallMillis += stats.TotalMilliseconds;

                // Need to track a weighted mean depending on how many
                // instructions came from which priority level, thus the
                // mulitplication by how many total there were:
                weightedSumMeanInstr += (stats.TotalInstructions * stats.MeanInstructionsPerUpdate);
                weightedSumMeanMillis += (stats.TotalInstructions * stats.MeanMillisecondsPerUpdate);

                if (overallMaxInstr < stats.MaxInstructionsPerUpdate)
                    overallMaxInstr = stats.MaxInstructionsPerUpdate;
                if (overallMaxMillis < stats.MaxMillisecondsInOneUpdate)
                    overallMaxMillis = stats.MaxMillisecondsInOneUpdate;
            }
            overallMeanInstr = weightedSumMeanInstr / overallInstr;
            overallMeanMillis = weightedSumMeanMillis / overallInstr;

            sb.Append(string.Format(formatterValues,
                "TOTAL:", overallInstr, overallMillis, overallMeanInstr, overallMeanMillis, overallMaxInstr, overallMaxMillis ));

            if (!doProfiling)
                sb.Append("(`log ProfileResult() to file.csv` for more information.)\n");
            sb.Append(" \n");
            return sb.ToString();
        }
        
        public void ResetStatistics()
        {
            totalCompileTime = 0D;
            executionStats.Clear();
        }
        
        private void PrintStatistics()
        {
            shared.Screen.Print(StatisticsDump(false));
            ResetStatistics();
        }
        
        private void CalculateProfileResult()
        {
            ProfileResult = currentContext.GetCodeFragment(0, currentContext.Program.Count - 1, true);
            // Prepend a header string consisting of the block of summary text:
            ProfileResult.Insert(0, StatisticsDump(true));
        }

        public void Dispose()
        {
            while (contexts.Count > 0)
            {
                PopContext();
            }
            contexts.Clear();
            shared.UpdateHandler.RemoveFixedObserver(this);
        }


        public void StartCompileStopwatch()
        {
            compileWatch.Start();
        }

        public void StopCompileStopwatch()
        {
            compileWatch.Stop();
        }

        private class BootGlobalPath : InternalPath
        {
            private string command;

            public BootGlobalPath(string command) : base()
            {
                this.command = command;
            }

            public override string Line(int line)
            {
                return command;
            }

            public override string ToString()
            {
                return "[Boot sequence]";
            }
        }
    }
}