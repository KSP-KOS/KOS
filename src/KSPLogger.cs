using System;
using System.Collections.Generic;
using System.Text;

namespace kOS
{
    public class KSPLogger : Logger
    {
        public KSPLogger(SharedObjects shared) : base(shared)
        {
        }

        public override void Log(string text)
        {
            base.Log(text);
            UnityEngine.Debug.Log(text);
        }

        public override void Log(Exception e)
        {
            base.Log(e);
            // print the call stack
            UnityEngine.Debug.Log(e);
            // print a fragment of the code where the exception ocurred
            List<string> codeFragment = Shared.Cpu.GetCodeFragment(16);
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Code Fragment");
            foreach (string instruction in codeFragment)
                messageBuilder.AppendLine(instruction);
            UnityEngine.Debug.Log(messageBuilder.ToString());
        }
    }
}
