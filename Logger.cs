using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class Logger
    {
        private SharedObjects _shared;
        
        public Logger()
        {
        }

        public Logger(SharedObjects shared)
        {
            _shared = shared;
        }

        public void Log(string text)
        {
            if (_shared != null && _shared.Screen != null)
            {
                _shared.Screen.Print(text);
            }
        }
    }
}
