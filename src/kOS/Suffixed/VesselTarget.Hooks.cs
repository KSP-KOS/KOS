using kOS.Safe.Module;
using System;
using System.Collections.Generic;

namespace kOS.Suffixed
{
    partial class VesselTarget : IDisposable
    {
        // Having or not having reference to Vessel rules creation and destruction of hooks.
        // Hooks only have weak reference to VesselTarget, thus VesselTarget can be collected
        // even when hooks still exists. VesselTarget has hard link to call Dispose on demand.

        private Vessel _vessel;
        private Hooks _hooks;
        public Vessel Vessel
        {
            get => _vessel;
            private set
            {
                if (_vessel == value)
                    return;
                if (_hooks != null)
                {
                    _hooks.Dispose();
                    _hooks = null;
                }

                _vessel = value;

                if (_vessel != null)
                    _hooks = new Hooks(this);

                InvalidateParts();
            }
        }

        // Dispose hooks when disposing VesselTarget
        // It is better to leave fields untouched if disposing from destructor
        // that is why Dispose(true) nulls Vessel, but Dispose(false) calls _hooks?.Dispose()

        // The `disposed` flag is unnecessary in current code because Dispose()
        // is only called by ClearInstanceCache, but the Dispose method (and IDisposable) are public.
        // The flag should prevent problems if anybody decides to call Dispose() from elsewhere.

        bool disposed;
        ~VesselTarget() => Dispose(false);
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Vessel = null;
            else if (!disposed)
                _hooks?.Dispose();
        }

        /// <summary>
        /// All constructors for this class have been restricted because everyone should
        /// be calling the factory method CreateOrGetExisting() instead.
        /// </summary>
        protected VesselTarget(Vessel target, SharedObjects shared)
            : base(shared)
        {
			// Initialize this before assigning Vessel!
			// (it sets StageValues.stale = true)
            StageValues = new StageValues(shared);

            Vessel = target;
            RegisterInitializer(InitializeSuffixes);
        }

        /// <summary>
        /// Factory method you should use instead of the constructor for this class.
        /// This will construct a new instance if and only if there isn't already
        /// an instance made for this particular kOSProcessor, for the given vessel
        /// (Uniqueness determinied by the vessel's GUID).
        /// If an instance already exists it will return a reference to that instead of making
        /// a new one.
        /// The reason this enforcement is needed is because VesselTarget has callback hooks
        /// that prevent orphaning and garbage collection.  (The delegate inserted
        /// into KSP's GameEvents counts as a reference to the VesselTarget.)
        /// Using this factory method instead of a constructor prevents having thousands of stale
        /// instances of VesselTarget, which was the cause of Github issue #1980.
        /// </summary>
        public static VesselTarget CreateOrGetExisting(SharedObjects shared) =>
            CreateOrGetExisting(shared.Vessel, shared);

        private static Dictionary<InstanceKey, WeakReference> instanceCache;

        /// <summary>
        /// Factory method you should use instead of the constructor for this class.
        /// This will construct a new instance if and only if there isn't already
        /// an instance made for this particular kOSProcessor, for the given vessel
        /// (Uniqueness determinied by the vessel's GUID).
        /// If an instance already exists it will return a reference to that instead of making
        /// a new one.
        /// The reason this enforcement is needed is because VesselTarget has callback hooks
        /// that prevent orphaning and garbage collection.  (The delegate inserted
        /// into KSP's GameEvents counts as a reference to the VesselTarget.)
        /// Using this factory method instead of a constructor prevents having thousands of stale
        /// instances of VesselTarget, which was the cause of Github issue #1980.
        /// </summary>
        public static VesselTarget CreateOrGetExisting(Vessel target, SharedObjects shared)
        {
            var key = new InstanceKey(shared.Processor, target);
            if (instanceCache == null)
                instanceCache = new Dictionary<InstanceKey, WeakReference>();
            else if (instanceCache.TryGetValue(key, out WeakReference wref))
            {
                var it = wref.Target as VesselTarget;
                if (it?.disposed == false) return it;
                instanceCache.Remove(key);
            }
            // If it either wasn't in the cache, or it was but the GC destroyed it by now, make a new one:
            var newlyConstructed = new VesselTarget(target, shared);
            instanceCache.Add(key, new WeakReference(newlyConstructed));
            return newlyConstructed;
        }

        /// <summary>
        /// Dispose all VeselTarget instances - called by SceneChangeCleaner
        /// </summary>
        public static void ClearInstanceCache()
        {
            if (instanceCache == null)
                return;
            foreach (var wref in instanceCache.Values)
            {
                var it = wref.Target as VesselTarget;
                if (it != null) it.Dispose(); //dispose to force unhooking!
            }
            instanceCache.Clear();
        }

        //TODO: Use WeakReference<VesselTarget> if upgrading to .NET 4.5+
        private class Hooks : WeakReference, IDisposable
        {
            bool hooked;
            public VesselTarget VesselTarget
            {
                get
                {
                    var it = (VesselTarget)Target;
                    if (it == null && hooked) Dispose();
                    return it;
                }
            }

            public Hooks(VesselTarget target) : base(target)
            {
                GameEvents.onVesselDestroy.Add(OnVesselDestroy);
                GameEvents.onVesselPartCountChanged.Add(OnVesselPartCountChanged);
                GameEvents.onStageActivate.Add(OnStageActive);
                GameEvents.onPartPriorityChanged.Add(OnPartPriorityChanged);
                GameEvents.StageManager.OnGUIStageAdded.Add(OnStageAdded);
                GameEvents.StageManager.OnGUIStageRemoved.Add(OnStageRemoved);
                GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnStageModified);
                hooked = true;
            }
            ~Hooks() => Dispose(false);
            public void Dispose() => Dispose(true);
            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                    Target = null;
                }
                if (hooked)
                {
                    GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
                    GameEvents.onVesselPartCountChanged.Remove(OnVesselPartCountChanged);
                    GameEvents.onStageActivate.Remove(OnStageActive);
                    GameEvents.onPartPriorityChanged.Remove(OnPartPriorityChanged);
                    GameEvents.StageManager.OnGUIStageAdded.Remove(OnStageAdded);
                    GameEvents.StageManager.OnGUIStageRemoved.Remove(OnStageRemoved);
                    GameEvents.StageManager.OnGUIStageSequenceModified.Remove(OnStageModified);
                    hooked = false;
                }
            }

            private void OnStageActive(int stage) =>
                VesselTarget?.InvalidateParts();
            private void OnStageAdded(int stage) =>
                VesselTarget?.InvalidateParts();
            private void OnStageRemoved(int stage) =>
                VesselTarget?.InvalidateParts();
            private void OnStageModified() =>
                VesselTarget?.InvalidateParts();

            private void OnPartPriorityChanged(global::Part part)
            {
                var target = VesselTarget;
                if (target != null && part.vessel == target.Vessel)
                    target.InvalidateParts();
            }

            private void OnVesselPartCountChanged(Vessel v)
            {
                var target = VesselTarget;
                if (target != null && target.Vessel.Equals(v))
                    target.InvalidateParts();
            }
            private void OnVesselDestroy(Vessel v)
            {
                if (VesselTarget?.Vessel.Equals(v) == true)
                    Dispose();
            }
        }

        // Composed key that identifies unique instance of VesselTarget per processor.
        // Used by VesselTarget.CreateOrGetExisting

        // NOTE: ThrowIfNotCPUVessel() and such would have to be changed
        // in order to have globally uniqie instances
        private struct InstanceKey: IEquatable<InstanceKey>
        {
            /// <summary>The kOSProcessor Module that built me.</summary>
            public int ProcessorId { get; }

            /// <summary>The KSP vessel object that I'm wrapping.</summary>
            public Guid VesselId { get; }

            public InstanceKey(IProcessor processor, Vessel vessel)
            {
                ProcessorId = processor.KOSCoreId;
                VesselId = vessel.id;
            }

            public bool Equals(InstanceKey other) =>
                ProcessorId == other.ProcessorId && VesselId == other.VesselId;
            public override bool Equals(object obj) =>
                obj is InstanceKey key && Equals(key);
            public override int GetHashCode() =>
                VesselId.GetHashCode() ^ (3001 * ProcessorId); //3001 is prime number
        }
    }
}