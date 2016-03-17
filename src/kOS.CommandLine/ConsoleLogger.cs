using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using kOS.Safe;

namespace kOS.CommandLine
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string text)
        {
            Console.WriteLine(text);
            //throw new NotImplementedException();
        }

        public void Log(Exception e)
        {
            Console.WriteLine(e.Message);
            //throw new NotImplementedException();
        }

        public void SuperVerbose(string s)
        {
            Console.WriteLine(s);
            //throw new NotImplementedException();
        }

        public void LogWarning(string s)
        {
            Console.WriteLine(s);
            //throw new NotImplementedException();
        }

        public void LogException(Exception exception)
        {
            Console.WriteLine(exception.Message);
            //throw new NotImplementedException();
        }

        public void LogError(string s)
        {
            Console.WriteLine(s);
            //throw new NotImplementedException();
        }
    }
}
