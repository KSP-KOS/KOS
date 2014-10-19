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

        protected KOSActionParam GetRgba(object argument)
        {
            var rgba = argument as KOSActionParam;
            if (rgba != null)
            {
                return rgba;
            }
            throw new KOSCastException(argument.GetType(),typeof(KOSActionParam));
        }

        // Fully qualified name kos.Suffixed.TimeSpan used because the compiler
        // was confusing it with System.TimeSpan:
        protected kOS.Suffixed.TimeSpan GetTimeSpan(object argument)
        { 
            if (argument is kOS.Suffixed.TimeSpan)
            {
                return argument as kOS.Suffixed.TimeSpan;
            }
            else if (argument is Double || argument is int || argument is long || argument is float)
            {
                return new kOS.Suffixed.TimeSpan( Convert.ToDouble(argument) );
            }
            else
            {
                throw new KOSCastException(argument.GetType(),typeof(kOS.Suffixed.TimeSpan));
            }
        }

        protected Orbitable GetOrbitable(object argument)
        {
            if (argument is Orbitable)
            {
                return argument as Orbitable;
            }
            else
            {
                throw new KOSCastException(argument.GetType(),typeof(Orbitable));
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
