using System;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Compilation
{
    public class OperandPair
    {
        public object Left { get; private set; }

        public Type LeftType
        {
            get { return Left.GetType(); }
        }

        public object Right { get; private set; }

        public Type RightType
        {
            get { return Right.GetType(); }
        }

        public OperandPair(object left, object right)
        {
            Left = left;
            Right = right;
            CoerceTypes();
        }

        private void CoerceTypes()
        {
            // convert floats to doubles
            if (Right is float) Right = Convert.ToDouble(Right);
            if (Left is float) Left = Convert.ToDouble(Left);

            Left = Structure.FromPrimitive(Left);
            Right = Structure.FromPrimitive(Right);
        }
    }
}