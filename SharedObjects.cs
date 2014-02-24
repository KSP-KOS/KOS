using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.InterProcessor;
using kOS.Bindings;
using kOS.Module;
using kOS.Persistence;
using kOS.Compilation;
using kOS.Execution;
using kOS.Screen;

namespace kOS
{
    public class SharedObjects
    {
        public Vessel Vessel;
        public CPU Cpu;
        public BindingManager BindingMgr;
        public ScreenBuffer Screen;
        public Interpreter Interpreter;
        public Script ScriptHandler;
        public Logger Logger;
        public VolumeManager VolumeMgr;
        public TermWindow Window;
        public kOSProcessor Processor;
        public ProcessorManager ProcessorMgr;
    }
}
