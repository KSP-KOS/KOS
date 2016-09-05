using kOS.Safe.Binding;
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
        private enum Status
        {
            Running = 1,
            Waiting = 2
        }

        private readonly IStack stack;
        private readonly VariableScope globalVariables;
        private Status currentStatus;
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
        
        /// <summary>
        /// Where was the instruction pointer at the time this update
        /// first started?
        /// </summary>
        int firstInstructionPointerInUpdate;

        public double SessionTime { get { return currentTime; } }
        
        public List<string> ProfileResult { get; private set; }

        public CPU(SafeSharedObjects shared)
        {
            this.shared = shared;
            this.shared.Cpu = this;
            stack = new Stack();
            globalVariables = new VariableScope(0, -1);
            contexts = new List<ProgramContext>();
            if (this.shared.UpdateHandler != null) this.shared.UpdateHandler.AddFixedObserver(this);
        }

        public void Boot()
        {
            // break all running programs
            currentContext = null;
            contexts.Clear();
            PushInterpreterContext();
            currentStatus = Status.Running;
            currentTime = 0;
            maxUpdateTime = 0.0;
            maxExecutionTime = 0.0;
            // clear stack (which also orphans all local variables so they can get garbage collected)
            stack.Clear();
            // clear global variables
            globalVariables.Variables.Clear();
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

                    string bootCommand = string.Format("run \"{0}\".", file.Path);

                    var options = new CompilerOptions
                    {
                        LoadProgramsInSameAddressSpace = true,
                        FuncManager = shared.FunctionManager,
                        IsCalledFromRun = false
                    };

                    shared.ScriptHandler.ClearContext(bootContext);
                    List<CodePart> parts = shared.ScriptHandler.Compile(new BootGlobalPath(bootCommand),
                        1, bootCommand, bootContext, options);

                    IProgramContext programContext = SwitchToProgramContext();
                    programContext.Silent = true;
                    programContext.AddParts(parts);
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

            if (contexts.Count > 1)
            {
                shared.Interpreter.SetInputLock(true);
            }
        }

        private void PopContext()
        {
            SafeHouse.Logger.Log("Popping context " + contexts.Count);
            if (contexts.Any())
            {
                // remove the last context
                ProgramContext contextRemove = contexts.Last();
                contexts.Remove(contextRemove);
                contextRemove.DisableActiveFlyByWire(shared.BindingMgr);
                SafeHouse.Logger.Log("Removed Context " + contextRemove.GetCodeFragment(0).FirstOrDefault());

                if (contexts.Any())
                {
                    currentContext = contexts.Last();
                    currentContext.EnableActiveFlyByWire(shared.BindingMgr);
                    RestorePointers();
                    SafeHouse.Logger.Log("New current context " + currentContext.GetCodeFragment(0).FirstOrDefault());
                }
                else
                {
                    currentContext = null;
                }

                if (contexts.Count == 1)
                {
                    shared.Interpreter.SetInputLock(false);
                }
            }
        }

        /// <summary>
        /// Push a single thing onto the secret "over" stack.
        /// </summary>
        public void PushAboveStack(object thing)
        {
            PushStack(thing);
            MoveStackPointer(-1);
        }

        /// <summary>
        /// Pop one or more things from the secret "over" stack, only returning the
        /// finalmost thing popped.  (i.e if you pop 3 things then you get:
        /// pop once and throw away, pop again and throw away, pop again and return the popped thing.)
        /// </summary>
        public object PopAboveStack(int howMany)
        {
            object returnVal = new int(); // bogus return val if given a bogus "pop zero things" request.
            while (howMany > 0)
            {
                MoveStackPointer(1);
                returnVal = PopStack();
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
            GetNestedDictionary("", closureList);
            // The closure's variable scopes need to be marked as such, so the
            // 'popscope' opcode knows to pop them off in one go when it hits
            // them on the stack:
            foreach (VariableScope scope in closureList)
                scope.IsClosure = true;
            return closureList;
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
            savedPointers = new VariableScope(0, -1);
            var pointers = new List<string>(globalVariables.Variables.Keys.Where(v => v.Contains('*')));

            foreach (string pointerName in pointers)
            {
                savedPointers.Variables.Add(pointerName, globalVariables.Variables[pointerName]);
                globalVariables.Variables.Remove(pointerName);
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

            foreach (KeyValuePair<string, Variable> item in savedPointers.Variables)
            {
                if (globalVariables.Variables.ContainsKey(item.Key))
                {
                    // if the pointer exists it means it was redefined from inside a program
                    // and it's going to be invalid outside of it, so we remove it
                    globalVariables.Variables.Remove(item.Key);
                    deletedPointers++;
                    // also remove the corresponding trigger if exists
                    if (item.Value.Value is int)
                        RemoveTrigger((int)item.Value.Value);
                }
                else
                {
                    globalVariables.Variables.Add(item.Key, item.Value);
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
                EndWait();

                if (SafeHouse.Config.ShowStatistics)
                    CalculateProfileResult();

                if (manual)
                {
                    PopFirstContext();
                    shared.Screen.Print("Program aborted.");
                    shared.BindingMgr.UnBindAll();
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
                        shared.BindingMgr.UnBindAll();
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

        public void PushStack(object item)
        {
            stack.Push(item);
        }

        public object PopStack()
        {
            return stack.Pop();
        }

        public void MoveStackPointer(int delta)
        {
            stack.MoveStackPointer(delta);
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
        /// Gets the dictionary N levels of nesting down the dictionary stack,
        /// where zero is the current localmost level.
        /// Never errors out or fails.  If N is too large you just end up with
        /// the global scope dictionary.
        /// Does not allow the walk to go past the start of the current function
        /// scope.
        /// </summary>
        /// <param name="peekDepth">how far down the peek under the top.  0 = localmost.</param>
        /// <returns>The dictionary found, or the global dictionary if peekDepth is too big.</returns>
        private VariableScope GetNestedDictionary(int peekDepth)
        {
            object stackItem = true; // any non-null value will do here, just to get the loop started.
            for (var rawStackDepth = 0; stackItem != null && peekDepth >= 0; ++rawStackDepth)
            {
                stackItem = stack.Peek(-1 - rawStackDepth);
                if (stackItem is VariableScope)
                    --peekDepth;
                if (stackItem is SubroutineContext)
                    stackItem = null; // once we hit the bottom of the current subroutine on the runtime stack - jump all the way out to global.
            }

            var scope = stackItem as VariableScope;
            return scope ?? globalVariables;
        }

        /// <summary>
        /// Gets the dictionary that contains the given identifier, starting the
        /// search at the local level and scanning the scopes upward all the
        /// way to the global dictionary.<br/>
        /// Does not allow the walk to use scope frames that were not directly in this
        /// scope's lexical chain.  It skips over scope frames from other branches
        /// of the parse tree.  (i.e. if a function calls a function elsewhere).<br/>
        /// Returns null when no hit was found.<br/>
        /// </summary>
        /// <param name="identifier">identifier name to search for.  Pass an empty string to guarantee no hits will
        ///   be found (which is useful to do when using the searchReport argument).</param>
        /// <param name="searchReport">If you want to see the list of all the scopes that constituted the search
        ///   path, not just the final hit, pass an empty list here and this method will fill it for you with
        ///   that report.  Pass in a null to not get a report.</param>
        /// <returns>The dictionary found, or null if no dictionary contains the identifier.</returns>
        private VariableScope GetNestedDictionary(string identifier, List<VariableScope> searchReport = null)
        {
            if (searchReport != null)
                searchReport.Clear();
            short rawStackDepth = 0;
            while (true) /*all of this loop's exits are explicit break or return statements*/
            {
                object stackItem;
                bool stackExhausted = !(stack.PeekCheck(-1 - rawStackDepth, out stackItem));
                if (stackExhausted)
                    break;
                var localDict = stackItem as VariableScope;
                if (localDict == null) // some items on the stack might not be variable scopes.  skip them.
                {
                    ++rawStackDepth;
                    continue;
                }

                if (searchReport != null)
                    searchReport.Add(localDict);

                if (localDict.Variables.ContainsKey(identifier))
                    return localDict;

                // Get the next VariableScope that is valid, where valid means:
                //    It is the lexical (not runtime) parent of this scope.
                // -------------------------------------------------------------------------------

                // Scan the stack until the variable scope with the right parent ID is seen:
                short skippedLevels = 0;
                while (!(stackExhausted))
                {
                    var needsIncrement = true;
                    var scopeFrame = stackItem as VariableScope;
                    if (scopeFrame != null) // skip cases where the thing on the stack isn't a variable scope.
                    {
                        // If the scope id of this frame is my parent ID, then we found it and are done.
                        if (scopeFrame.ScopeId == localDict.ParentScopeId)
                        {
                            break;
                        }
                        // In the case where the variable scope is the SAME lexical ID as myself, that
                        // means I recursively called myself and the thing on the runtime stack just before
                        // me is ... another instance of me.  In that case just follow it's parent skip level
                        if (scopeFrame.ScopeId == localDict.ScopeId && scopeFrame.ParentSkipLevels > 0)
                        {
                            skippedLevels += scopeFrame.ParentSkipLevels;
                            rawStackDepth += scopeFrame.ParentSkipLevels;
                            needsIncrement = false;
                        }
                    }
                    if (needsIncrement)
                    {
                        ++skippedLevels;
                        ++rawStackDepth;
                    }
                    stackExhausted = !(stack.PeekCheck(-1 - rawStackDepth, out stackItem));
                }

                // Record how many levels had to be skipped for that to work.  In future calls of this
                // method, it will know how far to jump in the stack without doing that scan.  This can
                // be quite a speedup when dealing with nested recursion, where the runtime stack might
                // be a hundred levels deep of the same function calling itself before hitting its lexical parent.
                if (stackItem != null && localDict.ParentSkipLevels == 0)
                    localDict.ParentSkipLevels = skippedLevels;
            }
            if (globalVariables.Variables.ContainsKey(identifier))
                return globalVariables;
            else
                return null;
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
            foreach (string ident in globalVariables.Variables.Keys)
            {
                string line;
                try
                {
                    Variable v = globalVariables.Variables[ident];
                    line = ident;
                    if (v == null || v.Value == null)
                        line += " is <null>";
                    else
                        line += " is a " + v.Value.GetType().FullName + " with value = " + v.Value;
                }
                catch (Exception e)
                {
                    // This is necessary because of the deprecation exceptions that
                    // get raised by FlightStats when you try to print all of them out:
                    line = ident + " is <value caused exception>\n    " + e.Message;
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
            VariableScope foundDict = GetNestedDictionary(identifier);
            if (foundDict != null)
                return foundDict.Variables[identifier];
            if (barewordOkay)
            {
                string strippedIdent = identifier.TrimStart('$');
                return new Variable { Name = strippedIdent, Value = strippedIdent };
            }
            if (failOkay)
                return null;
            // In the case where we were looking for a function pointer but didn't find one, and would
            // have failed with exception, then it's still acceptable to find a hit that isn't a function
            // pointer (has no trailing asterisk '*') but only if it's a delegate of some sort:
            if (identifier.EndsWith("*"))
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
            identifier = identifier.ToLower();

            if (!identifier.StartsWith("$"))
            {
                identifier = "$" + identifier;
            }

            VariableScope whichDict = local ? GetNestedDictionary(0) : globalVariables;
            if (whichDict.Variables.ContainsKey(identifier))
            {
                if (whichDict.Variables[identifier].Value is BoundVariable)
                    if (!overwrite)
                        throw new KOSIdentiferClashException(identifier);
                whichDict.Variables.Remove(identifier);
            }
            whichDict.Variables.Add(identifier, variable);
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
            identifier = identifier.ToLower();
            VariableScope foundDict = GetNestedDictionary(identifier);
            if (foundDict != null && VariableIsRemovable(foundDict.Variables[identifier]))
            {
                // Tell Variable to orphan its old value now.  Faster than relying
                // on waiting several seconds for GC to eventually call ~Variable()
                foundDict.Variables[identifier].Value = null;

                foundDict.Variables.Remove(identifier);
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
            Variable variable;
            VariableScope localDict = GetNestedDictionary(0);
            if (!localDict.Variables.TryGetValue(identifier, out variable))
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
            Variable variable;
            // Attempt to get it as a global.  Make a new one if it's not found.
            // This preserves the "bound-ness" of the variable if it's a
            // BoundVariable, whereas unconditionally making a new Variable wouldn't:
            if (!globalVariables.Variables.TryGetValue(identifier, out variable))
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
        /// Pop a value off the stack, and if it's a variable name then get its value,
        /// else just return it as it is.
        /// </summary>
        /// <param name="barewordOkay">Is this a context in which it's acceptable for
        ///   a variable not existing error to occur (in which case the identifier itself
        ///   should therefore become a string object returned)?</param>
        /// <returns>value off the stack</returns>
        public object PopValue(bool barewordOkay = false)
        {
            return GetValue(PopStack(), barewordOkay);
        }

        /// <summary>
        /// Peek at a value atop the stack without popping it, and if it's a variable name then get its value,
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
        public object PeekValue(int digDepth, bool barewordOkay = false)
        {
            return GetValue(stack.Peek(digDepth), barewordOkay);
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
        public Structure PopStructureEncapsulated(bool barewordOkay = false)
        {
            return Structure.FromPrimitiveWithAssert( PopValue(barewordOkay) );
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
        public Structure PeekStructureEncapsulated(int digDepth, bool barewordOkay = false)
        {
            return Structure.FromPrimitiveWithAssert(PeekValue(digDepth, barewordOkay));
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
        public Structure GetStructureEncapsulated(Structure testValue, bool barewordOkay = false)
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
        public object PopValueEncapsulated(bool barewordOkay = false)
        {
            return Structure.FromPrimitive( PopValue(barewordOkay) );
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
        public object PeekValueEncapsulated(int digDepth, bool barewordOkay = false)
        {
            return Structure.FromPrimitive(PeekValue(digDepth, barewordOkay));
        }

        /// <summary>
        /// Peek at a value atop the stack without popping it, and without evaluating it to get the variable's
        /// value.  (i.e. if the thing in the stack is $foo, and the variable foo has value 5, you'll get the string
        /// "$foo" returned, not the integer 5).
        /// </summary>
        /// <param name="digDepth">Peek at the element this far down the stack (0 means top, 1 means just under the top, etc)</param>
        /// <param name="checkOkay">Tells you whether or not the stack was exhausted.  If it's false, then the peek went too deep.</param>
        /// <returns>value off the stack</returns>
        public object PeekRaw(int digDepth, out bool checkOkay)
        {
            object returnValue;
            checkOkay = stack.PeekCheck(digDepth, out returnValue);
            return returnValue;
        }

        public int GetStackSize()
        {
            return stack.GetLogicalSize();
        }

        public void AddTrigger(int triggerFunctionPointer)
        {
            currentContext.AddPendingTrigger(triggerFunctionPointer);
        }

        public void RemoveTrigger(int triggerFunctionPointer)
        {
            currentContext.RemoveTrigger(triggerFunctionPointer);
        }

        /// <summary>
        /// Put the CPU into wait state for now, which will reset next Update.
        /// Also, if given a number of seconds, return the number of seconds into
        /// the future that is, in game-terms, for the sake of making a timestamp
        /// of when the wait is expected to be over.
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        public double StartWait(double waitTime)
        {
            if (currentStatus == Status.Running)
                currentStatus = Status.Waiting;
            return currentTime + waitTime;
        }

        public void EndWait()
        {
            if (currentStatus == Status.Waiting)
                currentStatus = Status.Running;
        }

        public void KOSFixedUpdate(double deltaTime)
        {
            bool showStatistics = SafeHouse.Config.ShowStatistics;
            var executionElapsed = 0.0;

            // If the script changes config value, it doesn't take effect until next update:
            instructionsPerUpdate = SafeHouse.Config.InstructionsPerUpdate;
            instructionsSoFarInUpdate = 0;
            var numMainlineInstructions = 0;

            firstInstructionPointerInUpdate = InstructionPointer;

            if (showStatistics)
            {
                updateWatch.Reset();
                executionWatch.Reset();
                instructionWatch.Reset();
                compileWatch.Stop();
                compileWatch.Reset();
                updateWatch.Start();
            }

            currentTime = shared.UpdateHandler.CurrentFixedTime;

            try
            {
                PreUpdateBindings();

                if (currentContext != null && currentContext.Program != null)
                {
                    ProcessTriggers();

                    if (currentStatus == Status.Running || currentStatus == Status.Waiting)
                    {
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
                totalCompileTime += compileWatch.ElapsedTicks * 1000D / Stopwatch.Frequency;
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
            var triggersToBeExecuted = new List<int>();
            
            // To ensure triggers execute in the same order in which they
            // got added (thus ensuring the system favors trying the least
            // recently added trigger first in a more fair round-robin way),
            // the nested function calls get built in stack order, so that
            // they execute in normal order.  Thus the backward iteration
            // order used in the loop below:
            for (int index = currentContext.ActiveTriggerCount() - 1 ; index >= 0 ; --index)
            {
                int triggerPointer = currentContext.GetTriggerByIndex(index);
                
                // If the program is ended from within a trigger, the trigger list will be empty and the pointer
                // will be invalid.  Only execute the trigger if it still exists.
                if (currentContext.ContainsTrigger(triggerPointer))
                {
                    // Insert a faked function call as if the trigger had been called from just
                    // before whatever opcode was about to get executed, by pusing a context
                    // record like OpcodeCall would do, and moving the IP to the
                    // first line of the trigger, like OpcodeCall would do.  Because
                    // triggers can't take arguments, most of the messy work of
                    // OpcodeCall.Execute isn't needed:
                    SubroutineContext contextRecord =
                        new SubroutineContext(currentInstructionPointer, triggerPointer);
                    PushAboveStack(contextRecord);
                    PushStack(new KOSArgMarkerType()); // to go with the faked function call of zero arguments.

                    triggersToBeExecuted.Add(triggerPointer);
                    
                    currentInstructionPointer = triggerPointer;
                    // Triggers can chain in this loop if more than one fire off at once - the second trigger
                    // will look like it was a function that was called from the start of the first trigger.
                    // The third trigger will look like a function that was called from the start of the second, etc.
                }
            }
            
            // Remove all triggers that will fire.  Any trigger that wants to
            // re-enable itself will do so by returning a boolean true, which
            // will tell OpcodeReturn that it needs to re-add the trigger.
            foreach (int triggerPointer in triggersToBeExecuted)
                RemoveTrigger(triggerPointer);

            currentContext.InstructionPointer = currentInstructionPointer;
        }

        private void ContinueExecution(bool doProfiling)
        {
            var executeNext = true;
            int howManyMainLine = 0;
            bool reachedMainLine = false;
            executeLog.Remove(0, executeLog.Length); // In .net 2.0, StringBuilder had no Clear(), which is what this is simulating.

            EndWait(); // Temporarily, perhaps. If the first thing it finds is the same OpcodeWait, it can revert back to wait mode.

            while (currentStatus == Status.Running &&
                   instructionsSoFarInUpdate < instructionsPerUpdate &&
                   executeNext &&
                   currentContext != null)
            {
                if (InstructionPointer == firstInstructionPointerInUpdate &&
                    ! stack.HasTriggerContexts()) // This additional check is important.
                                                 // Hypothetically it could be possible for trigger code to hit the same
                                                 // IP as where mainline code was interrupted, if both trigger code and
                                                 // mainline code make use of the same library function, and the
                                                 // trigger interruption occurred when mainline code was in that
                                                 // same library function.  This ensures that if so, that doesn't count.
                                                 // It only counts when all the triggers have popped off the call stack.
                {
                    reachedMainLine = true;
                }
                executeNext = ExecuteInstruction(currentContext, doProfiling);
                instructionsSoFarInUpdate++;
                if (reachedMainLine)
                  ++howManyMainLine;
            }

            // As long as at least one line of actual main code was reached, then re-enable all
            // triggers that wanted to be re-enabled.  This delay in re-enabling them
            // ensures that a nested bunch of triggers interruping other triggers can't
            // *completely* starve mainline code of execution, no matter how invasive
            // and cpu-hogging the user may have been written them to be:
            if (reachedMainLine)
                currentContext.ActivatePendingTriggers();

            if (executeLog.Length > 0)
                SafeHouse.Logger.Log(executeLog.ToString());
        }

        private bool ExecuteInstruction(IProgramContext context, bool doProfiling)
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

                if (opcode.IsYielding)
                    opcode.CheckYield(this);
                else
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