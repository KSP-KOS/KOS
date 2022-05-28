using kOS.Safe.Exceptions;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using kOS.Safe.Persistence;

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

        public override List<CodePart> Compile(GlobalPath filePath, int startLineNum, string scriptText, string contextId, CompilerOptions options)
        {
            var parts = new List<CodePart>();
            ParseTree parseTree = parser.Parse(scriptText);
            if (parseTree.Errors.Count == 0)
            {
                var compiler = new Compiler();
                LoadContext(contextId);

                CodePart mainPart;
                try
                {
                    mainPart = compiler.Compile(startLineNum, parseTree, currentContext, options);
                }
                catch (KOSCompileException e)
                {
                    e.AddSourceText((short)startLineNum, scriptText, filePath.ToString());
                    throw;
                }

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
                // TODO: Come back here and check on the possibility of reporting more
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

        private void AssignSourceId(IEnumerable<CodePart> parts, GlobalPath filePath)
        {
            currentContext.LastSourcePath = filePath;
            foreach (CodePart part in parts)
            {
                part.AssignSourceName(currentContext.LastSourcePath);
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
            bool inQuotes = false;
            bool inCommentToEoln = false;
            bool waitForMoreTokens = false;
            char curChar;
            char prevChar = '\0';

            // First, we have to check manually for unterminated string literals because
            // they are a continuation in the midst of a token, rather than between tokens,
            // and thus the parser doesn't quite catch them the same way.
            for (int n = 0; n < length; n++)
            {
                curChar = commandChars[n];
                switch (curChar)
                {
                    // Track if we are in a string literal that didn't close,
                    // and make sure it's not a string literal inside a comment,
                    // because those don't count:
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
                }
                prevChar = curChar;
            }
            
            // Second, if we aren't in an unterminated literal string, then let
            // the parser do the rest of the checking by seeing if it reports
            //    Unexpected Token 'EOF', and looking at what it was expecting instead.

            // - Possible future refactor - 
            // The string comparison of the human-readable message is the only way
            // to find out if the error is the exact one we're looking for, which
            // is what the code below does, and that's a bit fragile.
            // Making it use a more robust check would first require editing the
            // TinyPG C# source code and changing the way it encodes a ParseError
            // so it stores that sort of thing as separate pieces of data in its members.
            
            if (!inQuotes)
            {
                ParseTree parseTree = parser.Parse(command);
                
                foreach (ParseError err in parseTree.Errors)
                {
                    if (err.Message.StartsWith("Unexpected token 'EOF'"))
                    {
                        if (err.Message.Contains("Expected CURLYCLOSE") ||
                            err.Message.Contains("Expected BRACKETCLOSE"))
                        {
                            waitForMoreTokens = true;
                        }
                    }
                    else
                    {
                        // If ANY parse errors are NOT of the form "Unexpected Token 'EOF' ... yadda yadda" then that
                        // automatically means we should fail and not continue regardless of whether or not the other
                        // parse errors may have indicated a continuation is needed.  We want to let the user see
                        // the error happen instead.
                        waitForMoreTokens = false;
                        break;
                    }
                }
            }

            return (!waitForMoreTokens) && (!inQuotes);
        }
    }
}