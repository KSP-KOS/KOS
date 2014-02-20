using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
