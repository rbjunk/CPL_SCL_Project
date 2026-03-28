using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPL_SCL_Project
{
    public class Parser
    {
        private List<Token> _tokens;
        private int _currentIndex;
        private Token _currentToken;
        private HashSet<string> _declaredIdentifiers;
        private Tree_Node _parse_tree;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _currentIndex = -1;
            _declaredIdentifiers = new HashSet<string>();
            getNextToken();
        }
        // Increment the current index for the token list and grab the token at said index.
        public Token getNextToken()
        {
            _currentIndex++;
            if (_currentIndex < _tokens.Count)
            {
                _currentToken = _tokens[_currentIndex];
            }
            else
            {
                _currentToken = null;
            }
            return _currentToken;
        }
        //look at the next token without incrementng.
        public Token peekToken()
        {
            Token temp;
            if (_currentIndex + 1 < _tokens.Count)
            {
                temp = _tokens[_currentIndex + 1];
            }
            else
            {
                temp = null;
            }
            return temp;
        }

        //Checks if an identifier has already been declared
        public bool identifierExists(string identifier)
        {
            return _declaredIdentifiers.Contains(identifier);
        }
        public void begin()
        {
            Console.WriteLine("      PARSER INITIATED       ");
            try
            {
                start();
                Console.WriteLine("\nSTATUS: Parsing completed. Check above for any semantic warnings.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nSYNTAX ERROR: {ex.Message}");
            }
        }

        private void expect(token_types expected_type, string expected_value = null)
        {
            Token currentToken = peekToken();

            bool typeMatches = currentToken.token_type == expected_type;

            bool valueMatches = expected_value == null || currentToken.value == expected_value;

            if (!typeMatches || !valueMatches)
            {
                string expectedMsg = expected_value != null
                    ? $"{expected_type} '{expected_value}'"
                    : expected_type.ToString();

                throw new Exception(
                    $"Syntax Error: Expected {expectedMsg}, " +
                    $"but found {currentToken.token_type} '{currentToken.value}' at line {"LINEPLACEHOLDER"}."
                );
            }
        }

        private Tree_Node start()
        {
            _parse_tree = new Tree_Node("start", null);
            Tree_Node statementList = new Tree_Node("StatementList", null);
            _parse_tree.addChild(statementList);
            //look for imports at the top of the file first.
            while (matchKeyword("import"))
            {
                importStatement(statementList);
            }
            while (_currentToken != null)
            {
                parseStatement(statementList);
            }
            return _parse_tree;
        }
        private void importStatement(Tree_Node parent)
        {
            consumeKeyword("import"); //consume import statement
            Tree_Node importStatement = new Tree_Node("importStatement", null);
            Token temp = consume(token_types.literal, "Expected string literal after import"); //consume importee
            parent.addChild(importStatement);
            Tree_Node importee = new Tree_Node("literal", temp.value);
            importStatement.addChild(importee);
            consumeEOS();
            Console.WriteLine("Parsed: Import Statement, Matched Importee Value: " + importee.getValue());
        }
        private void parseStatement(Tree_Node parent)
        {
            // Check if the statement starts with the "define" keyword
            if (matchKeyword("define"))
            {
                defineStatement(parent);
            }

            else
            {
                throw new Exception($"Unexpected token '{_currentToken.value}' at start of statement.");
            }
        }
        private void defineStatement(Tree_Node parent)
        {
            consumeKeyword("define"); //consume the "define" keyword

            //create the tree node for this statement and attach it to the parent
            Tree_Node defineNode = new Tree_Node("defineStatement", null);
            parent.addChild(defineNode);

            //expect an identifier next (the name of the variable being defined) and add it to the tree
            Token identifierToken = consume(token_types.identifier, "Expected identifier after 'define'");
            defineNode.addChild(new Tree_Node("identifier", identifierToken.value));

            Tree_Node typeNode = null;
            if (matchKeyword("of"))
            {
                consumeKeyword("of");
                typeNode = typeStatement(defineNode);
            }
            consumeEOS();

            //only add if parsing succeeded
            if (!_declaredIdentifiers.Add(identifierToken.value))
            {
                Console.WriteLine($"Error: Identifier '{identifierToken.value}' is already defined.");
            }

            //safe string handling for the console
            string typeName = typeNode?.getValue() ?? "unspecified";
            Console.WriteLine($"Parsed: {identifierToken.value} (Type: {typeName})");
        }
        private Tree_Node typeStatement(Tree_Node parent)
        {
            // 1. Consume the 'type' keyword
            consumeKeyword("type");

            // 2. Check if the current token is one of the allowed types
            if (matchKeyword("integer") || matchKeyword("double") || matchKeyword("char"))
            {
                // Capture the name of the type before consuming it
                string typeName = _currentToken.value;

                // 3. Consume the keyword token to advance the parser
                Token typeToken = consume(token_types.keyword, "Expected a valid type (integer, double, char)");

                // 4. Create the node, attach to parent, and return
                Tree_Node typeNode = new Tree_Node(typeName, typeToken.value);
                parent.addChild(typeNode);

                return typeNode;
            }
            else
            {
                // 5. Explicit Error Handling
                // If the token isn't a valid type, we should halt parsing or flag a clear error.
                // Replace this with however your specific parser handles syntax exceptions.
                string badToken = _currentToken != null ? _currentToken.value : "EOF";
                throw new Exception($"Expected 'integer', 'double', or 'char' after 'type', but found '{badToken}'.");
            }
        }
        //helper functions
        private bool matchKeyword(string keyword)
        {
            return _currentToken != null && _currentToken.token_type == token_types.keyword && _currentToken.value == keyword;
        }

        private void consumeKeyword(string keyword)
        {
            if (matchKeyword(keyword)) { getNextToken(); }
            else { throw new Exception($"Expected keyword '{keyword}' but found '{_currentToken?.value}'"); }
        }

        private Token consume(token_types type, string errorMsg)
        {
            if (_currentToken != null && _currentToken.token_type == type)
            {
                Token temp = _currentToken;
                getNextToken();
                return temp;
            }
            throw new Exception(errorMsg + $" (Found: '{_currentToken?.value}')");
        }

        private void consumeOperator(string op)
        {
            if (_currentToken != null && _currentToken.token_type == token_types.operators && _currentToken.value == op)
            {
                getNextToken();
            }
            else { throw new Exception($"Expected operator '{op}' but found '{_currentToken?.value}'"); }
        }

        private void consumeEOS()
        {
            bool foundEos = false;
            while (_currentToken != null && _currentToken.token_type == token_types.endOfStatement)
            {
                foundEos = true;
                getNextToken();
            }
            if (!foundEos) { throw new Exception("Expected End Of Statement (Newline)."); }
        }
    }
    public class Tree_Node
    {
        public string label;
        public List<Tree_Node> children;
        public string token_value;
        public Tree_Node(string label, string token_value)
        {
            this.label = label;
            children = new List<Tree_Node>();
            this.token_value = token_value;
        }

        public void addChild(Tree_Node child)
            {
                children.Add(child);
            }
        public string getValue()
        {
            return token_value;
        }
    }
}

