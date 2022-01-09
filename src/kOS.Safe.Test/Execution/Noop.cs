using kOS.Safe.Callback;
using kOS.Safe.Execution;
using kOS.Safe.Module;
using kOS.Safe.Persistence;
using System;

namespace kOS.Safe.Test.Execution
{
    internal class NoopLogger : ILogger
    {
        public virtual void Log(Exception e)
        {
            Console.WriteLine(e);
        }

        public void Log(string text)
        {
        }

        public virtual void LogError(string s)
        {
            Console.WriteLine(s);
        }

        public virtual void LogException(Exception exception)
        {
            Console.WriteLine(exception);
        }

        public void LogWarning(string s)
        {
        }

        public void LogWarningAndScreen(string s)
        {
        }

        public void SuperVerbose(string s)
        {
        }
    }

    internal class NoopGameEventDispatchManager : IGameEventDispatchManager
    {
        public void Clear()
        {
        }

        public void RemoveDispatcherFor(ProgramContext context)
        {
        }

        public void SetDispatcherFor(ProgramContext context)
        {
        }
    }

    internal class NoopProcessor : IProcessor
    {
        public VolumePath BootFilePath
        {
            get
            {
                return null;
            }
        }

        public int KOSCoreId
        {
            get
            {
                return 0;
            }
        }

        public string Tag
        {
            get
            {
                return String.Empty;
            }
        }

        public bool CheckCanBoot()
        {
            return true;
        }

        public void SetMode(ProcessorModes newProcessorMode)
        {
        }
    }
}