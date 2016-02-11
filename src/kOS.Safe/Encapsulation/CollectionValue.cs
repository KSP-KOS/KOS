using System;
using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe
{
    public abstract class CollectionValue<T, C> : EnumerableValue<T, C> where C : ICollection<T> where T : Structure
    {
        protected readonly C Collection;

        public CollectionValue(string label, C collection) : base(label, collection)
        {
            this.Collection = collection;

            InitializeCollectionSuffixes();
        }

        private void InitializeCollectionSuffixes()
        {
            AddSuffix("CLEAR", new NoArgsVoidSuffix(Collection.Clear));
        }
    }
}

