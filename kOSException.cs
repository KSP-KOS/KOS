using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class kOSException : Exception
    {
        public new String Message;
        public String Filename;
        public int LineNumber;
        public Command commandObj;

        public kOSException(String message)
        {
            this.Message = message;
            //this.commandObj = commandObj;
        }
    }
}
