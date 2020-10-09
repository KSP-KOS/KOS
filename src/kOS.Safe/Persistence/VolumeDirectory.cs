using System;
using kOS.Safe.Persistence;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections;

namespace kOS.Safe
{
    [kOS.Safe.Utilities.KOSNomenclature("VolumeDirectory")]
    public abstract class VolumeDirectory : VolumeItem, IEnumerable<VolumeItem>
    {
        public VolumeDirectory(Volume volume, VolumePath path) : base(volume, path)
        {
            InitializeSuffixes();
        }

        public Lexicon ListAsLexicon()
        {
            Lexicon result = new Lexicon();

            foreach (KeyValuePair<string, VolumeItem> entry in List())
            {
                result.Add(new StringValue(entry.Key), entry.Value);
            }

            return result;
        }

        public abstract IDictionary<string, VolumeItem> List();

        public IEnumerator<VolumeItem> GetEnumerator()
        {
            return List().Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ITERATOR", new NoArgsSuffix<Enumerator>(() => new Enumerator(GetEnumerator())));
            AddSuffix(new string[] { "LIST","LEXICON","LEX"}, new Suffix<Lexicon>(ListAsLexicon));
        }
    }
}
