using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.KS
{
    public class SubprogramCollection
    {
        private readonly Dictionary<string, Subprogram> subprograms = new Dictionary<string, Subprogram>();
        private readonly List<Subprogram> newSubprograms = new List<Subprogram>();

        public bool Contains(string subprogramName)
        {
            return subprograms.ContainsKey(subprogramName);
        }

        public Subprogram GetSubprogram(string subprogramName)
        {
            if (subprograms.ContainsKey(subprogramName))
            {
                return subprograms[subprogramName];
            }
            var subprogramObject = new Subprogram(subprogramName);
            subprograms.Add(subprogramName, subprogramObject);
            newSubprograms.Add(subprogramObject);
            return subprogramObject;
        }

        public List<CodePart> GetParts(List<Subprogram> subprogramList)
        {
            return subprogramList.Select(subprogramObject => subprogramObject.GetCodePart()).ToList();
        }

        public List<CodePart> GetParts()
        {
            return GetParts(subprograms.Values.ToList());
        }

        public List<CodePart> GetNewParts()
        {
            List<CodePart> parts = GetParts(newSubprograms);
            newSubprograms.Clear();
            return parts;
        }
    }
}