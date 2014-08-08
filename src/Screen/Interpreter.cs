using System;
using System.Collections.Generic;
using System.Text;
using kOS.Utilities;
using kOS.Compilation;

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
                ProcessCommand(commandText);
                AddCommandHistoryEntry(commandText);
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
            
            if (!locked)
            {
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

        protected virtual void ProcessCommand(string commandText)
        {
            CompileCommand(commandText);
        }

        protected void CompileCommand(string commandText)
        {
            if (Shared.ScriptHandler == null) return;

            try
            {
                List<CodePart> commandParts = Shared.ScriptHandler.Compile("interpreter", commandText, "interpreter");
                if (commandParts != null)
                {
                    var interpreterContext = Shared.Cpu.GetInterpreterContext();
                    interpreterContext.AddParts(commandParts);
                }
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
