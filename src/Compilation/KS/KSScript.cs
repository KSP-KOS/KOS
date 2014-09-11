using System.Collections.Generic;

namespace kOS.Compilation.KS
{
    public class KSScript : Script
    {
        private readonly Scanner scanner;
        private readonly Parser parser;
        private readonly Dictionary<string, Context> contexts;
        private Context currentContext;

        public KSScript()
        {
            scanner = new Scanner();
            parser = new Parser(scanner);
            contexts = new Dictionary<string, Context>();
        }

        public override List<CodePart> Compile(string filePath, int startLineNum, string scriptText, string contextId, CompilerOptions options)
        {
            List<CodePart> parts = null;
            
            // make the code lowercase
            scriptText = MakeLowerCase(scriptText);
            scriptText = ReplaceIdentifiers(scriptText);
            
            //if (contextId != "interpreter") parts = _cache.GetFromCache(scriptText);

            // if parts is null means the code doesn't exists in the cache
            if (parts == null)
            {
                parts = new List<CodePart>();
                ParseTree parseTree = parser.Parse(scriptText);
                if (parseTree.Errors.Count == 0)
                {
                    var compiler = new Compiler();
                    LoadContext(contextId);

                    // TODO: handle compile errors (e.g. wrong run parameter count)
                    CodePart mainPart = compiler.Compile(startLineNum, parseTree, currentContext, options);

                    // add locks and triggers
                    parts.AddRange(currentContext.Locks.GetNewParts());
                    parts.AddRange(currentContext.Triggers.GetNewParts());
                    parts.AddRange(currentContext.Subprograms.GetNewParts());

                    parts.Add(mainPart);

                    AssignSourceId(parts, filePath);

                    //if (contextId != "interpreter") _cache.AddToCache(scriptText, parts);
                }
                else
                {
                    ParseError error = parseTree.Errors[0];
                    RaiseParseException(scriptText, error.Line, error.Position);
                } 
            }

            return parts;
        }

        private void LoadContext(string contextId)
        {
            if (contextId != string.Empty)
            {
                if (contexts.ContainsKey(contextId))
                {
                    currentContext = contexts[contextId];
                }
                else
                {
                    currentContext = new Context();
                    contexts.Add(contextId, currentContext);
                }
            }
            else
            {
                currentContext = new Context();
            }
        }

        private void AssignSourceId(IEnumerable<CodePart> parts, string fileName)
        {
            currentContext.LastSourceName = fileName;
            foreach (CodePart part in parts)
            {
                part.AssignSourceName(currentContext.LastSourceName);
            }
        }

        public override void ClearContext(string contextId)
        {
            if (contexts.ContainsKey(contextId))
            {
                contexts.Remove(contextId);
            }
        }

        public override bool IsCommandComplete(string command)
        {
            char[] commandChars = command.ToCharArray();
            int length = commandChars.Length;
            int openCurlyBrackets = 0;
            int openParentheses = 0;

            for (int n = 0; n < length; n++)
            {
                switch (commandChars[n])
                {
                    // match curly brackets
                    case '{':
                        openCurlyBrackets++;
                        break;
                    case '}':
                        openCurlyBrackets--;
                        break;
                    // match parentheses
                    case '(':
                        openParentheses++;
                        break;
                    case ')':
                        openParentheses--;
                        break;
                }
            }

            // returns true even if you wrote extra closing curly brackets/parentheses
            return (openCurlyBrackets <= 0)
                && (openParentheses <= 0);
        }
    }
}
