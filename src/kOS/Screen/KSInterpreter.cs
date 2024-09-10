using kOS.Safe.Compilation;
using kOS.Safe.Execution;
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
        public const string InterpreterName = "interpreter";

        protected SharedObjects Shared { get; private set; }

        public KSInterpreter(SharedObjects shared)
        {
            Shared = shared;            
        }

        public void ProcessCommand(string commandText)
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
                Shared.Terminal.GetCommandHistoryIndex(), commandText, InterpreterName, options);
            if (commandParts == null) return;

            var interpreterContext = ((CPU)Shared.Cpu).GetInterpreterContext();
            interpreterContext.AddParts(commandParts);
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

        public void BreakExecution(bool manual)
        {
            Shared.Cpu.BreakExecution(manual);
        }

        public int InstructionsThisUpdate()
        {
            return Shared.Cpu.InstructionsThisUpdate;
        }

        private class InterpreterPath : InternalPath
        {
            private Terminal interpreter;

            public InterpreterPath(Terminal interpreter) : base()
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
