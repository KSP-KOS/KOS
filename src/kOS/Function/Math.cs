using System;
using kOS.Safe.Compilation;
using kOS.Safe.Function;
using kOS.Suffixed;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation;

namespace kOS.Function
{
    [Function("vcrs", "vectorcrossproduct")]
    public class FunctionVectorCross : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = GetVector(PopValueAssert(shared));
            var vector1 = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            if (vector1 != null && vector2 != null)
            {
                object result = new Vector(Vector3d.Cross(vector1, vector2));
                ReturnValue = result;
            }
            else
                throw new KOSException("vector cross product attempted with a non-vector value");
        }
    }

    [Function("vdot", "vectordotproduct")]
    public class FunctionVectorDot : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = GetVector(PopValueAssert(shared));
            var vector1 = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            if (vector1 != null && vector2 != null)
            {
                object result = Vector3d.Dot(vector1, vector2);
                ReturnValue = result;
            }
            else
                throw new KOSException("vector dot product attempted with a non-vector value");
        }
    }

    [Function("vxcl", "vectorexclude")]
    public class FunctionVectorExclude : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = GetVector(PopValueAssert(shared));
            var vector1 = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            if (vector1 != null && vector2 != null)
            {
                object result = new Vector(Vector3d.Exclude(vector1, vector2));
                ReturnValue = result;
            }
            else
                throw new KOSException("vector exclude attempted with a non-vector value");
        }
    }

    [Function("vang", "vectorangle")]
    public class FunctionVectorAngle : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vector2 = GetVector(PopValueAssert(shared));
            var vector1 = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            if (vector1 != null && vector2 != null)
            {
                object result = Vector3d.Angle(vector1, vector2);
                ReturnValue = result;
            }
            else
                throw new KOSException("vector angle calculation attempted with a non-vector value");
        }
    }
}
