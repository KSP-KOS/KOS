using kOS.Safe.Exceptions;
using System.Collections.Generic;

namespace kOS.Safe.Compilation.KS
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
            var parts = new List<CodePart>();
            ParseTree parseTree = parser.Parse(scriptText);
            if (parseTree.Errors.Count == 0)
            {
                var compiler = new Compiler();
                LoadContext(contextId);

                // TODO: handle compile errors (e.g. wrong run parameter count)
                CodePart mainPart = compiler.Compile(startLineNum, parseTree, currentContext, options);

                // add locks and triggers
                parts.AddRange(currentContext.UserFunctions.GetNewParts());
                parts.AddRange(currentContext.Triggers.GetNewParts());
                parts.AddRange(currentContext.Subprograms.GetNewParts());

                parts.Add(mainPart);

                AssignSourceId(parts, filePath);

                //if (contextId != "interpreter") _cache.AddToCache(scriptText, parts);
            }
            else
            {
                // TODO: Come back here and check on the possiblity of reporting more
                // errors than just the first one.  It appears that TinyPG builds a
                // whole array of error messages so people could see multiple syntax
                // errors in one go if we supported the reporting of it.  It may be that
                // it was deliberately not done because it might be too verbose that way
                // for the small text terminal.

                ParseError error = parseTree.Errors[0];
                throw new KOSParseException(error, scriptText);
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
            // Good FUture Refactor Opportunity:
            //
            // It's not good that this is
            // essentially doing kerboscript-aware syntax thinking
            // somewhere outside the parser generator TinyPG.  That
            // puts the same logic in two places of the code, which
            // is usually a bad thing.  If someone can think of a
            // better way to query the parser to ask it "is this
            // statement incomplete?" rather than putting this logic
            // here, that would be a good opportunity for a refactor.
            //
            // The difficulty is that the parser returns "syntax erorr"
            // the same regardless of whether it's because it's incomplete
            // or because there's an error.  So we can't say "if parser
            // doesn't think it's done" wihtout the side effect of it hanging
            // on syntax errors because it thinks they're "incomplete".            

            char[] commandChars = command.ToCharArray();
            int length = commandChars.Length;
            int openCurlyBrackets = 0;
            int openParentheses = 0;
            bool inQuotes = false;
            bool inCommentToEoln = false;
            char curChar;
            char prevChar = '\0';
            
            for (int n = 0; n < length; n++)
            {
                curChar = commandChars[n];
                switch (curChar)
                {
                    // Track if we are in quotes or a comment, which
                    // should make it bypass the checks for matching
                    // parentheses and braces:
                    case '\"':
                        if (! inCommentToEoln)
                            inQuotes = !(inQuotes);
                        break;
                    case '/':
                        if (prevChar == '/' && !inQuotes )
                            inCommentToEoln = true;
                        break;
                    case '\n':
                    case '\r':
                        inCommentToEoln = false;
                        break;
                        
                    // match curly brackets:
                    case '{':
                        if (!inQuotes && !inCommentToEoln)
                            openCurlyBrackets++;
                        break;

                    case '}':
                        if (!inQuotes && !inCommentToEoln)
                            openCurlyBrackets--;
                        break;

                    // match parentheses:
                    case '(':
                        if (!inQuotes && !inCommentToEoln)
                            openParentheses++;
                        break;

                    case ')':
                        if (!inQuotes && !inCommentToEoln)
                            openParentheses--;
                        break;
                }
                prevChar = curChar;
            }

            // Only return true if none of the conditions that
            // indicate there's more to type are present:            
            return
                openCurlyBrackets <= 0 &&
                openParentheses <= 0   &&
                (!inQuotes);
                
        }
    }
}