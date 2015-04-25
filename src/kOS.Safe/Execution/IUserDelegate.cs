using System.Collections.Generic;

namespace kOS.Safe.Execution
{
    /// <summary>
    /// A callback reference to a user-land function, implemented in kRISC code<br/>
    /// <br/>
    /// (As opposed to being a C# delegate, implemented in C# code).<br/>
    /// </summary>
    public interface IUserDelegate
    {
        IProgramContext ProgContext {get;}
        int EntryPoint {get;}
        List<VariableScope> Closure {get;}
    }
}
