﻿using System;
using System.Collections.Generic;
using System.Text;
using kOS.Execution;
using kOS.Safe.Compilation;
using kOS.Safe.Execution;
using kOS.Safe.Screen;
using kOS.Safe.UserIO;
using kOS.Safe.Persistence;

namespace kOS.Screen
{
    public class Interpreter : TextEditor, IInterpreter
    {
        public const string InterpreterName = "interpreter";
        private readonly List<string> commandHistory = new List<string>();
        private int commandHistoryIndex;
        private bool locked;

        protected SharedObjects Shared { get; private set; }

        public Interpreter(SharedObjects shared)
        {
            Shared = shared;
        }

        protected override void NewLine()
        {
            string commandText = LineBuilder.ToString();

            if (Shared.ScriptHandler.IsCommandComplete(commandText))
            {
                base.NewLine();
                AddCommandHistoryEntry(commandText); // add to history first so that if ProcessCommand generates an exception,
                                                     // the command is present in the history to be found and printed in the
                                                     // error message.
                ProcessCommand(commandText);
                int numRows = LineSubBuffer.RowCount;
                LineSubBuffer.Wipe();
                LineSubBuffer.SetSize(numRows,ColumnCount); // refill it to its previous size
            }
            else
            {
                InsertChar('\n');
            }
        }
        
        /// <summary>
        /// Detect if the interpreter happens to be right at the start of a new command line.
        /// </summary>
        /// <returns>true if it's at the start of a new line</returns>
        public bool IsAtStartOfCommand()
        {
            return LineBuilder == null || LineBuilder.Length == 0;
        }

        public override void Type(char ch)
        {
            if (!locked)
            {
                base.Type(ch);
            }
        }

        public override bool SpecialKey(char key)
        {
            if (key == (char)UnicodeCommand.BREAK)
            {
                Shared.Cpu.BreakExecution(true);
                LineBuilder.Remove(0, LineBuilder.Length); // why isn't there a StringBuilder.Clear()?

                NewLine(); // process the now emptied line, to make it do all the updates it normally
                           // does to the screenbuffers on pressing enter.
            }

            if (locked) return false;

            switch (key)
            {
                case (char)UnicodeCommand.UPCURSORONE:
                    ShowCommandHistoryEntry(-1);
                    break;
                case (char)UnicodeCommand.DOWNCURSORONE:
                    ShowCommandHistoryEntry(1);
                    break;
                default:
                    return base.SpecialKey(key);
            }
            return true;
        }

        private void AddCommandHistoryEntry(string commandText)
        {
            if (commandHistory.Count == 0 ||
                commandText != commandHistory[commandHistory.Count - 1])
            {
                commandHistory.Add(commandText);
            }
            commandHistoryIndex = commandHistory.Count;
        }

        private void ShowCommandHistoryEntry(int deltaIndex)
        {
            if (commandHistory.Count > 0)
            {
                int newHistoryIndex = commandHistoryIndex + deltaIndex;
                if (newHistoryIndex >= 0 && newHistoryIndex < commandHistory.Count)
                {
                    commandHistoryIndex = newHistoryIndex;
                    LineBuilder = new StringBuilder();
                    LineBuilder.Append(commandHistory[commandHistoryIndex]);
                    LineCursorIndex = LineBuilder.Length;
                    MarkRowsDirty(LineSubBuffer.PositionRow, LineSubBuffer.RowCount);
                    LineSubBuffer.Wipe();
                    UpdateLineSubBuffer();
                }
            }
        }
        
        public string GetCommandHistoryAbsolute(int absoluteIndex)
        {
            return commandHistory[absoluteIndex-1];
        }

        protected virtual void ProcessCommand(string commandText)
        {
            CompileCommand(commandText);
        }

        protected void CompileCommand(string commandText)
        {
            if (Shared.ScriptHandler == null) return;

            try
            {
                CompilerOptions options = new CompilerOptions
                {
                    LoadProgramsInSameAddressSpace = false,
                    FuncManager = Shared.FunctionManager,
                    IsCalledFromRun = false
                };

                List<CodePart> commandParts = Shared.ScriptHandler.Compile(new InterpreterPath(this),
                    commandHistoryIndex, commandText, InterpreterName, options);
                if (commandParts == null) return;

                var interpreterContext = ((CPU)Shared.Cpu).GetInterpreterContext();
                interpreterContext.AddParts(commandParts);
            }
            catch (Exception e)
            {
                if (Shared.Logger != null)
                {
                    Shared.Logger.Log(e);
                }
            }
        }

        public bool IsWaitingForCommand()
        {
            IProgramContext context = ((CPU)Shared.Cpu).GetInterpreterContext();
            // If running from a boot script, there will be no interpreter instructions,
            // only a single OpcodeEOF.  So we check to see if the interpreter is locked,
            // which is a sign that a sub-program is running.
            return !locked && context.Program[context.InstructionPointer] is OpcodeEOF;
        }

        public void SetInputLock(bool isLocked)
        {
            locked = isLocked;
            if (Shared.Window != null) Shared.Window.ShowCursor = !isLocked;
            LineSubBuffer.Enabled = !isLocked;
        }

        public override void Reset()
        {
            Shared.ScriptHandler.ClearContext(InterpreterName);
            commandHistory.Clear();
            commandHistoryIndex = 0;
            base.Reset();
        }

        public override void PrintAt(string textToPrint, int row, int column)
        {
            SaveCursorPos();
            base.PrintAt(textToPrint, row, column);
            RestoreCursorPos();
        }

        private class InterpreterPath : InternalPath
        {
            private Interpreter interpreter;

            public InterpreterPath(Interpreter interpreter) : base()
            {
                this.interpreter = interpreter;
            }

            public override string Line(int line)
            {
                return interpreter.GetCommandHistoryAbsolute(line);
            }

            public override string ToString()
            {
                return InterpreterName;
            }
        }
    }
}
