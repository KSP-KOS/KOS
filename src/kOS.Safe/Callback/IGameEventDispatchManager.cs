using System;
using System.Collections.Generic;
using kOS.Safe.Execution;
using kOS.Safe.Callback;

namespace kOS.Safe.Callback
{
	public interface IGameEventDispatchManager
	{
		void SetDispatcherFor(ProgramContext context);
		void RemoveDispatcherFor(ProgramContext context);
		void Clear();
	}
}
