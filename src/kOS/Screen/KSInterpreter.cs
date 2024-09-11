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

        public void Boot()
        {
            Shared.UpdateHandler.AddFixedObserver(Shared.Cpu);
            Shared.ScriptHandler.ClearContext(InterpreterName);
            // TODO: ^ this line was previously in Shared.Terminal.Reset() and was being called from
            // v Shared.Cpu.Boot() putting this line here changes the order of operations. Make sure nothing got broken
            Shared.Cpu.Boot();
        }

        public void Shutdown() // Shutdown stops execution of kerboscript but keeps it alive
        {
            Shared.UpdateHandler.RemoveFixedObserver(Shared.Cpu);
            BreakExecution(true);
        }

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
                    Shared.Terminal.GetCommandHistoryIndex(), commandText, InterpreterName, options);
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

        public void BreakExecution(bool manual)
        {
            Shared.Cpu?.BreakExecution(manual);
        }

        public int InstructionsThisUpdate()
        {
            return Shared.Cpu.InstructionsThisUpdate;
        }

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
                return InterpreterName;
            }
        }
    }
}
