using System;
using kOS.Suffixed;
using kOS.Safe.Exceptions;

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
                throw new KOSCastException(argument.GetType(),typeof(Double));
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
                throw new KOSCastException(argument.GetType(),typeof(Int32));
            }
        }

        protected Vector GetVector(object argument)
        {
            var vector = argument as Vector;
            if (vector != null)
            {
                return vector;
            }
            throw new KOSCastException(argument.GetType(),typeof(Vector));
        }

        protected RgbaColor GetRgba(object argument)
        {
            var rgba = argument as RgbaColor;
            if (rgba != null)
            {
                return rgba;
            }
            throw new KOSCastException(argument.GetType(),typeof(RgbaColor));
        }

        protected Suffixed.TimeSpan GetTimeSpan(object argument)
        {
            if (argument is Suffixed.TimeSpan)
            {
                return argument as Suffixed.TimeSpan;
            }
            if (argument is Double || argument is int || argument is long || argument is float)
            {
                return new Suffixed.TimeSpan( Convert.ToDouble(argument) );
            }
            throw new KOSCastException(argument.GetType(),typeof(Suffixed.TimeSpan));
        }

        protected Orbitable GetOrbitable(object argument)
        {
            if (argument is Orbitable)
            {
                return argument as Orbitable;
            }
            throw new KOSCastException(argument.GetType(),typeof(Orbitable));
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
