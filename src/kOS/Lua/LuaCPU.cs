using kOS.Safe;
using kOS.Safe.Compilation;
using kOS.Safe.Execution;
using kOS.Safe.Utilities;
using kOS.Safe.Binding;
using kOS.Safe.Callback;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kOS.Module;

namespace kOS.Lua
{
    public class LuaCPU : CPU
    {
        public LuaCPU(SafeSharedObjects shared) : base(shared) { }
        
        // difference from base Boot method:
        // not adding a fixed observer
        // changed boot message
        // not running a boot script. its done in LuaInterpreter
        public override void Boot()
        {
            // break all running programs
            currentContext = null;
            contexts.Clear();            
            if (shared.GameEventDispatchManager != null) shared.GameEventDispatchManager.Clear();
            PushInterpreterContext();
            CurrentPriority = InterruptPriority.Normal;
            currentTime = 0;
            // clear stack (which also orphans all local variables so they can get garbage collected)
            stack.Clear();
            // clear global variables
            globalVariables.Clear();
            // clear interpreter
            if (shared.Terminal != null) shared.Terminal.Reset();
            // load functions
            if (shared.FunctionManager != null) shared.FunctionManager.Load();
            // load bindings
            if (shared.BindingMgr != null) shared.BindingMgr.Load();

            // Booting message
            if (shared.Screen != null)
            {
                shared.Screen.ClearScreen();
                string bootMessage = string.Format("kOS Operating System\nLua v{0}\n(manual at {1})\n \nProceed.\n", LuaInterpreter.LuaVersion, SafeHouse.DocumentationURL);
                shared.Screen.Print(bootMessage);
            }
        }
       
        public override Opcode GetCurrentOpcode()
        {
            return new OpcodeBogus();
        }
        
        public override void YieldProgram(YieldFinishedDetector yieldTracker) // lua TODO: implement kerboscript yielding
        {
            return;
        }
    }
}
