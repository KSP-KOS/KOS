﻿using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Suffixed;
using System;
using TimeSpan = kOS.Suffixed.TimeSpan;

namespace kOS.Function
{
    public abstract class FunctionBase : SafeFunctionBase
    {
        public abstract void Execute(SharedObjects shared);

        public override void Execute(Safe.SharedObjects shared)
        {
            Execute(shared as SharedObjects);
        }

        protected Vector GetVector(object argument)
        {
            var vector = argument as Vector;
            if (vector != null)
            {
                return vector;
            }
            throw new KOSCastException(argument.GetType(), typeof(Vector));
        }

        protected RgbaColor GetRgba(object argument)
        {
            var rgba = argument as RgbaColor;
            if (rgba != null)
            {
                return rgba;
            }
            throw new KOSCastException(argument.GetType(), typeof(RgbaColor));
        }

        protected TimeSpan GetTimeSpan(object argument)
        {
            var span = argument as TimeSpan;
            if (span != null)
            {
                return span;
            }
            try
            {
                // Convert to double instead of cast in case the identifier is stored
                // as an encapsulated ScalarValue, preventing an unboxing collision.
                return new TimeSpan(Convert.ToDouble(argument));
            }
            catch
            {
                throw new KOSCastException(argument.GetType(), typeof(TimeSpan));
            }
        }

        protected Orbitable GetOrbitable(object argument)
        {
            var orbitable = argument as Orbitable;
            if (orbitable != null)
            {
                return orbitable;
            }
            throw new KOSCastException(argument.GetType(), typeof(Orbitable));
        }
    }
}