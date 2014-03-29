using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class Logger
    {
        protected SharedObjects _shared;
        
        public Logger()
        {
        }

        public Logger(SharedObjects shared)
        {
            _shared = shared;
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
            if (_shared != null && _shared.Screen != null)
            {
                _shared.Screen.Print(text);
            }
        }
    }
}
