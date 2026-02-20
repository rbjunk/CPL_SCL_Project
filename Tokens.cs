using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CPL_SCL_Project
{
    public enum token_types
    {
        comment,
        commentStart,
        literal,
        keyword,
        operators,
        specialSymbols,
        identifier,
        endOfStatement,
        unknown
    }

    public class TokenDefinition
    {
        public token_types type { get; set; }
        public Regex regex { get; set; }
        
        public TokenDefinition(token_types type, string pattern)
        {
            this.type = type;
            regex = new Regex("^" + pattern);
        }
    }
    class TokenTypes //nested dictionary that includes all possible keywords, and other supported characters
    {
        public static Dictionary<token_types, Dictionary<string, int>> TokenList = new Dictionary<token_types, Dictionary<string, int>>()
        {
            {
                token_types.keyword, new Dictionary<string, int>()
                {
                    { "import", 0 },
                    { "implementations", 1 },
                    { "function", 2 },
                    { "main", 3 },
                    { "return", 4 },
                    { "type", 5 },
                    { "integer", 6 },
                    { "double", 7 },
                    { "char", 8 },
                    { "num", 9 },
                    { "is", 10 },
                    { "variables", 11 },
                    { "define", 12 },
                    { "of", 13 },
                    { "begin", 14 },
                    { "display", 15 },
                    { "set", 16 },
                    { "exit", 17 },
                    { "endfun", 18 },
                    { "symbol", 19 },
                    { "end", 20 },
                    { "input", 21 },
                    { "structures", 22 },
                    { "pointer", 23 },
                    { "head", 24 },
                    { "last", 25 },
                    { "NULL", 26 },
                    { "ChNode", 27 },
                    { "using", 28 },
                    { "reverse", 29 },
                    { "while", 30 },
                    { "endwhile", 31 },
                    { "call", 32 },
                    { "constants", 33 },
                    { "float", 34 },
                    { "array", 35 },
                    { "for", 36 },
                    { "to", 37 },
                    { "do", 38 },
                    { "endfor", 39 },
                }
            },

            {
                token_types.operators, new Dictionary<string, int>()
                {
                    { "+", 400 },
                    { "-", 401 },
                    { "*", 402 },
                    { "/", 403 },
                    { "^", 404 },
                    { ">", 405 },
                    { "<", 406 },
                    { "=", 407 },
                    { "(", 408 },
                    { ")", 409 },
                }
            },

            { 
                token_types.literal, new Dictionary<string, int>()
                {
                    { "literal", 500 }
                }

            },

            {
                token_types.endOfStatement, new Dictionary<string, int>()
                {
                    { "endOfStatement", 600 }
                }

            },

            {
                token_types.specialSymbols, new Dictionary<string, int>()
                {
                    { ",", 800 },
                    { ".", 801 }
                }
            }
        };
    }

    class Token
    {
        public token_types token_type { get; set; }
        public int id { get; set; }
        public string value { get; set; }

        public Token(token_types type, int id, string value)
        {
            token_type = type;
            this.id = id;
            this.value = value;
        }
    }
}
