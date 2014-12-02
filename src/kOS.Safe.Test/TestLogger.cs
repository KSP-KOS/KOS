using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Safe.Test
{
    public class TestLogger : ILogger
    {
        public void Log(string text)
        {
            Console.WriteLine(text);
        }

        public void Log(Exception e)
        {
            throw new NotImplementedException();
        }

        public void SuperVerbose(string s)
        {
            throw new NotImplementedException();
        }
    }
}
