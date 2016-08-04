using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;

/// <summary>
/// Holds the information about a trigger, which could be
/// a user-setup trigger like WHEN or ON, or could be a
/// callback trigger invoked by our own C# code that needs
/// to get run like a trigger.
/// </summary>
public class TriggerInfo
{
    /// <summary>
    /// Where OpcodeCall should jump to in the program to run this trigger.
    /// </summary>
    public int EntryPoint { get; private set; }
    /// <summary>
    /// If true, this is a callback invoked by our own C# code, which will
    /// be awaiting the answer the user's code returns.
    /// </summary>
    public bool IsCSharpCallback { get; private set; }
    /// <summary>
    /// If this is a C# callback, this is where its return value will get
    /// stored for the C# code to read it from.
    /// </summary>
    public Structure ReturnValue { get; private set; }
    /// <summary>
    /// If this is a C# callback, this tells you if it reached the end and populated
    /// its ReturnValue yet or not.  (It's possible for a callback to take more
    /// than 1 update to reach the end of the function).
    /// </summary>
    public bool CallbackFinished { get; private set; }
    /// <summary>
    /// Describes which program context this Trigger instance is meant to
    /// be run under.  Triggers should never get run under a different
    /// program context than they were made for, because that would mean
    /// the EntryPoint refers to some instruction index from a previous,
    /// now gone, program.
    /// </summary>
    public int ContextId { get; private set; }

    /// <summary>
    /// The list of arguments that should be passed in to the trigger (Only if it is
    /// a callback.  LOCK, WHEN, and ON triggers don't take args).
    /// </summary>
    public List<Structure> Args { get; private set; }

    /// <summary>
    /// Make a new trigger for insertion into the trigger list.
    /// </summary>
    /// <param name="context">The ProgramContext under which this Trigger is meant to run.</param>
    /// <param name="entryPoint">Address within the program context where the routine starts that
    /// needs to be called when the trigger needs to be invoked.</param>
    /// <param name="isCSharpCallback">set to true to tell the system this trigger is meant to be a callback</param>
    public TriggerInfo(IProgramContext context, int entryPoint, bool isCSharpCallback = false)
    {
        EntryPoint = entryPoint;
        IsCSharpCallback = isCSharpCallback;
        ReturnValue = new ScalarIntValue(0);
        CallbackFinished = false;
        Args = new List<Structure>();
        ContextId = context.ContextId;
    }
    
    /// <summary>
    /// Make a new trigger for insertion into the trigger list, which is a callback from C# code.
    /// </summary>
    /// <param name="context">The ProgramContext under which this Trigger is meant to run.</param>
    /// <param name="entryPoint">Address within the program context where the routine starts that
    /// needs to be called when the trigger needs to be invoked.</param>
    /// <param name="args">list of the arguments to pass in to the function.  Note, the existence of
    /// arguments mandates that this is a callback trigger.</param>
    public TriggerInfo(IProgramContext context, int entryPoint, List<Structure> args)
    {
        EntryPoint = entryPoint;
        IsCSharpCallback = true;
        ReturnValue = new ScalarIntValue(0);
        CallbackFinished = false;
        Args = args;
        ContextId = context.ContextId;
    }
    
    /// <summary>
    /// Once the callback trigger is done, call this to populate the return value,
    /// and flag it as finished.
    /// </summary>
    /// <param name="val">will be converted to Structure primitive for you</param>
    public void FinishCallback(object returnedValue)
    {
        ReturnValue = (Structure) Structure.FromPrimitive(returnedValue);
        CallbackFinished = true;
    }
    
    /// <summary>
    /// Two TriggerInfos shall be considered equivalent (and thus disallowed
    /// from existing simultaneously in collections that demand uniqueness) if
    /// they are non-callback triggers that refer to the same entry
    /// point.  For callback triggers, ANY two callbacks that are not the
    /// same reference shall be consided unequal.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(object other)
    {
        TriggerInfo otherInfo = other as TriggerInfo;
        if (otherInfo == null)
            return false;

        if (IsCSharpCallback)
        {
            // For callbacks, only reference-equals are good enough.
            // Hypothetically the same chunk of user code (same entry point)
            // could have been given as a callback to two different areas of
            // our C# code, and if so we still want them to be two different
            // callbacks tracked separately.
            if (otherInfo != this)
                return false;
        }
        else
        {
            // for non-callbacks, as long as they refer to the same
            // chunk of user code, they are equal:
            if (EntryPoint != otherInfo.EntryPoint)
                return false;
        }

        return true;
    }

    /// <summary>
    /// GetHashCode() should be overridden whenever Equals() is, such that you never
    /// have a case where Equal() objects have differing hash codes.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override int GetHashCode()
    {
        int baseHashCode = base.GetHashCode();
        if (IsCSharpCallback)
            return baseHashCode;
        else
            return baseHashCode + EntryPoint.GetHashCode();
    }
    
    public override string ToString()
    {
        return string.Format("TriggerInfo: {0}:{1}:(arg count: {2})", EntryPoint, (IsCSharpCallback ? "callback" : "non-callback"), Args.Count);
    }
 
}
