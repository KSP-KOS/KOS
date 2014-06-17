using System;

namespace kOS
{
    public class Logger
    {
        protected SharedObjects Shared;
        
        public Logger()
        {
        }

        public Logger(SharedObjects shared)
        {
            Shared = shared;
        }

        public virtual void Log(string text)
        {
            LogToScreen(text);
        }

        public virtual void Log(Exception e)
        {
            LogToScreen(e.Message);
        }

        protected void LogToScreen(string text)
        {
            if (Shared != null && Shared.Screen != null)
            {
                Shared.Screen.Print(text);
            }
        }
    }
}
