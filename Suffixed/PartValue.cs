using System.Collections.Generic;

namespace kOS.Suffixed
{
    public class PartValue : SpecialValue
    {
        protected Part Part { get; private set; }

        public PartValue(Part part)
        {
            Part = part;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "NAME":
                    return Part.name;
                case "STAGE":
                    return Part.inverseStage;
                case "UID":
                    return Part.uid;
                case "RESOURCES":
                    var resources = new ListValue();
                    foreach (PartResource resource in Part.Resources)
                    {
                        resources.Add(new ResourceValue(resource));
                    }
                    return resources;
            }
            return base.GetSuffix(suffixName);
        }
        public override string ToString()
        {
            return string.Format("PART({0},{1})", Part.name, Part.uid);
        }

        public static ListValue PartsToList(IEnumerable<Part> parts)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                toReturn.Add(new PartValue(part));
            }
            return toReturn;
        }
    }
}