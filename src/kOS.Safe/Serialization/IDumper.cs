using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Serialization
{
    public interface IDumperContext : IDisposable
    {
        Dump Convert(IDumper conversionTarget);
    }

    public class DumperState
    {
        private class DumperContext : IDumperContext
        {
            private DumperState state;
            private object contextHolder;
            private bool disposed = false;
            public DumperContext(DumperState state, object contextHolder)
            {
                if (contextHolder == null)
                    throw new ArgumentNullException("Context holder cannot be null", "contextHolder");

                this.state = state;
                this.contextHolder = contextHolder;

                state.seenList.Add(contextHolder);
            }

            public void Dispose()
            {
                if (this.disposed)
                    return;

                var lastItem = state.seenList.Last();
                if (lastItem != contextHolder)
                    throw new KOSYouShouldNeverSeeThisException("Context accounting failure during serialization.");

                state.seenList.RemoveAt(state.seenList.Count - 1);
            }

            public Dump Convert(IDumper conversionTarget)
            {
                if (state.seenList.Contains(conversionTarget))
                    return new DumpRecursionPlaceholder();
                return conversionTarget.Dump(state);
            }
        }
        public DumperState()
        {
            seenList = new List<object>();
        }

        private List<object> seenList;
        public IDumperContext Context(IDumper contextHolder)
        {
            return new DumperContext(this, contextHolder);
        }
    }

    public interface IDumper
    {
        Dump Dump(DumperState s);
    }
}
