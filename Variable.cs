using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class Variable
    {
        private object value = 0.0f;
        public virtual object Value
        {
            get 
            { 
                return this.value; 
            }
            set 
            { 
                this.value = value; 
            }
        }

        public Variable()
        {
        }

        public virtual object GetSubValue(string svName)
        {
            return 0.0f;
        }
    }
}
