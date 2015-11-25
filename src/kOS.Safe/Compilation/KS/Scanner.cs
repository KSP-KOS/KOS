// Generated by TinyPG v1.3 available at www.codeproject.com

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace kOS.Safe.Compilation.KS
{
    #region Scanner

    public partial class Scanner
    {
        public string Input;
        public int StartPos = 0;
        public int EndPos = 0;
        public string CurrentFile;
        public int CurrentLine;
        public int CurrentColumn;
        public int CurrentPosition;
        public List<Token> Skipped; // tokens that were skipped
        public Dictionary<TokenType, Regex> Patterns;

        private Token LookAheadToken;
        private List<TokenType> Tokens;
        private List<TokenType> SkipList; // tokens to be skipped
        private readonly TokenType FileAndLine;

        public Scanner()
        {
            Regex regex;
            Patterns = new Dictionary<TokenType, Regex>();
            Tokens = new List<TokenType>();
            LookAheadToken = null;
            Skipped = new List<Token>();

            SkipList = new List<TokenType>();
            SkipList.Add(TokenType.WHITESPACE);
            SkipList.Add(TokenType.COMMENTLINE);

            regex = new Regex(@"(\+|-)");
            Patterns.Add(TokenType.PLUSMINUS, regex);
            Tokens.Add(TokenType.PLUSMINUS);

            regex = new Regex(@"\*");
            Patterns.Add(TokenType.MULT, regex);
            Tokens.Add(TokenType.MULT);

            regex = new Regex(@"/");
            Patterns.Add(TokenType.DIV, regex);
            Tokens.Add(TokenType.DIV);

            regex = new Regex(@"\^");
            Patterns.Add(TokenType.POWER, regex);
            Tokens.Add(TokenType.POWER);

            regex = new Regex(@"(?i)\be\b");
            Patterns.Add(TokenType.E, regex);
            Tokens.Add(TokenType.E);

            regex = new Regex(@"(?i)\bnot\b");
            Patterns.Add(TokenType.NOT, regex);
            Tokens.Add(TokenType.NOT);

            regex = new Regex(@"(?i)\band\b");
            Patterns.Add(TokenType.AND, regex);
            Tokens.Add(TokenType.AND);

            regex = new Regex(@"(?i)\bor\b");
            Patterns.Add(TokenType.OR, regex);
            Tokens.Add(TokenType.OR);

            regex = new Regex(@"(?i)\btrue\b|\bfalse\b");
            Patterns.Add(TokenType.TRUEFALSE, regex);
            Tokens.Add(TokenType.TRUEFALSE);

            regex = new Regex(@"<>|>=|<=|=|>|<");
            Patterns.Add(TokenType.COMPARATOR, regex);
            Tokens.Add(TokenType.COMPARATOR);

            regex = new Regex(@"(?i)\bset\b");
            Patterns.Add(TokenType.SET, regex);
            Tokens.Add(TokenType.SET);

            regex = new Regex(@"(?i)\bto\b");
            Patterns.Add(TokenType.TO, regex);
            Tokens.Add(TokenType.TO);

            regex = new Regex(@"(?i)\bis\b");
            Patterns.Add(TokenType.IS, regex);
            Tokens.Add(TokenType.IS);

            regex = new Regex(@"(?i)\bif\b");
            Patterns.Add(TokenType.IF, regex);
            Tokens.Add(TokenType.IF);

            regex = new Regex(@"(?i)\belse\b");
            Patterns.Add(TokenType.ELSE, regex);
            Tokens.Add(TokenType.ELSE);

            regex = new Regex(@"(?i)\buntil\b");
            Patterns.Add(TokenType.UNTIL, regex);
            Tokens.Add(TokenType.UNTIL);

            regex = new Regex(@"(?i)\bstep\b");
            Patterns.Add(TokenType.STEP, regex);
            Tokens.Add(TokenType.STEP);

            regex = new Regex(@"(?i)\bdo\b");
            Patterns.Add(TokenType.DO, regex);
            Tokens.Add(TokenType.DO);

            regex = new Regex(@"(?i)\block\b");
            Patterns.Add(TokenType.LOCK, regex);
            Tokens.Add(TokenType.LOCK);

            regex = new Regex(@"(?i)\bunlock\b");
            Patterns.Add(TokenType.UNLOCK, regex);
            Tokens.Add(TokenType.UNLOCK);

            regex = new Regex(@"(?i)\bprint\b");
            Patterns.Add(TokenType.PRINT, regex);
            Tokens.Add(TokenType.PRINT);

            regex = new Regex(@"(?i)\bat\b");
            Patterns.Add(TokenType.AT, regex);
            Tokens.Add(TokenType.AT);

            regex = new Regex(@"(?i)\bon\b");
            Patterns.Add(TokenType.ON, regex);
            Tokens.Add(TokenType.ON);

            regex = new Regex(@"(?i)\btoggle\b");
            Patterns.Add(TokenType.TOGGLE, regex);
            Tokens.Add(TokenType.TOGGLE);

            regex = new Regex(@"(?i)\bwait\b");
            Patterns.Add(TokenType.WAIT, regex);
            Tokens.Add(TokenType.WAIT);

            regex = new Regex(@"(?i)\bwhen\b");
            Patterns.Add(TokenType.WHEN, regex);
            Tokens.Add(TokenType.WHEN);

            regex = new Regex(@"(?i)\bthen\b");
            Patterns.Add(TokenType.THEN, regex);
            Tokens.Add(TokenType.THEN);

            regex = new Regex(@"(?i)\boff\b");
            Patterns.Add(TokenType.OFF, regex);
            Tokens.Add(TokenType.OFF);

            regex = new Regex(@"(?i)\bstage\b");
            Patterns.Add(TokenType.STAGE, regex);
            Tokens.Add(TokenType.STAGE);

            regex = new Regex(@"(?i)\bclearscreen\b");
            Patterns.Add(TokenType.CLEARSCREEN, regex);
            Tokens.Add(TokenType.CLEARSCREEN);

            regex = new Regex(@"(?i)\badd\b");
            Patterns.Add(TokenType.ADD, regex);
            Tokens.Add(TokenType.ADD);

            regex = new Regex(@"(?i)\bremove\b");
            Patterns.Add(TokenType.REMOVE, regex);
            Tokens.Add(TokenType.REMOVE);

            regex = new Regex(@"(?i)\blog\b");
            Patterns.Add(TokenType.LOG, regex);
            Tokens.Add(TokenType.LOG);

            regex = new Regex(@"(?i)\bbreak\b");
            Patterns.Add(TokenType.BREAK, regex);
            Tokens.Add(TokenType.BREAK);

            regex = new Regex(@"(?i)\bpreserve\b");
            Patterns.Add(TokenType.PRESERVE, regex);
            Tokens.Add(TokenType.PRESERVE);

            regex = new Regex(@"(?i)\bdeclare\b");
            Patterns.Add(TokenType.DECLARE, regex);
            Tokens.Add(TokenType.DECLARE);

            regex = new Regex(@"(?i)\bdefined\b");
            Patterns.Add(TokenType.DEFINED, regex);
            Tokens.Add(TokenType.DEFINED);

            regex = new Regex(@"(?i)\blocal\b");
            Patterns.Add(TokenType.LOCAL, regex);
            Tokens.Add(TokenType.LOCAL);

            regex = new Regex(@"(?i)\bglobal\b");
            Patterns.Add(TokenType.GLOBAL, regex);
            Tokens.Add(TokenType.GLOBAL);

            regex = new Regex(@"(?i)\bparameter\b");
            Patterns.Add(TokenType.PARAMETER, regex);
            Tokens.Add(TokenType.PARAMETER);

            regex = new Regex(@"(?i)\bfunction\b");
            Patterns.Add(TokenType.FUNCTION, regex);
            Tokens.Add(TokenType.FUNCTION);

            regex = new Regex(@"(?i)\breturn\b");
            Patterns.Add(TokenType.RETURN, regex);
            Tokens.Add(TokenType.RETURN);

            regex = new Regex(@"(?i)\bswitch\b");
            Patterns.Add(TokenType.SWITCH, regex);
            Tokens.Add(TokenType.SWITCH);

            regex = new Regex(@"(?i)\bcopy\b");
            Patterns.Add(TokenType.COPY, regex);
            Tokens.Add(TokenType.COPY);

            regex = new Regex(@"(?i)\bfrom\b");
            Patterns.Add(TokenType.FROM, regex);
            Tokens.Add(TokenType.FROM);

            regex = new Regex(@"(?i)\brename\b");
            Patterns.Add(TokenType.RENAME, regex);
            Tokens.Add(TokenType.RENAME);

            regex = new Regex(@"(?i)\bvolume\b");
            Patterns.Add(TokenType.VOLUME, regex);
            Tokens.Add(TokenType.VOLUME);

            regex = new Regex(@"(?i)\bfile\b");
            Patterns.Add(TokenType.FILE, regex);
            Tokens.Add(TokenType.FILE);

            regex = new Regex(@"(?i)\bdelete\b");
            Patterns.Add(TokenType.DELETE, regex);
            Tokens.Add(TokenType.DELETE);

            regex = new Regex(@"(?i)\bedit\b");
            Patterns.Add(TokenType.EDIT, regex);
            Tokens.Add(TokenType.EDIT);

            regex = new Regex(@"(?i)\brun\b");
            Patterns.Add(TokenType.RUN, regex);
            Tokens.Add(TokenType.RUN);

            regex = new Regex(@"(?i)\bonce\b");
            Patterns.Add(TokenType.ONCE, regex);
            Tokens.Add(TokenType.ONCE);

            regex = new Regex(@"(?i)\bcompile\b");
            Patterns.Add(TokenType.COMPILE, regex);
            Tokens.Add(TokenType.COMPILE);

            regex = new Regex(@"(?i)\blist\b");
            Patterns.Add(TokenType.LIST, regex);
            Tokens.Add(TokenType.LIST);

            regex = new Regex(@"(?i)\breboot\b");
            Patterns.Add(TokenType.REBOOT, regex);
            Tokens.Add(TokenType.REBOOT);

            regex = new Regex(@"(?i)\bshutdown\b");
            Patterns.Add(TokenType.SHUTDOWN, regex);
            Tokens.Add(TokenType.SHUTDOWN);

            regex = new Regex(@"(?i)\bfor\b");
            Patterns.Add(TokenType.FOR, regex);
            Tokens.Add(TokenType.FOR);

            regex = new Regex(@"(?i)\bunset\b");
            Patterns.Add(TokenType.UNSET, regex);
            Tokens.Add(TokenType.UNSET);

            regex = new Regex(@"\(");
            Patterns.Add(TokenType.BRACKETOPEN, regex);
            Tokens.Add(TokenType.BRACKETOPEN);

            regex = new Regex(@"\)");
            Patterns.Add(TokenType.BRACKETCLOSE, regex);
            Tokens.Add(TokenType.BRACKETCLOSE);

            regex = new Regex(@"\{");
            Patterns.Add(TokenType.CURLYOPEN, regex);
            Tokens.Add(TokenType.CURLYOPEN);

            regex = new Regex(@"\}");
            Patterns.Add(TokenType.CURLYCLOSE, regex);
            Tokens.Add(TokenType.CURLYCLOSE);

            regex = new Regex(@"\[");
            Patterns.Add(TokenType.SQUAREOPEN, regex);
            Tokens.Add(TokenType.SQUAREOPEN);

            regex = new Regex(@"\]");
            Patterns.Add(TokenType.SQUARECLOSE, regex);
            Tokens.Add(TokenType.SQUARECLOSE);

            regex = new Regex(@",");
            Patterns.Add(TokenType.COMMA, regex);
            Tokens.Add(TokenType.COMMA);

            regex = new Regex(@":");
            Patterns.Add(TokenType.COLON, regex);
            Tokens.Add(TokenType.COLON);

            regex = new Regex(@"(?i)\bin\b");
            Patterns.Add(TokenType.IN, regex);
            Tokens.Add(TokenType.IN);

            regex = new Regex(@"#");
            Patterns.Add(TokenType.ARRAYINDEX, regex);
            Tokens.Add(TokenType.ARRAYINDEX);

            regex = new Regex(@"(?i)\ball\b");
            Patterns.Add(TokenType.ALL, regex);
            Tokens.Add(TokenType.ALL);

            regex = new Regex(@"(?i)[a-z_][a-z0-9_]*");
            Patterns.Add(TokenType.IDENTIFIER, regex);
            Tokens.Add(TokenType.IDENTIFIER);

            regex = new Regex(@"(?i)[a-z_][a-z0-9_]*(\.[a-z0-9_][a-z0-9_]*)*");
            Patterns.Add(TokenType.FILEIDENT, regex);
            Tokens.Add(TokenType.FILEIDENT);

            regex = new Regex(@"[0-9]+");
            Patterns.Add(TokenType.INTEGER, regex);
            Tokens.Add(TokenType.INTEGER);

            regex = new Regex(@"[0-9]*\.[0-9]+");
            Patterns.Add(TokenType.DOUBLE, regex);
            Tokens.Add(TokenType.DOUBLE);

            regex = new Regex(@"@?\""(\""\""|[^\""])*\""");
            Patterns.Add(TokenType.STRING, regex);
            Tokens.Add(TokenType.STRING);

            regex = new Regex(@"\.");
            Patterns.Add(TokenType.EOI, regex);
            Tokens.Add(TokenType.EOI);

            regex = new Regex(@"@");
            Patterns.Add(TokenType.ATSIGN, regex);
            Tokens.Add(TokenType.ATSIGN);

            regex = new Regex(@"(?i)\blazyglobal\b");
            Patterns.Add(TokenType.LAZYGLOBAL, regex);
            Tokens.Add(TokenType.LAZYGLOBAL);

            regex = new Regex(@"^$");
            Patterns.Add(TokenType.EOF, regex);
            Tokens.Add(TokenType.EOF);

            regex = new Regex(@"\s+");
            Patterns.Add(TokenType.WHITESPACE, regex);
            Tokens.Add(TokenType.WHITESPACE);

            regex = new Regex(@"//[^\n]*\n?");
            Patterns.Add(TokenType.COMMENTLINE, regex);
            Tokens.Add(TokenType.COMMENTLINE);


        }

        public void Init(string input)
        {
            Init(input, "");
        }

        public void Init(string input, string fileName)
        {
            this.Input = input;
            StartPos = 0;
            EndPos = 0;
            CurrentFile = fileName;
            CurrentLine = 1;
            CurrentColumn = 1;
            CurrentPosition = 0;
            LookAheadToken = null;
        }

        public Token GetToken(TokenType type)
        {
            Token t = new Token(this.StartPos, this.EndPos);
            t.Type = type;
            return t;
        }

         /// <summary>
        /// executes a lookahead of the next token
        /// and will advance the scan on the input string
        /// </summary>
        /// <returns></returns>
        public Token Scan(params TokenType[] expectedtokens)
        {
            Token tok = LookAhead(expectedtokens); // temporarely retrieve the lookahead
            LookAheadToken = null; // reset lookahead token, so scanning will continue
            StartPos = tok.EndPos;
            EndPos = tok.EndPos; // set the tokenizer to the new scan position
            CurrentLine = tok.Line + (tok.Text.Length - tok.Text.Replace("\n", "").Length);
            CurrentFile = tok.File;
            return tok;
        }

        /// <summary>
        /// returns token with longest best match
        /// </summary>
        /// <returns></returns>
        public Token LookAhead(params TokenType[] expectedtokens)
        {
            int i;
            int startpos = StartPos;
            int endpos = EndPos;
            int currentline = CurrentLine;
            string currentFile = CurrentFile;
            Token tok = null;
            List<TokenType> scantokens;


            // this prevents double scanning and matching
            // increased performance
            if (LookAheadToken != null 
                && LookAheadToken.Type != TokenType._UNDETERMINED_ 
                && LookAheadToken.Type != TokenType._NONE_) return LookAheadToken;

            // if no scantokens specified, then scan for all of them (= backward compatible)
            if (expectedtokens.Length == 0)
                scantokens = Tokens;
            else
            {
                scantokens = new List<TokenType>(expectedtokens);
                scantokens.AddRange(SkipList);
            }

            do
            {

                int len = -1;
                TokenType index = (TokenType)int.MaxValue;
                string input = Input.Substring(startpos);

                tok = new Token(startpos, endpos);

                for (i = 0; i < scantokens.Count; i++)
                {
                    Regex r = Patterns[scantokens[i]];
                    Match m = r.Match(input);
                    if (m.Success && m.Index == 0 && ((m.Length > len) || (scantokens[i] < index && m.Length == len )))
                    {
                        len = m.Length;
                        index = scantokens[i];  
                    }
                }

                if (index >= 0 && len >= 0)
                {
                    tok.EndPos = startpos + len;
                    tok.Text = Input.Substring(tok.StartPos, len);
                    tok.Type = index;
                }
                else if (tok.StartPos == tok.EndPos)
                {
                    if (tok.StartPos < Input.Length)
                        tok.Text = Input.Substring(tok.StartPos, 1);
                    else
                        tok.Text = "EOF";
                }

                // Update the line and column count for error reporting.
                tok.File = currentFile;
                tok.Line = currentline;
                if (tok.StartPos < Input.Length)
                    tok.Column = tok.StartPos - Input.LastIndexOf('\n', tok.StartPos);

                if (SkipList.Contains(tok.Type))
                {
                    startpos = tok.EndPos;
                    endpos = tok.EndPos;
                    currentline = tok.Line + (tok.Text.Length - tok.Text.Replace("\n", "").Length);
                    currentFile = tok.File;
                    Skipped.Add(tok);
                }
                else
                {
                    // only assign to non-skipped tokens
                    tok.Skipped = Skipped; // assign prior skips to this token
                    Skipped = new List<Token>(); //reset skips
                }

                // Check to see if the parsed token wants to 
                // alter the file and line number.
                if (tok.Type == FileAndLine)
                {
                    var match = Patterns[tok.Type].Match(tok.Text);
                    var fileMatch = match.Groups["File"];
                    if (fileMatch.Success)
                        currentFile = fileMatch.Value;
                    var lineMatch = match.Groups["Line"];
                    if (lineMatch.Success)
                        currentline = int.Parse(lineMatch.Value);
                }
            }
            while (SkipList.Contains(tok.Type));

            LookAheadToken = tok;
            return tok;
        }
    }

    #endregion

    #region Token

    public enum TokenType
    {

            //Non terminal tokens:
            _NONE_  = 0,
            _UNDETERMINED_= 1,

            //Non terminal tokens:
            Start   = 2,
            instruction_block= 3,
            instruction= 4,
            lazyglobal_directive= 5,
            directive= 6,
            set_stmt= 7,
            if_stmt = 8,
            until_stmt= 9,
            fromloop_stmt= 10,
            unlock_stmt= 11,
            print_stmt= 12,
            on_stmt = 13,
            toggle_stmt= 14,
            wait_stmt= 15,
            when_stmt= 16,
            onoff_stmt= 17,
            onoff_trailer= 18,
            stage_stmt= 19,
            clear_stmt= 20,
            add_stmt= 21,
            remove_stmt= 22,
            log_stmt= 23,
            break_stmt= 24,
            preserve_stmt= 25,
            declare_identifier_clause= 26,
            declare_parameter_clause= 27,
            declare_function_clause= 28,
            declare_lock_clause= 29,
            declare_stmt= 30,
            return_stmt= 31,
            switch_stmt= 32,
            copy_stmt= 33,
            rename_stmt= 34,
            delete_stmt= 35,
            edit_stmt= 36,
            run_stmt= 37,
            compile_stmt= 38,
            list_stmt= 39,
            reboot_stmt= 40,
            shutdown_stmt= 41,
            for_stmt= 42,
            unset_stmt= 43,
            arglist = 44,
            expr    = 45,
            and_expr= 46,
            compare_expr= 47,
            arith_expr= 48,
            multdiv_expr= 49,
            unary_expr= 50,
            factor  = 51,
            suffix  = 52,
            suffix_trailer= 53,
            suffixterm= 54,
            suffixterm_trailer= 55,
            function_trailer= 56,
            array_trailer= 57,
            atom    = 58,
            sci_number= 59,
            number  = 60,
            varidentifier= 61,
            identifier_led_stmt= 62,
            identifier_led_expr= 63,

            //Terminal tokens:
            PLUSMINUS= 64,
            MULT    = 65,
            DIV     = 66,
            POWER   = 67,
            E       = 68,
            NOT     = 69,
            AND     = 70,
            OR      = 71,
            TRUEFALSE= 72,
            COMPARATOR= 73,
            SET     = 74,
            TO      = 75,
            IS      = 76,
            IF      = 77,
            ELSE    = 78,
            UNTIL   = 79,
            STEP    = 80,
            DO      = 81,
            LOCK    = 82,
            UNLOCK  = 83,
            PRINT   = 84,
            AT      = 85,
            ON      = 86,
            TOGGLE  = 87,
            WAIT    = 88,
            WHEN    = 89,
            THEN    = 90,
            OFF     = 91,
            STAGE   = 92,
            CLEARSCREEN= 93,
            ADD     = 94,
            REMOVE  = 95,
            LOG     = 96,
            BREAK   = 97,
            PRESERVE= 98,
            DECLARE = 99,
            DEFINED = 100,
            LOCAL   = 101,
            GLOBAL  = 102,
            PARAMETER= 103,
            FUNCTION= 104,
            RETURN  = 105,
            SWITCH  = 106,
            COPY    = 107,
            FROM    = 108,
            RENAME  = 109,
            VOLUME  = 110,
            FILE    = 111,
            DELETE  = 112,
            EDIT    = 113,
            RUN     = 114,
            ONCE    = 115,
            COMPILE = 116,
            LIST    = 117,
            REBOOT  = 118,
            SHUTDOWN= 119,
            FOR     = 120,
            UNSET   = 121,
            BRACKETOPEN= 122,
            BRACKETCLOSE= 123,
            CURLYOPEN= 124,
            CURLYCLOSE= 125,
            SQUAREOPEN= 126,
            SQUARECLOSE= 127,
            COMMA   = 128,
            COLON   = 129,
            IN      = 130,
            ARRAYINDEX= 131,
            ALL     = 132,
            IDENTIFIER= 133,
            FILEIDENT= 134,
            INTEGER = 135,
            DOUBLE  = 136,
            STRING  = 137,
            EOI     = 138,
            ATSIGN  = 139,
            LAZYGLOBAL= 140,
            EOF     = 141,
            WHITESPACE= 142,
            COMMENTLINE= 143
    }

    public class Token
    {
        private string file;
        private int line;
        private int column;
        private int startpos;
        private int endpos;
        private string text;
        private object value;

        // contains all prior skipped symbols
        private List<Token> skipped;

        public string File { 
            get { return file; } 
            set { file = value; }
        }

        public int Line { 
            get { return line; } 
            set { line = value; }
        }

        public int Column {
            get { return column; } 
            set { column = value; }
        }

        public int StartPos { 
            get { return startpos;} 
            set { startpos = value; }
        }

        public int Length { 
            get { return endpos - startpos;} 
        }

        public int EndPos { 
            get { return endpos;} 
            set { endpos = value; }
        }

        public string Text { 
            get { return text;} 
            set { text = value; }
        }

        public List<Token> Skipped { 
            get { return skipped;} 
            set { skipped = value; }
        }
        public object Value { 
            get { return value;} 
            set { this.value = value; }
        }

        [XmlAttribute]
        public TokenType Type;

        public Token()
            : this(0, 0)
        {
        }

        public Token(int start, int end)
        {
            Type = TokenType._UNDETERMINED_;
            startpos = start;
            endpos = end;
            Text = ""; // must initialize with empty string, may cause null reference exceptions otherwise
            Value = null;
        }

        public void UpdateRange(Token token)
        {
            if (token.StartPos < startpos) startpos = token.StartPos;
            if (token.EndPos > endpos) endpos = token.EndPos;
        }

        public override string ToString()
        {
            if (Text != null)
                return Type.ToString() + " '" + Text + "'";
            else
                return Type.ToString();
        }
    }

    #endregion
}