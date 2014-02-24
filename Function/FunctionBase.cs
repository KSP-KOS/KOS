using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Function
{
    public class FunctionBase
    {
        public virtual void Execute(SharedObjects shared)
        {
        }

        protected double GetDouble(object argument)
        {
            if (argument is int)
                return (double)(int)argument;
            else if (argument is double)
                return (double)argument;
            else
                throw new ArgumentException(string.Format("Can't cast {0} to double.", argument));
        }

        protected int GetInt(object argument)
        {
            if (argument is int)
                return (int)argument;
            else if (argument is double)
                return (int)(double)argument;
            else
                throw new ArgumentException(string.Format("Can't cast {0} to int.", argument));
        }

        protected double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        protected double RadiansToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }
    }
}
