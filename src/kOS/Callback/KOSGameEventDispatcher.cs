using System;
using System.Collections.Generic;
using kOS.Safe.Execution;
using kOS.Safe.Encapsulation;
using kOS.Safe.Callback;
using kOS;
using kOS.Suffixed;
using kOS.Suffixed.Part;

namespace kOS.Callback
{
	/// <summary>
	/// Acts as the bridge between the KSP's GameEvents system and
	/// the kOS UserDelegate functions the user script registers with it.<br/>
	/// <br/>
	/// The kOS program cannot be a direct responder to KSP's GameEvents, however
	/// we can have our own KSP game event callbacks that translate the KSP
	/// game event into the equivalent kOS call, and schedule an AddTrigger()
	/// call for each registered kOS function.  This class's purpose is to
	/// provide that layer.<br/>
	/// <br/>
	/// There should be exactly one instance of this class per instance of the
	/// running program environment (i.e. one per "ProgramContext").
	/// </summary>
	public class KOSGameEventDispatcher
	{
		// Note: It turned out to be nearly impossible (as far as we can tell)
		// to make this completely generic because stock KSP didn't provide a base
		// class for the various EventData<T>, EventData<T,U>, EventData<T,U,V>,
		// and EventData(T,U,V,W) event handler classes.  So instead this had to
		// be a lot of the same code cut and pasted.
		//
		// To add a new hook to this handler:
		// ----------------------------------
		//
		//   - Search this file for all cases where there's a comment
		//   saying "WHEN IMPLEMENTING A NEW EVENT TYPE", and they will
		//   tell you where code should be added to this class.

		private SharedObjects Shared { get; set; }

		// We should construct an instance of this class whenever a new ProgramContext
		// is getting run in the CPU.  The hooks this class tracks are only good for
		// the life of one ProgramContext.
		public KOSGameEventDispatcher(SharedObjects shared)
		{
			Shared = shared;
		}

		// We should Dispose of this instance whenever a program crashes or exits normally, because
		// then all the UserDelegates it had are no longer valid and the attempts this instance
		// will make to Shared.Cpu.AddTrigger() run those UserDelegates will just be denied and
		// silently ignored anyway.
		public void Dispose()
		{
			ClearLists();
		}

		/// <summary>
		/// Will return false if this UserDelegate shouldn't be called right now because
		/// the current program context isn't the one the user delegate is for, or
		/// because the delegate is a dummy placeholder.
		/// </summary>
		/// <param name="del"></param>
		/// <returns></returns>
		private bool UserDelgateIsAcceptable(UserDelegate del)
		{
			return del != null && !(del is DoNothingDelegate) && del.ProgContext.ContextId == Shared.Cpu.GetCurrentContext().ContextId;
		}

		// WHEN IMPLEMENTING A NEW EVENT TYPE:  Add a line to the following section
		// similar to the ones you see here.
		// Note that you want to make the data structure such that it contains one of these:
		//
		//       UniqueSetValue<UserDelegate>
		//
		// At the bottom level of it, so you can hand that back to the script for it to
		// manipulate directly with normal List commands.  UniqueSetValue<T> is used instead of List<T>
		// precisely for this reason.  Note that if you need additional breakdowns into sub-lists,
		// like for example, a different list of hooks per vessel, then make a Dictionary that maps
		// from that breakdown into the variousUniqueSetValue<UserDelegate>'s.  (See soiChangeNotifyees
		// below as an example.  It makes a separate list of delegates per vessel, specifically so
		// that it is possible for VesselTarget's GetSuffix to return one of those sub-lists.)
		//
		// Also, leave these as null.  Don't initialize them to an empty list until later, during
		// the associated GetFooNotifyees() call you make down below.  This way you can detect whether
		// or not it's the very first time the list has been requested, and use that to perform your
		// initial setup of the GameEvent hook.

		private UniqueSetValue<UserDelegate> switchVesselNotifyees = null;
		private Dictionary<Vessel, UniqueSetValue<UserDelegate>> soiChangeNotifyees = null;
		private Dictionary<Vessel, UniqueSetValue<UserDelegate>> stageSeparationNotifyees = null;
		private Dictionary<Part, UniqueSetValue<UserDelegate>> partCoupleNotifyees = null;
		private Dictionary<Part, UniqueSetValue<UserDelegate>> partUndockNotifyees = null;

		private void ClearLists()
		{
			// WHEN IMPLEMENTING A NEW EVENT TYPE: Add a few lines here similar to to the ones you see below.
			// (Clear the list if it's been initialized, and remove the GameEvent callback delegate.)

		    Console.WriteLine("eraseme: ClearLists() being called.");
		    if (soiChangeNotifyees != null ) soiChangeNotifyees.Clear();
			GameEvents.onVesselSOIChanged.Remove(SendToSOIChangeNotifyees);

			if (switchVesselNotifyees != null ) switchVesselNotifyees.Clear();
			GameEvents.onVesselSwitching.Remove(SendToSwitchVesselNotifyees);

			if (stageSeparationNotifyees != null ) stageSeparationNotifyees.Clear();
			GameEvents.onStageSeparation.Remove(SendToStageSeparationNotifyees);

			if (partCoupleNotifyees != null ) partCoupleNotifyees.Clear();
			GameEvents.onPartCouple.Remove(SendToPartCouplingNotifyees);

			if (partUndockNotifyees != null ) partUndockNotifyees.Clear();
			GameEvents.onPartUndock.Remove(SendToPartUndockNotifyees);
		}
		
		// WHEN IMPLEMENTING A NEW EVENT TYPE:
		// Add a GetFooNotifyees() and a SendToFooNotifyees() method for whatever new "Foo" event you're
		// trying to support.  Use the examples below as guides.
		//
		// The general algorithm of such methods should be:
		//
		// GetFooNotifyees(SomeKSPType thingy):  // Some cases might not need the thingy arg.
		// ..................................................................................
		//   - Given which "thingy" is being watched for, return the list of UserDelegates registered for this event on that thingy.
		//       This List should be a UniqueSetValue and it should be passed directly back to the user script in a GetSuffix call.
		//       (There is no explicit "add a userdelegate to the list" call in this class.  Instead it expects the user to add
		//       hooks by adding them to the UniqueSetValue<> this returns.).
		//   - We may as well not clutter up KSP's callback events system with pointless callbacks that don't do anything.
		//       So only when there is evidence the system is being used by the script because GetFooNotifyees() is getting
		//       called, do we then register our SendToFooNotiyees() that goes with it.  That registration is a bit like a
		//       "lazy-load" logic.
		//
		// SendToFooNotifyees() : The dispatcher for this event.
		// .....................................................
		//   - This must be a callback delegate that we register as our hook for whatever GameEvent Foo is.
		//   - When we receive GameEvent foo, this method must check our list of UserDelegates, and if the list isn't
		//       empty, then for each acceptable UserDelegate in the list, we schedule a call
		//       to that user delegate with an AddTrigger() call.
		//   - Note: There isn't a good way to know when the users script is done using the callbacks system, so once
		//       registered, the callback hook we make here will remain alive for the duration of this object, until
		//       Dispose() makes it go away.  It shouldn't waste too much execution time, though.  If the user is done
		//       using the system, all we'll be doing is noticing the UniqueSetValue<UserDelegate> is empty, and returning
		//       early with nothing to do.

		// SwitchActiveVessel:
		// -------------------
		public UniqueSetValue<UserDelegate> GetSwitchVesselNotifyees()
		{
			UniqueSetValue<UserDelegate> theList = switchVesselNotifyees;
			if (theList == null) {
				// This is where we lazy-build it if it's not there yet.
				switchVesselNotifyees = new UniqueSetValue<UserDelegate>();
				theList = switchVesselNotifyees;
				// Now that we know it's likely getting used, activate our own callback hook:
				Console.WriteLine("eraseme: Adding GameEvents.onVesselSwitching");
				GameEvents.onVesselSwitching.Add(SendToSwitchVesselNotifyees);
			}
			Console.WriteLine("eraseme: GetSwitchVesselNotifyees() is about to return a list of size " + theList.Count().ToString() );
			return theList;
		}

		public void SendToSwitchVesselNotifyees(Vessel fromVes, Vessel toVes)
		{
		    Console.WriteLine("eraseme: SendToSwitchVesselNotifyees() has been started.");
			UniqueSetValue<UserDelegate> notifyees = GetSwitchVesselNotifyees();
			foreach (UserDelegate del in notifyees)
				if (UserDelgateIsAcceptable(del))
					Shared.Cpu.AddTrigger(del, new VesselTarget(fromVes, Shared), new VesselTarget(toVes, Shared));
		}

		// SOIChange:
		// ----------
		public UniqueSetValue<UserDelegate> GetSOIChangeNotifyees(Vessel ves)
		{
			UniqueSetValue<UserDelegate> theList;
			if (soiChangeNotifyees == null)
			    soiChangeNotifyees = new Dictionary<Vessel, UniqueSetValue<UserDelegate>>();
			if (!soiChangeNotifyees.TryGetValue(ves, out theList)) {
				// This is where we lazy-build it if it's not there yet.
				theList = new UniqueSetValue<UserDelegate>();
				soiChangeNotifyees[ves] = theList;
				// Now that we know it's likely getting used, activate our own callback hook:
				GameEvents.onVesselSOIChanged.Add(SendToSOIChangeNotifyees);
			}
			return theList;
		}

		public void SendToSOIChangeNotifyees(GameEvents.HostedFromToAction<Vessel, CelestialBody> evt)
		{
			UniqueSetValue<UserDelegate> notifyees = GetSOIChangeNotifyees(evt.host);
			foreach (UserDelegate del in notifyees)
				if (UserDelgateIsAcceptable(del))
					Shared.Cpu.AddTrigger(del, new BodyTarget(evt.@from, Shared), new BodyTarget(evt.to, Shared));
		}

		// Staging:
		// --------
		public UniqueSetValue<UserDelegate> GetStageSeparationNotifyees(Vessel ves)
		{
			UniqueSetValue<UserDelegate> theList;
			if (stageSeparationNotifyees == null)
			    stageSeparationNotifyees = new Dictionary<Vessel, UniqueSetValue<UserDelegate>>();
			if (!stageSeparationNotifyees.TryGetValue(ves, out theList)) {
				// This is where we lazy-build it if it's not there yet.
				theList = new UniqueSetValue<UserDelegate>();
				stageSeparationNotifyees[ves] = theList;
				// Now that we know it's likely getting used, activate our own callback hook:
				GameEvents.onStageSeparation.Add(SendToStageSeparationNotifyees);
			}
			return theList;
		}

		public void SendToStageSeparationNotifyees(EventReport evt)
		{
		    Console.WriteLine("eraseme: SendToStageSeparationNotifyees just got called.");
		    Console.WriteLine("eraseme:    with evt.origin = " + evt.origin.ToString());
		    Console.WriteLine("eraseme:    with evt.origin.vessel = " + evt.origin.vessel.ToString());
			UniqueSetValue<UserDelegate> notifyees = GetStageSeparationNotifyees(evt.origin.vessel);
			foreach (UserDelegate del in notifyees)
				if (UserDelgateIsAcceptable(del))
			        Shared.Cpu.AddTrigger(del, PartValueFactory.Construct(evt.origin, Shared), new ScalarIntValue(evt.stage));
		}

		// PartCouple:
		// -----------
		public UniqueSetValue<UserDelegate> GetPartCoupleNotifyees(Part p)
		{
		    if (partCoupleNotifyees == null)
		        partCoupleNotifyees = new Dictionary<Part, UniqueSetValue<UserDelegate>>();
			UniqueSetValue<UserDelegate> theList;
			if (!partCoupleNotifyees.TryGetValue(p, out theList)) {
				// This is where we lazy-build it if it's not there yet.
				theList = new UniqueSetValue<UserDelegate>();
				partCoupleNotifyees[p] = theList;
				// Now that we know it's likely getting used, activate our own callback hook:
				GameEvents.onPartCouple.Add(SendToPartCouplingNotifyees);
			}
			return theList;
		}

		public void SendToPartCouplingNotifyees(GameEvents.FromToAction<Part, Part> evt)
		{		    
			// Notify any hooks attached to the part on the "from" side of the event:
			UniqueSetValue<UserDelegate> notifyees = GetPartCoupleNotifyees(evt.@from);
			// Use GetFooNotifyees to activate the lazy-build logic if need be.
			foreach (UserDelegate del in notifyees)
				if (UserDelgateIsAcceptable(del))
					Shared.Cpu.AddTrigger(del, PartValueFactory.Construct(evt.to, Shared));

			// Also notify any hooks attached to the part on the "to" side of the event.  Let's not
			// confuse users with KSP's strange notion of the "from" and the "to" of a docking, and just
			// treat both ports as equal peers that both get the same notification:
			notifyees = GetPartCoupleNotifyees(evt.to);
			foreach (UserDelegate del in notifyees)
			    if (UserDelgateIsAcceptable(del))
					Shared.Cpu.AddTrigger(del, PartValueFactory.Construct(evt.@from, Shared));
		}

		// PartUndock:
		// -----------
		public UniqueSetValue<UserDelegate> GetPartUndockNotifyees(Part p)
		{
		    if (partUndockNotifyees == null)
		        partUndockNotifyees = new Dictionary<Part, UniqueSetValue<UserDelegate>>();
			UniqueSetValue<UserDelegate> theList;
			if (!partUndockNotifyees.TryGetValue(p, out theList)) {
				// This is where we lazy-build it if it's not there yet.
				theList = new UniqueSetValue<UserDelegate>();
				partUndockNotifyees[p] = theList;
				// Now that we know it's likely getting used, activate our own callback hook:
				GameEvents.onPartUndock.Add(SendToPartUndockNotifyees);
			}
			return theList;
		}

		public void SendToPartUndockNotifyees(Part p)
		{
			// Notify any hooks attached to the part on the "from" side of the event:
			UniqueSetValue<UserDelegate> notifyees = GetPartUndockNotifyees(p);
			foreach (UserDelegate del in notifyees)
				if (UserDelgateIsAcceptable(del))
					Shared.Cpu.AddTrigger(del);

			// Notify any hooks attached to the part on the "to" side of the event:
			ModuleDockingNode dockModule = (ModuleDockingNode) p.Modules["ModuleDockingNode"];
			notifyees = GetPartUndockNotifyees(dockModule.otherNode.part);
			foreach (UserDelegate del in notifyees)
				if (UserDelgateIsAcceptable(del))
					Shared.Cpu.AddTrigger(del);

			// The event has no data available on which other part it had been attached to, apparently.
		}
	}
}