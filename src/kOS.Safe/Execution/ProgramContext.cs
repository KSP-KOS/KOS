using System;
using System.Collections.Generic;
using kOS.Safe.Binding;
using kOS.Safe.Compilation;

namespace kOS.Safe.Execution
{
    public class ProgramContext : IProgramContext
    {
        private readonly Dictionary<string, bool> flyByWire;
        private readonly ProgramBuilder builder;
        private readonly Dictionary<string, int> fileMap;

        public List<Opcode> Program { get; set; }
        public int InstructionPointer { get; set; }
        public InterruptPriority CurrentPriority { get; set; }

        // Increments every time we construct a new ProgramContext.
        private static int globalInstanceCount = 0;
        
        /// <summary>Each constructed instance of ProgramContext gets a new ID number.
        /// That way we can mark other objects with the ID of the ProgramContext
        /// they go with, without needing to keep references around that would
        /// prevent disposing the program context.</summary>
        public int ContextId { get; set; }

        /// <summary>
        /// List of triggers that are currently active
        /// </summary>
        private List<TriggerInfo> Triggers { get; set; }

        private int nextTriggerInstanceId = 1;
        public int NextTriggerInstanceId { get {return nextTriggerInstanceId++;} }
        public void ResetTriggerInstanceIdCounter()
        {
            nextTriggerInstanceId = 1;
        }

        /// <summary>
        /// List of triggers that are *about to become* currently active, but only after
        /// the CPU tells us it's a good safe time to re-insert them.  This delay is done
        /// so that triggers interrupting other triggers can't entirely starve mainline
        /// code from executing - they don't get re-inserted until back to the mainline
        /// code again.
        /// </summary>
        private List<TriggerInfo> TriggersToInsert { get; set; }
        public bool Silent { get; set; }
  
        public ProgramContext(bool interpreterContext)
        {
            Program = new List<Opcode>();
            InstructionPointer = 0;
            Triggers = new List<TriggerInfo>();
            TriggersToInsert = new List<TriggerInfo>();
            builder = interpreterContext ? new ProgramBuilderInterpreter() : new ProgramBuilder();
            flyByWire = new Dictionary<string, bool>();
            fileMap  = new Dictionary<string, int>();
            ContextId = ++globalInstanceCount;
        }

        public ProgramContext(bool interpreterContext, List<Opcode> program) : this(interpreterContext)
        {
            Program = program;
            ContextId = ++globalInstanceCount;
        }

        public void AddParts(IEnumerable<CodePart> parts)
        {
            builder.AddRange(parts);
            List<Opcode> newProgram = builder.BuildProgram();
            UpdateProgram(newProgram);
        }

        public int AddObjectParts(IEnumerable<CodePart> parts, string objectFileID)
        {
            Guid objectFileGuid = builder.AddObjectFile(parts);
            List<Opcode> newProgram = builder.BuildProgram();
            int entryPointAddress = builder.GetObjectFileEntryPointAddress(objectFileGuid);
            UpdateProgram(newProgram);
            UpdateFileMap(objectFileID, entryPointAddress);
            return entryPointAddress;
        }
        
        private void UpdateFileMap(string fileID, int entryPoint)
        {
            fileMap[fileID] = entryPoint;
        }
        
        /// <summary>
        /// Return the entry point into the program context where this
        /// filename was already inserted into the system before.
        /// If it hasn't been inserted before, returns a negative number
        /// as a flag indicating this fact.  fileID should be a string that
        /// will be fully unique for each file (the fully qualified
        /// path name, for example).
        /// </summary>
        /// <param name="fileID"></param>
        /// <returns>entry point or -1 if not present</returns>
        public int GetAlreadyCompiledEntryPoint(string fileID)
        {
            if (fileMap.ContainsKey(fileID))
                return fileMap[fileID];
            else
                return -1;
        }

        private void UpdateProgram(List<Opcode> newProgram)
        {
            if (Program != null) {
                List<Opcode> oldProgram = Program;
                Program = newProgram;
                UpdateInstructionPointer(oldProgram);
            } else {
                Program = newProgram;
            }
        }

        private void UpdateInstructionPointer(List<Opcode> oldProgram)
        {
            if (oldProgram.Count > 1) {
                int delta = 0;

                if (InstructionPointer == (oldProgram.Count - 1)) {
                    delta = 1;
                }

                int currentInstructionId = oldProgram[InstructionPointer - delta].Id;

                for (int index = 0; index < Program.Count; index++) {
                    if (Program[index].Id == currentInstructionId) {
                        InstructionPointer = index + delta;
                        break;
                    }
                }
            }
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (!flyByWire.ContainsKey(paramName)) {
                flyByWire.Add(paramName, enabled);
            } else {
                flyByWire[paramName] = enabled;
            }
        }

        public void DisableActiveFlyByWire(IBindingManager manager)
        {
            foreach (KeyValuePair<string, bool> kvp in flyByWire) {
                if (kvp.Value) {
                    try
                    {
                        manager.ToggleFlyByWire(kvp.Key, false);
                    }
                    catch(Exception ex) // intentionally catch any exception thrown so we don't crash in the middle of breaking execution
                    {
                        // log the exception only when "super verbose" is enabled
                        Utilities.SafeHouse.Logger.SuperVerbose(string.Format("Excepton in ProgramContext.DisableActiveFlyByWire\r\n{0}", ex));
                    }
                }
            }
        }

        public void EnableActiveFlyByWire(IBindingManager manager)
        {
            foreach (KeyValuePair<string, bool> kvp in flyByWire) {
                manager.ToggleFlyByWire(kvp.Key, kvp.Value);
            }
        }
        
        /// <summary>
        /// Remove all active and pending triggers.
        /// </summary>
        public void ClearTriggers()
        {
            Triggers.Clear();
            TriggersToInsert.Clear();
        }
        
        /// <summary>
        /// Add a trigger to the list of triggers pending insertion.
        /// It will not *finish* inserting it until the CPU tells us it's a good
        /// time to do so (i.e. next time CPU does a FixedUpdate), by calling
        /// ActivatePendingTriggers().
        /// It will also refuse to insert a WHEN/ON/LOCK trigger that's already either active
        /// or pending insertion to the active list (avoids duplication).
        /// </summary>
        /// <param name="trigger"></param>
        public void AddPendingTrigger(TriggerInfo trigger)
        {
            // ContainsTrigger is a sequential walk, but that should be okay
            // because it should be unlikely that there's hundreds of
            // triggers.  There'll be at most tens of them, and even that's
            // unlikely.
            trigger.IsImmediateTrigger = false;
            if (! ContainsTrigger(trigger))
                TriggersToInsert.Add(trigger);
        }

        /// <summary>
        /// Adds a trigger to happen immediately on the next opcode, instead of
        /// waiting for the next CPU FixedUpdate like AddPendingTrigger does.
        /// </summary>
        /// <param name="trigger">Trigger to be inserted</param>
        public void AddImmediateTrigger(TriggerInfo trigger)
        {
            trigger.IsImmediateTrigger = true;
            Triggers.Add(trigger);
        }
        
        /// <summary>
        /// Remove a trigger from current triggers or pending insertion
        /// triggers or both if need be, so it's not there anymore at all.
        /// </summary>
        /// <param name="trigger"></param>
        public void RemoveTrigger(TriggerInfo trigger)
        {
            Triggers.RemoveAll((item) => item.Equals(trigger)); // can ignore if it wasn't in the list.
            TriggersToInsert.RemoveAll((item) => item.Equals(trigger)); // can ignore if it wasn't in the list.
        }
        
        /// <summary>
        /// How many triggers (active) there are.
        /// </summary>
        /// <returns></returns>
        public int ActiveTriggerCount()
        {
            return Triggers.Count;
        }
        
        /// <summary>
        /// Return the active trigger at the given index.  Cannot be used
        /// to get pending insertion triggers.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TriggerInfo GetTriggerByIndex(int index)
        {
            return Triggers[index];
        }
        
        /// <summary>
        /// True if the given trigger's IP is for a trigger that
        /// is currently active, or is about to become active.
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        public bool ContainsTrigger(TriggerInfo trigger)
        {
            return Triggers.Contains(trigger) || TriggersToInsert.Contains(trigger);
        }

        /// <summary>
        /// Take only those pending triggers that AddPendingTrigger added who's
        /// Priority is higher than the given value, and make them become active.
        /// ("active" here means "called on the callstack like a subroutine.")
        /// </summary>
        /// <param name="aboveThis"></param>
        public void ActivatePendingTriggersAbovePriority(InterruptPriority aboveThis)
        {
            Triggers.AddRange(TriggersToInsert.FindAll(t => t.Priority > aboveThis));
            TriggersToInsert.RemoveAll(t => t.Priority > aboveThis);
        }

        public bool HasActiveTriggersAtLeastPriority(InterruptPriority pri)
        {
            return Triggers.Exists(t => t.Priority >= pri && !t.IsImmediateTrigger);
        }

        public List<string> GetCodeFragment(int contextLines)
        {
            return GetCodeFragment(InstructionPointer - contextLines, InstructionPointer + contextLines);
        }

        public List<string> GetCodeFragment(int start, int stop, bool doProfile = false)
        {
            var codeFragment = new List<string>();
            var profileFragment = new List<string>();

            string formatStr = "{0,-20} {1,4}:{2,-3} {3:0000} {4,-7} {5} {6}";
            if (doProfile)
                formatStr = formatStr.Replace(' ',','); // make profile output be suitable for CSV files.
            string header1 = string.Format(formatStr, "File", "Line", "Col", "IP  ", "label", "opcode", "operand");
            string header2 = string.Format(formatStr, "====", "====", "===", "====", "================================", "", "");
            codeFragment.Add(header1);
            codeFragment.Add(header2);
            
            int longestLength = header1.Length;

            for (int index = start; index <= stop; index++)
            {
                if (index >= 0 && index < Program.Count)
                {
                    string thisLine = string.Format(
                        formatStr,
                        Program[index].SourcePath,
                        Program[index].SourceLine,
                        Program[index].SourceColumn,
                        index,
                        (doProfile ? ProtectCSVField(Program[index].Label) : Program[index].Label),
                        (doProfile ? ProtectCSVField(Program[index].ToString()) : Program[index].ToString()),
                        (index == InstructionPointer ? "<<--INSTRUCTION POINTER--" : ""));
                    codeFragment.Add(thisLine);
                    if (longestLength < thisLine.Length)
                        longestLength = thisLine.Length;
                }
            }
            
            if (!doProfile)
                return codeFragment;
            
            // Append the profile data columns to the right of the codeFragment lines:

            const string PROFILE_FORMAT_STR = "{0},{1,12:0.0000},{2,6},{3,12:0.0000}";
            profileFragment.Add(string.Format(PROFILE_FORMAT_STR, codeFragment[0].PadRight(longestLength), "Total ms", "Count", "Average ms"));
            profileFragment.Add(string.Format(PROFILE_FORMAT_STR, codeFragment[1].PadRight(longestLength), "========", "=====", "=========="));
            for (int index = start; index <= stop; index++)
            {
                if (index >= 0 && index < Program.Count)
                {
                    long totalTicks = Program[index].ProfileTicksElapsed;
                    int  count = Program[index].ProfileExecutionCount;
                    string thisLine = string.Format(
                        PROFILE_FORMAT_STR,
                        codeFragment[2 + (index-start)].PadRight(longestLength),
                        (totalTicks*1000D) / System.Diagnostics.Stopwatch.Frequency,
                        count,
                        ((totalTicks*1000D) / count) / System.Diagnostics.Stopwatch.Frequency
                       );
                    profileFragment.Add(thisLine);
                }
            }
            return profileFragment;
        }
        
        /// <summary>
        /// Return a version of the string that has been protected for use in a comma-separated
        /// file field by quoting and escaping as necessary any special characters inside it.
        /// </summary>
        /// <param name="in"></param>
        /// <returns></returns>
        private string ProtectCSVField(string s)
        {
            bool needQuotes = s.IndexOfAny(new char[] {'"', ',', '\n', '\r'}) >= 0 ;
            
            if (!needQuotes)
                return s;
            
            return "\"" + s.Replace("\"","\"\"") + "\"";
        }
    }
}
