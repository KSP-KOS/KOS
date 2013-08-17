using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS
{
    /*
    public class Interpreter : ExecutionContext
    {
        private static int COLUMNS = 50;
        private static int ROWS = 36;

        public char[,] Buffer = new char[COLUMNS, ROWS];

        public int CursorX;
        public int CursorY;

        public Interpreter(CPU cpu) : base(cpu) { }

        public virtual void Type(char ch)
        {
        }
        
        public override void ClearScreen()
        {
            for (int y = 0; y < Buffer.GetLength(1); y++)
            {
                for (int x = 0; x < Buffer.GetLength(0); x++)
                {
                    Buffer[x, y] = (char)0;
                }
            }
        }

        public override Volume GetSelectedVolume()
        {
            return Cpu.SelectedVolume;
        }

        public override BindingManager getBindingManager()
        {
            return Cpu.bindingManager;
        }

        public override Variable FindVariable(string varName)
        {
            if (variables.ContainsKey(varName.ToUpper()))
            {
                return variables[varName.ToUpper()];
            }
            else
            {
                return Cpu.FindVariable(varName);
            }
        }

        public virtual void ArrowKey(Arrows value)
        {
        }

        public virtual void FunctionKey(int fkeyNumber)
        {
            
        }

        public virtual void HomeKey()
        {
        }

        public virtual void EndKey()
        {
        }

        public virtual void Delete()
        {
        }
    }*/
}
