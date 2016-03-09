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

        public List<Opcode> Program { get; set; }
        public int InstructionPointer { get; set; }
        public List<int> Triggers { get; set; }
        public bool Silent { get; set; }

        public ProgramContext(bool interpreterContext)
        {
            Program = new List<Opcode>();
            InstructionPointer = 0;
            Triggers = new List<int>();
            builder = interpreterContext ? new ProgramBuilderInterpreter() : new ProgramBuilder();
            flyByWire = new Dictionary<string, bool>();
        }

        public ProgramContext(bool interpreterContext, List<Opcode> program) : this(interpreterContext)
        {
            Program = program;
        }

        public void AddParts(IEnumerable<CodePart> parts)
        {
            builder.AddRange(parts);
            List<Opcode> newProgram = builder.BuildProgram();
            UpdateProgram(newProgram);
        }

        public int AddObjectParts(IEnumerable<CodePart> parts)
        {
            Guid objectFileId = builder.AddObjectFile(parts);
            List<Opcode> newProgram = builder.BuildProgram();
            int entryPointAddress = builder.GetObjectFileEntryPointAddress(objectFileId);
            UpdateProgram(newProgram);
            return entryPointAddress;
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
                    manager.ToggleFlyByWire(kvp.Key, false);
                }
            }
        }

        public void EnableActiveFlyByWire(IBindingManager manager)
        {
            foreach (KeyValuePair<string, bool> kvp in flyByWire) {
                manager.ToggleFlyByWire(kvp.Key, kvp.Value);
            }
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
                        Program[index].SourceName,
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
