using System;
using kOS.Suffixed;

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

        protected Vector GetVector(object argument)
        {
            var vector = argument as Vector;
            if (vector != null)
            {
                return vector;
            }
            throw new ArgumentException(string.Format("Can't cast {0} to V().", argument));
        }

        protected RgbaColor GetRgba(object argument)
        {
            var rgba = argument as RgbaColor;
            if (rgba != null)
            {
                return rgba;
            }
            throw new ArgumentException(string.Format("Can't cast {0} to RGB().", argument));
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
