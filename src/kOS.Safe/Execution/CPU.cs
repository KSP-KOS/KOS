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
        private enum Section
        {
            Main = 1,
            Trigger = 2
        }

        private readonly IStack stack;
        private readonly VariableScope globalVariables;
        private Section currentRunSection;
        private List<YieldFinishedDetector> triggerYields;
        private List<YieldFinishedDetector> mainYields;
        
        private double currentTime;
        private readonly SafeSharedObjects shared;
        private readonly List<ProgramContext> contexts;
        private ProgramContext currentContext;
        private VariableScope savedPointers;
        private int instructionsSoFarInUpdate;
        private int instructionsPerUpdate;

        public int InstructionsThisUpdate { get { return instructionsSoFarInUpdate; } }

        // statistics
        private double totalCompileTime;

        private double totalUpdateTime;
        private double totalExecutionTime;
        private double maxUpdateTime;
        private double maxExecutionTime;
        private Stopwatch instructionWatch = new Stopwatch();
        private Stopwatch updateWatch = new Stopwatch();
        private Stopwatch executionWatch = new Stopwatch();
        private Stopwatch compileWatch = new Stopwatch();
        private int maxMainlineInstructionsSoFar;
        private readonly StringBuilder executeLog = new StringBuilder();

        public int InstructionPointer
        {
            get { return currentContext.InstructionPointer; }
            set { currentContext.InstructionPointer = value; }
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
        private List<WeakReference> popContextNotifyees;

        public CPU(SafeSharedObjects shared)
        {
            this.shared = shared;
            this.shared.Cpu = this;
            stack = new Stack();
            globalVariables = new VariableScope(0, null);
            contexts = new List<ProgramContext>();
            mainYields = new List<YieldFinishedDetector>();
            triggerYields = new List<YieldFinishedDetector>();
            if (this.shared.UpdateHandler != null) this.shared.UpdateHandler.AddFixedObserver(this);
            popContextNotifyees = new List<WeakReference>();
        }

        public void Boot()
        {
            // break all running programs
            currentContext = null;
            contexts.Clear();            
            if (shared.GameEventDispatchManager != null) shared.GameEventDispatchManager.Clear();
            PushInterpreterContext();
            currentRunSection = Section.Main;
            currentTime = 0;
            maxUpdateTime = 0.0;
            maxExecutionTime = 0.0;
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
                var bootMessage = string.Format(
                    "kOS Operating System\n" +
                    "KerboScript v{0}\n" +
                    "(manual at {1})\n",
                    SafeHouse.Version,
                    SafeHouse.DocumentationURL);
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

            VolumePath path = shared.Processor.BootFilePath;
            // Check to make sure the boot file name is valid, and then that the boot file exists.
            if (path == null)
            {
                SafeHouse.Logger.Log("Boot file name is empty, skipping boot script");

                shared.Screen?.Print(" \n" + "Proceed.\n");
            }
            else if (!shared.Processor.CheckCanBoot())
            {
                shared.Screen?.Print(string.Format(
                    " \n" +
                    "Could not boot from {0}\n" +
                    "Probably no connection to home\n" +
                    " \n" +
                    "Proceed.\n",
                    path));
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

                    shared.Screen?.Print(string.Format(
                        " \n" +
                        "Could not boot from {0}\n" +
                        "The file is missing\n" +
                        " \n" +
                        "Proceed.\n",
                        path));
                }
                else
                {
                    shared.Screen?.Print(string.Format(
                        " \n" +
                        "Booting from {0}\n" +
                        " \n",
                        path));

                    var bootContext = "program";
                    shared.ScriptHandler.ClearContext(bootContext);
                    IProgramContext programContext = SwitchToProgramContext();
                    programContext.Silent = true;

                    string bootCommand = string.Format("run \"{0}\".", file.Path);

                    var options = new CompilerOptions
                    {
                        LoadProgramsInSameAddressSpace = true,
                        FuncManager = shared.FunctionManager,
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
            // Not sure what the definition of Equals is for a weak reference,
            // this walks through looking if it's already registered, to avoid duplicates:
            for (int i = 0; i < popContextNotifyees.Count; ++i)
                if (popContextNotifyees[i].Target == notifyee)
                    return;

            popContextNotifyees.Add(new WeakReference(notifyee));
        }

        public void RemovePopContextNotifyee(IPopContextNotifyee notifyee)
        {
            // Might as well also get rid of any that are stale references while we're here:
            popContextNotifyees.RemoveAll((item)=>(!item.IsAlive) || item.Target == notifyee);
        }

        private void NotifyPopContextNotifyees(IProgramContext context)
        {
            // Notify them all:
            for (int i = 0; i < popContextNotifyees.Count; ++i)
            {
                WeakReference current = popContextNotifyees[i];
                if (current.IsAlive) // Avoid resurrecting it if it's gone, and don't call its hook.
                {
                    IPopContextNotifyee notifyee = current.Target as IPopContextNotifyee;
                    if (!notifyee.OnPopContext(context))
                        current.Target = null; // mark for removal below, because the notifyee wants us to
                }
            }

            // Remove the ones flagged for removal or that are stale anyway:
            popContextNotifyees.RemoveAll((item)=>(!item.IsAlive) || item.Target == null);
        }

        public void PushNewScope(Int16 scopeId, Int16 parentScopeId)
        {
            VariableScope parentScope = parentScopeId == 0 ? globalVariables : stack.FindScope(parentScopeId);
            stack.PushScope(new VariableScope(scopeId, parentScope));
        }

        /// <summary>
        /// Push a single thing onto the scope stack.
        /// </summary>
        public void PushScopeStack(object thing)
        {
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
                --howMany;
            }

            return returnVal;
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
                    // if the pointer exists it means it was redefined from inside a program
                    // and it's going to be invalid outside of it, so we remove it
                    globalVariables.Remove(item.Key);
                    deletedPointers++;
                    // also remove the corresponding trigger if exists
                    if (item.Value.Value is int)
                        RemoveTrigger((int)item.Value.Value);
                }
                else
                {
                    globalVariables.Add(item.Key, item.Value);
                    restoredPointers++;
                }
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
            if (contexts.Count > 1)
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
            }
            else
            {
                currentContext.ClearTriggers();   // remove all the triggers
                SkipCurrentInstructionId();
            }
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
            switch (currentRunSection)
            {
                case Section.Main:
                    mainYields.Add(yieldTracker);
                    break;
                case Section.Trigger:
                    triggerYields.Add(yieldTracker);
                    break;
                default:
                    // Should hypothetically be impossible unless we add more values to the enum.
                    break;
            }
            yieldTracker.creationTimeStamp = currentTime;
            yieldTracker.Begin(shared);
        }

        private bool IsYielding()
        {
            List<YieldFinishedDetector> yieldTrackers;
            
            // Decide if we should operate on the yield trackers that are
            // at the main code level or the ones that are at the trigger
            // level:
            switch (currentRunSection)
            {
                case Section.Main:
                    yieldTrackers = mainYields;
                    break;
                case Section.Trigger:
                    yieldTrackers = triggerYields;
                    break;
                default:
                    // Should hypothetically be impossible unless we add more values to the enum.
                    return false;
            }
            // Query the yield trackers and remove all the ones that claim they are finished.
            // Always treat them as unfinished if this is the same fixed time stamp as the
            // one in which the yield got Begin()'ed regardless of what their IsFinished() claims,
            // because all waits will always wait at least one tick, according to all our docs.
            yieldTrackers.RemoveAll((t) => t.creationTimeStamp != currentTime && t.IsFinished());
            
            // If any are still present, that means not all yielders in this context (main or trigger)
            // are finished and we should still return that we are waiting:
            return yieldTrackers.Count > 0;
        }
        
        private void AbortAllYields()
        {
            mainYields.Clear();
            triggerYields.Clear();
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
            Variable variable = currentScope.RemoveNested(identifier);
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
        /// Schedules a trigger function call to occur near the start of the next CPU update tick.
        /// If multiple such function calls get inserted between ticks, they will behave like
        /// a nested stack of function calls.  Mainline code will not continue until all such
        /// functions have finished at least once.  This type of trigger must be a function that
        /// takes zero parameters and returns a BooleanValue.  If its return is true, it will be
        /// automatically scheduled to run again by being re-inserted with a new AddTrigger()
        /// when it finishes.  If its return is false, it won't fire off again.
        /// </summary>
        /// <param name="triggerFunctionPointer">The entry point of this trigger function.</param>
        /// <param name="closure">The closure the trigger should be called with.  If this is
        /// null, then the trigger will only be able to see global variables reliably.</param>
        /// <returns>A TriggerInfo structure describing this new trigger, which probably isn't very useful
        /// tp the caller in most circumstances where this is a fire-and-forget trigger.</returns>
        public TriggerInfo AddTrigger(int triggerFunctionPointer, List<VariableScope> closure)
        {
            TriggerInfo triggerRef = new TriggerInfo(currentContext, triggerFunctionPointer, closure);
            currentContext.AddPendingTrigger(triggerRef);
            return triggerRef;
        }

        /// <summary>
        /// Schedules a trigger function call to occur near the start of the next CPU update tick.
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
        /// <param name="args">The list of arguments to pass to the UserDelegate when it gets called.</param>
        /// <returns>A TriggerInfo structure describing this new trigger.  It can be used to monitor
        /// the progress of the function call: To see if it has had a chance to finish executing yet,
        /// and to see what its return value was if it has finished.  Will be null if the UserDelegate was
        /// for an "illegal" program context.  Null returns are used instead of throwing an exception
        /// because this condition is expected to occur often when a program just ended that had callback hooks
        /// in it.</returns>
        public TriggerInfo AddTrigger(UserDelegate del, List<Structure> args)
        {
            if (del.ProgContext != currentContext)
                return null;
            TriggerInfo callbackRef = new TriggerInfo(currentContext, del.EntryPoint, del.Closure, del.GetMergedArgs(args));
            currentContext.AddPendingTrigger(callbackRef);
            return callbackRef;
        }

        /// <summary>
        /// Schedules a trigger function call to occur near the start of the next CPU update tick.
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
        /// <param name="args">A parms list of arguments to pass to the UserDelegate when it gets called.</param>
        /// <returns>A TriggerInfo structure describing this new trigger.  It can be used to monitor
        /// the progress of the function call: To see if it has had a chance to finish executing yet,
        /// and to see what its return value was if it has finished.  Will be null if the UserDelegate was
        /// for an "illegal" program context.  Null returns are used instead of throwing an exception
        /// because this condition is expected to occur often when a program is ended that had callback hooks
        /// in it.</returns>
        public TriggerInfo AddTrigger(UserDelegate del, params Structure[] args)
        {
            if (del.ProgContext != currentContext)
                return null;
            return AddTrigger(del, new List<Structure>(args));
        }

        /// <summary>
        /// Schedules a trigger function call to occur near the start of the next CPU update tick.
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
        /// <returns>To be in agreement with how the other AddTrigger() methods work, this returns
        /// a TriggerInfo which is just the same one you passed in.  It will return a null, however,
        /// in cases where the TriggerInfo you passed in is for a different ProgramContext.</returns>
        public TriggerInfo AddTrigger(TriggerInfo trigger)
        {
            if (trigger.ContextId != currentContext.ContextId)
                return null;
            currentContext.AddPendingTrigger(trigger);
            return trigger;
        }

        public void RemoveTrigger(int triggerFunctionPointer)
        {
            currentContext.RemoveTrigger(new TriggerInfo(currentContext, triggerFunctionPointer, null));
        }

        public void RemoveTrigger(TriggerInfo trigger)
        {
            currentContext.RemoveTrigger(trigger);
        }

        public void CancelCalledTriggers(int triggerFunctionPointer)
        {
            CancelCalledTriggers(new TriggerInfo(currentContext, triggerFunctionPointer, null));
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
            var executionElapsed = 0.0;

            // If the script changes config value, it doesn't take effect until next update:
            instructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
            instructionsSoFarInUpdate = 0;
            var numMainlineInstructions = 0;

            if (showStatistics)
            {
                updateWatch.Reset();
                executionWatch.Reset();
                instructionWatch.Reset();
                if (!compileWatch.IsRunning)
                {
                    compileWatch.Reset();
                }
                updateWatch.Start();
            }

            currentTime = shared.UpdateHandler.CurrentFixedTime;

            try
            {
                PreUpdateBindings();

                if (currentContext != null && currentContext.Program != null)
                {
                    ProcessTriggers();

                    if (showStatistics)
                    {
                        executionWatch.Start();
                        instructionWatch.Start();
                    }
                    ContinueExecution(showStatistics);
                    numMainlineInstructions = instructionsSoFarInUpdate;
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
                }
                else
                {
                    // break execution of all programs and pop interpreter context
                    PopFirstContext();
                    stack.Clear(); // If breaking all execution, get rid of the cruft here too.
                }
            }

            if (showStatistics)
            {
                updateWatch.Stop();
                double updateElapsed = updateWatch.ElapsedTicks * 1000D / Stopwatch.Frequency;
                totalUpdateTime += updateElapsed;
                executionElapsed = executionWatch.ElapsedTicks * 1000D / Stopwatch.Frequency;
                totalExecutionTime += executionElapsed;
                if (!compileWatch.IsRunning)
                {
                    totalCompileTime += compileWatch.ElapsedTicks * 1000D / Stopwatch.Frequency;
                }
                if (maxMainlineInstructionsSoFar < numMainlineInstructions)
                    maxMainlineInstructionsSoFar = numMainlineInstructions;
                if (maxUpdateTime < updateElapsed)
                    maxUpdateTime = updateElapsed;
                if (maxExecutionTime < executionElapsed)
                    maxExecutionTime = executionElapsed;
            }
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
            
            // To ensure triggers execute in the same order in which they
            // got added (thus ensuring the system favors trying the least
            // recently added trigger first in a more fair round-robin way),
            // the nested function calls get built in stack order, so that
            // they execute in normal order.  Thus the backward iteration
            // order used in the loop below:
            for (int index = currentContext.ActiveTriggerCount() - 1 ; index >= 0 ; --index)
            {
                TriggerInfo trigger = currentContext.GetTriggerByIndex(index);
                
                // If the program is ended from within a trigger, the trigger list will be empty and the pointer
                // will be invalid.  Only execute the trigger if it still exists.
                if (currentContext.ContainsTrigger(trigger))
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

                        // Reverse-push the closure's scope record, if there is one, just after the function return context got put on the stack.
                        if (trigger.Closure != null)
                            for (int i = trigger.Closure.Count - 1 ; i >= 0 ; --i)
                                PushScopeStack(trigger.Closure[i]);

                        PushArgumentStack(new KOSArgMarkerType());

                        if (trigger.IsCSharpCallback)
                            for (int argIndex = trigger.Args.Count - 1; argIndex >= 0 ; --argIndex) // TODO test with more than 1 arg to see if this is the right order!
                                PushArgumentStack(trigger.Args[argIndex]);
                        
                        triggersToBeExecuted.Add(trigger);

                        currentInstructionPointer = trigger.EntryPoint;
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
            int howManyMainLine = 0;
            currentRunSection = Section.Trigger; // assume we begin with trigger mode until we hit mainline code.
            
            executeLog.Remove(0, executeLog.Length); // In .net 2.0, StringBuilder had no Clear(), which is what this is simulating.

            while (instructionsSoFarInUpdate < instructionsPerUpdate &&
                   executeNext &&
                   currentContext != null)
            {
                if (! stack.HasTriggerContexts())
                {
                    currentRunSection = Section.Main;
                }
                
                if (IsYielding())
                {
                    executeNext = false;
                }
                else
                {
                    executeNext = ExecuteInstruction(currentContext, doProfiling);
                    instructionsSoFarInUpdate++;
                    if (currentRunSection == Section.Main)
                       ++howManyMainLine;
                }
            }

            // As long as at least one line of actual main code was reached, then re-enable all
            // triggers that wanted to be re-enabled.  This delay in re-enabling them
            // ensures that a nested bunch of triggers interruping other triggers can't
            // *completely* starve mainline code of execution, no matter how invasive
            // and cpu-hogging the user may have been written them to be:
            if (currentRunSection == Section.Main)
                currentContext.ActivatePendingTriggers();

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

                if (doProfiling)
                {
                    // This will count *all* the time between the end of the prev instruction and now:
                    instructionWatch.Stop();
                    opcode.ProfileTicksElapsed += instructionWatch.ElapsedTicks;
                    opcode.ProfileExecutionCount++;
                    
                    // start the *next* instruction's timer right after this instruction ended
                    instructionWatch.Reset();
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
            
            string delimiter = "";
            if (doProfiling)
                delimiter = ",";
            
            StringBuilder sb = new StringBuilder();

            sb.Append(string.Format("{0}{0}{0}{0}Total compile time: {0}{1:F3}ms\n", delimiter, totalCompileTime));
            sb.Append(string.Format("{0}{0}{0}{0}Total update time: {0}{1:F3}ms\n", delimiter, totalUpdateTime));
            sb.Append(string.Format("{0}{0}{0}{0}Total execution time: {0}{1:F3}ms\n", delimiter, totalExecutionTime));
            sb.Append(string.Format("{0}{0}{0}{0}Maximum update time: {0}{1:F3}ms\n", delimiter, maxUpdateTime));
            sb.Append(string.Format("{0}{0}{0}{0}Maximum execution time: {0}{1:F3}ms\n", delimiter, maxExecutionTime));
            sb.Append(string.Format("{0}{0}{0}{0}Most Mainline instructions in one update: {0}{1}\n", delimiter, maxMainlineInstructionsSoFar));
            if (!doProfiling)
                sb.Append("(`log ProfileResult() to file.csv` for more information.)\n");
            sb.Append(" \n");
            return sb.ToString();
        }
        
        public void ResetStatistics()
        {
            totalCompileTime = 0D;
            totalUpdateTime = 0D;
            totalExecutionTime = 0D;
            maxUpdateTime = 0.0;
            maxExecutionTime = 0.0;
            maxMainlineInstructionsSoFar = 0;
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