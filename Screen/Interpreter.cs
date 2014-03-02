using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Utilities;
using kOS.Compilation;

namespace kOS.Screen
{
    public class Interpreter : TextEditor
    {
        private SharedObjects _shared;
        private ProgramBuilder builder = new ProgramBuilder();
        private List<string> _commandHistory = new List<string>();
        private int _commandHistoryIndex = 0;
        private bool _locked = false;
        public bool BatchMode { get; set; }

        public Interpreter(SharedObjects shared)
        {
            _shared = shared;
            BatchMode = false;
        }

        protected override void NewLine()
        {
            string commandText = _lineBuilder.ToString();
            base.NewLine();

            CompileCommand(commandText);
            AddCommandHistoryEntry(commandText);
        }

        public override void Type(char ch)
        {
            if (!_locked)
            {
                base.Type(ch);
            }
        }

        public override void SpecialKey(kOSKeys key)
        {
            if (!_locked)
            {
                UpdateLineStartRow();

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
            if (_commandHistory.Count == 0 ||
                commandText != _commandHistory[_commandHistory.Count - 1])
            {
                _commandHistory.Add(commandText);
            }
            _commandHistoryIndex = _commandHistory.Count;
        }

        private void ShowCommandHistoryEntry(int deltaIndex)
        {
            if (_commandHistory.Count > 0)
            {
                int newHistoryIndex = _commandHistoryIndex + deltaIndex;
                if (newHistoryIndex >= 0 && newHistoryIndex < _commandHistory.Count)
                {
                    _commandHistoryIndex = newHistoryIndex;
                    string lastCommand = _lineBuilder.ToString();
                    string currentCommand = _commandHistory[_commandHistoryIndex].PadRight(lastCommand.Length);
                    PrintAtAbsolute(currentCommand, _lineStartRow, 0);
                    MoveCursorEndTrimmedLine(currentCommand);

                    _lineBuilder = new StringBuilder();
                    _lineBuilder.Append(_commandHistory[_commandHistoryIndex]);
                }
            }
        }

        private void MoveCursorEndTrimmedLine(string printedText)
        {
            // move the cursor to the end of the printed text
            MoveCursor(_cursorRow, (printedText.Length % _columnCount));

            // move the cursor to the end of the trimmed printed text
            int textLength = printedText.Length;
            int trimmedLength = printedText.TrimEnd().Length;

            if (textLength > trimmedLength)
            {
                MoveCursor(trimmedLength - textLength);
            }
        }

        private void CompileCommand(string commandText)
        {
            if (_shared.ScriptHandler != null)
            {
                try
                {
                    List<CodePart> commandParts = _shared.ScriptHandler.Compile(commandText, "interpreter");
                    if (commandParts != null)
                    {
                        builder.AddRange(commandParts);
                        List<Opcode> program = builder.BuildProgram(true);

                        if (_shared.Cpu != null)
                        {
                            _shared.Cpu.UpdateProgram(program);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (_shared.Logger != null)
                    {
                        _shared.Logger.Log(e.Message);
                    }
                }
            }
        }

        public void SetInputLock(bool locked)
        {
            _locked = locked;
            if (_shared.Window != null) _shared.Window.SetShowCursor(!locked);
        }

        public override void Reset()
        {
            builder = new ProgramBuilder();
            _shared.ScriptHandler.ClearContext("interpreter");
            _commandHistory.Clear();
            _commandHistoryIndex = 0;
            base.Reset();
        }

        public override void PrintAt(char character, int row, int column)
        {
            SaveCursorPos();
            base.PrintAt(character, row, column);
            RestoreCursorPos();
        }

        public override void PrintAt(string textToPrint, int row, int column)
        {
            SaveCursorPos();
            base.PrintAt(textToPrint, row, column);
            RestoreCursorPos();
        }
    }
}
