using System;
using kOS.Context;

namespace kOS.Interpreter
{
    public class InterpreterBootup : ExecutionContext
    {
        private float bootTime;
        private float animationTime;
        private readonly char[,] buffer = new char[COLUMNS, ROWS];

        public InterpreterBootup(IExecutionContext parent)
            : base(parent) 
        {
            //ShowAnimationFrame(0);
            PrintAt("BOOTING UP...", 22, 20);

            State = ExecutionState.WAIT;
        }

        public override char[,] GetBuffer()
        {
            return buffer;
        }

        public void PrintAt(String text, int x, int y)
        {
            var cA = text.ToCharArray();

            for (var i = 0; i < text.Length; i++)
            {
                var c = cA[i];

                if (x + i >= buffer.GetLength(0)) return;

                buffer[x + i, y] = c;
            }
        }

        public override void Update(float time)
        {
            bootTime += time;
            animationTime += time;

            if (animationTime > 0.5f) animationTime -= 0.5f;
            ShowAnimationFrame(animationTime > 0.25f ? 1 : 0);

            if (bootTime > 2.25f)
            {
                State = ExecutionState.DONE;
            }
        }

        
        public void ShowAnimationFrame(int frame)
        {
            const int tX = 25;
            const int tY = 14;

            for (var y = 0; y < 6; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var sY = y + 11;
                    var sX = x + (frame * 4);

                    var c = (char)(sY * 16 + sX);

                    buffer[tX + x, tY + y] = c;
                }
            }
        }
    }
}
