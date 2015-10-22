﻿using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Persistence;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using kOS.Utilities;
using System;
using System.Linq;
using kOS.Module;
using kOS.Safe.Exceptions;

namespace kOS
{
    public class Core : Structure
    {
        public static VersionInfo VersionInfo;
        private readonly SharedObjects shared;

        static Core()
        {
            var ver = typeof(Core).Assembly.GetName().Version;
            VersionInfo = new VersionInfo(ver.Major, ver.Minor, ver.Build);
        }

        public Core(SharedObjects shared)
        {
            this.shared = shared;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("VERSION", new Suffix<VersionInfo>(() => VersionInfo));
            AddSuffix("PART", new Suffix<PartValue>(() => new PartValue(shared.KSPPart, shared)));
            AddSuffix("VESSEL", new Suffix<VesselTarget>(() => new VesselTarget(shared.KSPPart.vessel, shared)));
            AddSuffix("ELEMENT", new Suffix<ElementValue>(GetEelement));
            AddSuffix("VOLUME", new Suffix<Volume>(GetCurrentProcessorVolume));
            AddSuffix("BOOTFILENAME", new SetSuffix<string>(GetBootFilename, SetBootFilename, "The name of the processor's boot file."));
            AddSuffix("CURRENTVOLUME", new Suffix<Volume>(GetCurrentVolume, "The currently selected volume"));
        }

        private ElementValue GetEelement()
        {
            var elList = shared.KSPPart.vessel.PartList("elements", shared);
            var part = new PartValue(shared.KSPPart, shared);
            return elList.Cast<ElementValue>().FirstOrDefault(el => el.Parts.Contains(part));
        }

        private Volume GetCurrentProcessorVolume()
        {
            kOSProcessor processor = shared.Processor as kOSProcessor;

            if (processor == null) {
                throw new KOSException("This processor does not have a hard disk");
            }

            return processor.HardDisk;
        }

        private Volume GetCurrentVolume()
        {
            return shared.VolumeMgr.CurrentVolume;
        }

        private string GetBootFilename()
        {
            return shared.Processor.BootFilename;
        }

        private void SetBootFilename(string name)
        {
            shared.Processor.BootFilename = name;
        }
    }
}