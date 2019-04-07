using System;
using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using kOS.Safe.Exceptions;

namespace kOS.Safe
{
    [kOS.Safe.Utilities.KOSNomenclature("Collection")]
    public abstract class CollectionValue<T, C> : EnumerableValue<T, C> where C : ICollection<T> where T : Structure
    {
        protected readonly C Collection;
        public bool IsReadOnly { get; set; }

        public CollectionValue(string label, C collection) : base(label, collection)
        {
            this.Collection = collection;
            AddSuffix("CLEAR", new NoArgsVoidSuffix(Clear));
        }

        protected void CheckReadOnly()
        {
            if (IsReadOnly) throw new KOSModifyReadonly();
        }
        public void Clear()
        {
            CheckReadOnly();
            Collection.Clear();
        }
    }
}

