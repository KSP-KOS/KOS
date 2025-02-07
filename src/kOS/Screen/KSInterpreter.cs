using kOS.Safe.Compilation;
using kOS.Safe.Execution;
using kOS.Safe.Persistence;
using kOS.Safe.Screen;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS.Screen
{
    public class KSInterpreter : IInterpreter
    {
        public static readonly string[] FilenameExtensions = new string[] { Volume.KERBOSCRIPT_EXTENSION, Volume.KOS_MACHINELANGUAGE_EXTENSION };
        public string Name => "kerboscript";
        private SharedObjects Shared { get; }

        public KSInterpreter(SharedObjects shared)
        {
            Shared = shared;            
        }

        public void Boot() { }

        public void ProcessCommand(string commandText)
        {
            if (Shared.ScriptHandler == null) return;
            try
            {
                CompilerOptions options = new CompilerOptions
                {
                    LoadProgramsInSameAddressSpace = false,
                    FuncManager = Shared.FunctionManager,
                    BindManager = Shared.BindingMgr,
                    AllowClobberBuiltins = SafeHouse.Config.AllowClobberBuiltIns,
                    IsCalledFromRun = false
                };

                List<CodePart> commandParts = Shared.ScriptHandler.Compile(new InterpreterPath(Shared.Terminal as Terminal),
                    Shared.Terminal.GetCommandHistoryIndex(), commandText, Terminal.InterpreterName, options);
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

        public bool IsCommandComplete(string commandText)
        {
            return Shared.ScriptHandler.IsCommandComplete(commandText);
        }

        public bool IsWaitingForCommand()
        {
            IProgramContext context = ((CPU)Shared.Cpu).GetInterpreterContext();
            // If running from a boot script, there will be no interpreter instructions,
            // only a single OpcodeEOF.  So we check to see if the interpreter is locked,
            // which is a sign that a sub-program is running.
            return context.Program[context.InstructionPointer] is OpcodeEOF;
        }

        public void BreakExecution()
        {
            if (Shared.Cpu.GetCurrentContext() == null) return;
            Shared.Cpu.GetCurrentOpcode().AbortProgram = true;
        }

        public int InstructionsThisUpdate()
        {
            return Shared.Cpu.InstructionsThisUpdate;
        }

        public int ECInstructionsThisUpdate() => InstructionsThisUpdate();

        public void Dispose() { } // Everything is disposed in CPU

        private class InterpreterPath : InternalPath
        {
            private Terminal terminal;

            public InterpreterPath(Terminal terminal) : base()
            {
                this.terminal = terminal;
            }

            public override string Line(int line)
            {
                return terminal.GetCommandHistoryAbsolute(line);
            }

            public override string ToString()
            {
                return Terminal.InterpreterName;
            }
        }
    }
}
