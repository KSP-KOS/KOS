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

            UnityEngine.Debug.Log(text);
        }

        public void Log(Exception e)
        {
            Log(e.Message);
            UnityEngine.Debug.Log(e);
        }

        public void Log(Exception e, int instructionPointer)
        {
            Log(string.Format("{0}\nInstruction {1}", e.Message, instructionPointer));
            UnityEngine.Debug.Log(e);
        }
    }
}
