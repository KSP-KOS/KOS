namespace kOS.Execution
{
    /// <summary>
    /// <p>The context record that gets pushed onto the stack to store everything
    /// that you need to know about the current nesting level when entering/leaving
    /// a subroutine.  i.e. run program, or call lock function (or other function
    /// when user functions are implemented.)</p>
    /// 
    /// <p>At the moment it only contains the instruction pointer to return to.</p>
    /// 
    /// <p>Why isn't it just an integer then?</p>
    /// 
    /// <p>It was just an integer before, but that makes it impossible to tell the
    /// difference between an integer expression value on the stack and the
    /// function's return point.  Having a special type for storing the return
    /// pointer effectively puts section separators into the stack - everything
    /// in between two SubroutineContext records on the stack belongs to that same
    /// nesting level of function call.</p>
    /// 
    /// <p>It can also be expanded to include more things than the return IP.  For
    /// example if we ever want to implement local variables, it would make sense
    /// to store a reference to the Dictionary namespace of them in this context record, so it
    /// too can be popped back when returning from the routine.</p>
    /// 
    /// <p>It is also useful if we want to implement a premature abort from a routine
    /// (i.e. exit or return statement).  With a special named class like this on
    /// the stack at the point where the current subroutine was begun, it will be
    /// possible to identify how much of the top of the stack to erease if we want to
    /// abort prematurely and return (everything on top of the topmost SubroutineContext
    /// is the stuff that was added by the current routine).</p>
    /// 
    /// <p>It also makes it possible to implement varying arguments passed to a function
    /// this way, because we can find out how many of the objects on top of the stack are
    /// the parameters to the current function - When the function starts everything in
    /// the stack above where the SubroutineContext record is will be a parameter.  (Also,
    /// it should be possible to detect the wrong number of arguments better this way, for
    /// the same reason).</p>
    /// </summary>
    public class SubroutineContext
    {
        /// <summary>The instruction pointer that this subroutine call came from, and therefore
        /// should be returned to when it's done.</summary>
        public int CameFromInstPtr {get; private set;}

        /// <summary>Make a new Subroutine Context, with all the required data.</summary>
        /// <param name="cameFromInstPtr">Sets the ComeFromIP field</param>
        public SubroutineContext(int cameFromInstPtr)
        {
            CameFromInstPtr = cameFromInstPtr;
        }
        
        public override string ToString()
        {
            return string.Format("SubroutineContext: {{CameFromInstPtr {0}}}", CameFromInstPtr);
        }
    }
}
