using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using System.Collections.Generic;
using System.IO;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("CraftTemplate")]
    public class CraftTemplate : Structure
    {
        public static CraftTemplate GetTemplateByName(string name, string facility)
        {
            if (string.IsNullOrEmpty(facility))
            {
                facility = "<empty>";
            }
            if (!string.IsNullOrEmpty(name))
            {
                switch (facility.ToLower())
                {
                    case ("vab"):
                        EditorDriver.editorFacility = EditorFacility.VAB;
                        break;

                    case ("sph"):
                        EditorDriver.editorFacility = EditorFacility.SPH;
                        break;

                    default:
                        throw new KOSException("Failed to load craft.  Facility " + facility + " not recognized, expected \"VAB\" or \"SPH\"");
                }
                var path = ShipConstruction.GetSavePath(name);
                var template = ShipConstruction.LoadTemplate(path);
                if (template == null)
                    throw new KOSException("Failed to load craft named \"" + name + "\" from given path:\n  " + path);
                return new CraftTemplate(path, template);
            }
            throw new KOSException("Failed to load craft named " + name + " from facility " + facility);
        }

        public static ListValue GetAllTemplates()
        {
            ListValue ret = new ListValue();
            var files = GetAllPathStrings();
            foreach (string path in files)
            {
                var template = ShipConstruction.LoadTemplate(path);
                if (template != null)
                {
                    ret.Add(new CraftTemplate(path, template));
                }
            }
            return ret;
        }

        private static List<string> GetAllPathStrings()
        {
            var ret = new List<string>();
            string path1 = KSPUtil.GetOrCreatePath("Ships/VAB");
            string path2 = KSPUtil.GetOrCreatePath("Ships/SPH");
            string path3 = KSPUtil.GetOrCreatePath("saves/" + HighLogic.SaveFolder + "/Ships/VAB");
            string path4 = KSPUtil.GetOrCreatePath("saves/" + HighLogic.SaveFolder + "/Ships/SPH");
            if (HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels)
            {
                ret.AddRange(Directory.GetFiles(path1));
                ret.AddRange(Directory.GetFiles(path2));
            }
            ret.AddRange(Directory.GetFiles(path3));
            ret.AddRange(Directory.GetFiles(path4));
            return ret;
        }

        private string path;
        private ShipTemplate template;

        public CraftTemplate(string filePath)
        {
            path = filePath;
            template = ShipConstruction.LoadTemplate(filePath);
            if (template == null)
                throw new KOSException("Failed to load template from given path:\n  " + filePath);
            InitializeSuffixes();
        }

        public CraftTemplate(string filePath, ShipTemplate temp)
        {
            path = filePath;
            template = temp;
            if (template == null)
                throw new KOSException("Failed to load template from given path:\n  " + filePath);
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => Name));
            AddSuffix("DESCRIPTION", new Suffix<StringValue>(() => Description));
            AddSuffix("EDITOR", new Suffix<StringValue>(() => Editor));
            AddSuffix("LAUNCHSITE", new Suffix<StringValue>(() => LaunchFacility));
            AddSuffix("MASS", new Suffix<ScalarValue>(() => Mass));
            AddSuffix("COST", new Suffix<ScalarValue>(() => Cost));
            AddSuffix("PARTCOUNT", new Suffix<ScalarValue>(() => PartCount));
        }

        public ShipTemplate InnerTemplate { get { return template; } }

        public string Name { get { return template.shipName; } }

        public string Description { get { return template.shipDescription; } }

        public string FilePath { get { return path; } }

        public string Editor
        {
            get
            {
                return template.shipType == (int)EditorFacility.SPH ? "SPH" : "VAB";
            }
        }

        public string LaunchFacility
        {
            get
            {
                return template.shipType == (int)EditorFacility.SPH ? "Runway" : "LaunchPad";
            }
        }

        public float Mass { get { return template.totalMass; } }

        public double Cost { get { return template.totalCost; } }

        public double PartCount { get { return template.partCount; } }

        public override string ToString()
        {
            return string.Format("CRAFTTEMPLATE(\"{0}\", \"{1}\")", Name, Editor);
        }
    }
}