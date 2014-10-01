using System;
using System.Collections.Generic;
using System.Text;
using kOS.Execution;
using kOS.Safe.Compilation;
using kOS.Safe.Screen;
using kOS.Safe.Utilities;
using kOS.Utilities;

namespace kOS.Screen
{
    public class Interpreter : TextEditor
    {
        private readonly List<string> commandHistory = new List<string>();
        private int commandHistoryIndex;
        private bool locked;

        public Interpreter(SharedObjects shared)
        {
            Shared = shared;
        }

        protected SharedObjects Shared { get; private set; }

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
            }
            else
            {
                InsertChar('\n');
            }
        }

        public override void Type(char ch)
        {
            if (!locked)
            {
                base.Type(ch);
            }
        }

        public override void SpecialKey(kOSKeys key)
        {
            if (key == kOSKeys.BREAK)
            {
                Shared.Cpu.BreakExecution(true);
            }

            if (locked) return;

            switch (key)
            {
                case kOSKeys.UP:
                    ShowCommandHistoryEntry(-1);
                    break;
                case kOSKeys.DOWN:
                    ShowCommandHistoryEntry(1);
                    break;
                default:
                    base.SpecialKey(key);
                    break;
            }
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
                List<CodePart> commandParts = Shared.ScriptHandler.Compile("interpreter history", commandHistoryIndex, commandText, "interpreter");
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

        public void SetInputLock(bool isLocked)
        {
            locked = isLocked;
            if (Shared.Window != null) Shared.Window.SetShowCursor(!isLocked);
            LineSubBuffer.Enabled = !isLocked;
        }

        public override void Reset()
        {
            Shared.ScriptHandler.ClearContext("interpreter");
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
    }
}
