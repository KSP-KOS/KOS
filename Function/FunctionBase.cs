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
            try
            {
                return Convert.ToDouble(argument);
            }
            catch(Exception)
            {
                throw new ArgumentException(string.Format("Can't cast {0} to double.", argument));
            }    
        }

        protected int GetInt(object argument)
        {
            try
            {
                return Convert.ToInt32(argument);
            }
            catch (Exception)
            {
                throw new ArgumentException(string.Format("Can't cast {0} to int.", argument));
            }
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
