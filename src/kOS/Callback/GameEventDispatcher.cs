using System;
using System.Collections.Generic;
using kOS.Safe.Execution;
using kOS.Safe.Callback;

namespace kOS.Callback
{
	/// <summary>
	/// Description of GameEventDispatchManager.
	/// </summary>
	public class GameEventDispatchManager : IGameEventDispatchManager
	{
		private Dictionary<ProgramContext, KOSGameEventDispatcher> mapping;
		private SharedObjects Shared { get; set; }
		
		public KOSGameEventDispatcher CurrentDispatcher{ get; set; }

		public GameEventDispatchManager(SharedObjects shared)
		{
		    Console.WriteLine("eraseme: GameEventDispatchManager constructor called.");
			Shared = shared;
			mapping = new Dictionary<ProgramContext, KOSGameEventDispatcher>();
			CurrentDispatcher = null;
		}

		public void SetDispatcherFor(ProgramContext context)
		{
		    Console.WriteLine("eraseme: GameEventDispatchManager.SetDistpacherFor("+context.ToString()+") called.");
		    // Return existing one, or make a new one for it:
		    if (mapping.ContainsKey(context))
		    {
		        CurrentDispatcher =  mapping[context];
		        return;
		    }
		    
			KOSGameEventDispatcher newDispatcher = new KOSGameEventDispatcher(Shared);
			mapping.Add(context, newDispatcher);
			CurrentDispatcher = newDispatcher;
		}

		public void RemoveDispatcherFor(ProgramContext context)
		{
		    Console.WriteLine("eraseme: GameEventDispatchManager.RemoveDispatcherFor("+context.ToString()+") called.");
		    // First, unset the current dispatcher if the context
		    // being removed is for the current dispatcher:
		    KOSGameEventDispatcher contextDispatcher;
		    mapping.TryGetValue(context, out contextDispatcher);
		    if (contextDispatcher == CurrentDispatcher)
		        CurrentDispatcher = null;
		    
		    // Then get rid of the mapping for this context:
		    mapping.Remove(context);
		}
		
		public void Clear()
		{
		    Console.WriteLine("eraseme: GameEventDispatchManager.Clear() called.");
		    mapping.Clear();
		    CurrentDispatcher = null;
		}
	}
}
