#
# File used for syntax-highlighting code sections in the documentation.
#

import re

from pygments.lexer import RegexLexer, include, bygroups, using, \
    this, inherit, default, words
from pygments.util import get_bool_opt
from pygments.token import Text, Comment, Operator, Keyword, Name, String, \
    Number, Punctuation, Error

class KerboscriptLexer(RegexLexer):
   
    name = 'Kerboscript'
    aliases = ['kerboscript']
    filenames = ['*.ks']
    # mimetypes = ['text/somethinghere'] # We don't have a kerboscript mime type (yet?)
 
    flags = re.MULTILINE | re.DOTALL | re.IGNORECASE

    __all__ = ['KerboscriptLexer']

    tokens = {
        #
        # See http://pygments.org/docs/tokens/ for a list of parts of speech 
        # to assign things to in this list
        #
        'root': [
            #
            # Note: Precedence in a tie is to pick the one that
            # came earlier in this list.
            #
            # Warning: In my experimentation I found that if a rule is
            # present in the list below where a string of zero length 
            # matches the regex, this causes Pygment to just get stuck
            # in an infinite loop.
            #     For example, if the whitespace regex was:
            #         [\t\s\r\n]*
            #     Instead of :
            #         [\t\s\r\n]+
            #     Then zero chars would be a valid match.  So
            #     Pygment matches the rule without advancing
            #     any further into the input, and just gets
            #     stuck doing that forever.
            #
            (r'//[^\r\n]*[\r\n]', Comment.Single),
            (r'/\*[^*]*\*+(?:[^/*][^*]*\*+)*/', Comment.MULTILINE),
            (r'"[^"]*"', String),
            (r'[\t\s\r\n]+', Text), #whitespace
            (r'[*/+|?<>=#^\-]', Operator),
            (r'\b(to|is|not|and|or|all)\b', Operator.Word),
            (r'[()\[\]\.,:\{\}@]', Punctuation),
            (words(( 'set', 'if', 'else', 'until', 'step', 'do',
                'lock', 'unlock', 'print', 'at', 'toggle', 'wait',
                'when', 'then', 'stage', 'clearscreen', 'add', 'remove',
                'log', 'break', 'preserve', 'declare', 'defined', 'local',
                'global', 'return', 'switch', 'copy', 'from', 'rename',
                'volume', 'file', 'delete', 'edit', 'run', 'once', 'compile',
                'list', 'reboot', 'shutdown', 'for', 'unset'), suffix=r'\b'), Keyword),
            (r'\b(declare|local|global|parameter|function)\b', Keyword.Declaration),
            (r'\b(true|false|on|off)\b', Name.Builtin),
            (r'\b[a-z_][a-z_\d]*\b', Name.Variable), # TODO - we could differentiate type of name: i.e. built-in vs user.
            (r'\b(\d+\.\d*|\.\d+|\d+)[eE][+-]?\d+\b', Number.Float),
            (r'\b(\d+)+\b', Number.Float), # markup ints just like floats
        ]
    }

def setup(app):
    app.add_lexer("kerboscript", KerboscriptLexer())
