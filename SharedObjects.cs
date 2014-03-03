using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.InterProcessor;
using kOS.Binding;
using kOS.Module;
using kOS.Persistence;
using kOS.Compilation;
using kOS.Execution;
using kOS.Screen;
using kOS.Factories;

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
        public UpdateHandler UpdateHandler;
        public IFactory Factory;
    }
}
