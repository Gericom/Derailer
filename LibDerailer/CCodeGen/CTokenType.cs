using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CCodeGen
{
    public enum CTokenType
    {
        Whitespace,

        Operator,
        Literal,

        Type,
        Identifier,
        Keyword,

        // Punctuation {
        OpenBrace,
        CloseBrace,

        OpenParen,
        CloseParen,

        OpenBracket,
        CloseBracket,
        
        Comma,
        Semicolon,
        Colon,
        // }

        Cast
    }
}
