using System;
using kOS.Safe.Persistence;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe
{
    public abstract class VolumeDirectory : VolumeItem
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

        private void InitializeSuffixes()
        {
            AddSuffix("LIST", new Suffix<Lexicon>(ListAsLexicon));
        }
    }
}
