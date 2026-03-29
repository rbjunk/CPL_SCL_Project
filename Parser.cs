using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPL_SCL_Project
{
    public class Parser
    {
        private List<Token> _tokens; //list for the tokens that come from scanner
        private int _currentIndex;
        private Token _currentToken;
        private HashSet<string> _declaredIdentifiers; //hash set for currently declared variables.
        private Tree_Node _parse_tree; //node that holds all children nodes for the entire program.

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
            Console.WriteLine("===PARSER INITIATED===");
            try
            {
                start(); //begin parsing the tokens.
                Console.WriteLine("\nSTATUS: Parsing completed. Check above for any semantic warnings.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nSYNTAX ERROR: {ex.Message}");
            }
        }

        private Tree_Node start()
        {
            _parse_tree = new Tree_Node("start", null);
            Tree_Node statementList = new Tree_Node("StatementList", null); //the root node of the tree
            _parse_tree.addChild(statementList);
            //look for all imports at the top of the file first.
            while (matchKeyword("import"))
            {
                importStatement(statementList);
            }
            while (_currentToken != null) //loop to the end of the program.
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
            Console.WriteLine("Parsed: Import Statement, Matched Importee Value: " + importee.token_value);
        }
        private void parseStatement(Tree_Node parent)
        {
            // Check if the statement starts with the "define" keyword
            if (matchKeyword("define"))
            {
                defineStatement(parent); //send the parent node so child nodes can be linked back.
            }
            // Check if the statement starts with the "set" keyword
            else if (matchKeyword("set"))
            {
                setStatement(parent); //send the parent node so child nodes can be linked back.
            }
            // Check if the statement starts with the "function" keyword
            else if (matchKeyword("function"))
            {
                functionStatement(parent); //send the parent node so child nodes can be linked back.
            }
            // Check if the statement starts with the "return" keyword
            else if (matchKeyword("return"))
            {
                returnStatement(parent);
            }
            // Check if the statement starts with the "display" keyword
            else if (matchKeyword("display"))
            {
                displayStatement(parent);
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
            string typeName = typeNode?.token_value ?? "unspecified";
            Console.WriteLine($"Parsed: {identifierToken.value} (Type: {typeName})");
        }
        private Tree_Node typeStatement(Tree_Node parent)
        {
            //Consume the 'type' keyword
            consumeKeyword("type");

            //Check if the current token is one of the allowed types
            if (matchKeyword("integer") || matchKeyword("double") || matchKeyword("char"))
            {
                // Capture the name of the type before consuming it
                string typeName = _currentToken.value;

                // Consume the keyword token to advance the parser
                Token typeToken = consume(token_types.keyword, "Expected a valid type (integer, double, char)");

                // Create the node, attach to parent, and return
                Tree_Node typeNode = new Tree_Node(typeName, typeToken.value);
                parent.addChild(typeNode);

                return typeNode;
            }
            else
            {
                // Explicit Error Handling
                // If the token isn't a valid type, halt parsing or flag a clear error.
                string badToken = _currentToken != null ? _currentToken.value : "EOF";
                throw new Exception($"Expected 'integer', 'double', or 'char' after 'type', but found '{badToken}'.");
            }
        }
        private Tree_Node setStatement(Tree_Node parent)
        {
            //Consume the 'set' keyword
            consumeKeyword("set");

            //create the tree node for this statement and attach it to the parent
            Tree_Node setNode = new Tree_Node("setStatement", null);
            parent.addChild(setNode);

            //expect an identifier next (the name of the variable being changed) and add it to the tree
            Token identifierToken = consume(token_types.identifier, "Expected identifier after 'set'");
            setNode.addChild(new Tree_Node("identifier", identifierToken.value));
            //expect an equals operator next and add it to the tree
            consumeOperator("=");
            setNode.addChild(new Tree_Node("operator", "="));

            Tree_Node expressionNode = new Tree_Node("expressionStatement", null);
            setNode.addChild(expressionNode);
            expressionNode.addChild(expression(expressionNode));

            consumeEOS();
            return setNode;
        }

        private void functionStatement(Tree_Node parent)
        {
            // Consume the starting 'function' keyword
            consumeKeyword("function");

            // Create the main node for the AST
            Tree_Node funcNode = new Tree_Node("functionStatement", null);
            parent.addChild(funcNode);

            // Consume the function identifier (the name of the function)
            Token idToken = consume(token_types.identifier, "Expected function identifier after 'function'");
            funcNode.addChild(new Tree_Node("identifier", idToken.value));

            if (!_declaredIdentifiers.Add(idToken.value))
            {
                throw new Exception($"The name '{idToken.value}' is already in use by a variable or another function.");
            }

            funcNode.addChild(new Tree_Node("identifier", idToken.value));
            // Consume "return" and the type statement
            consumeKeyword("return");

            //existing typeStatement() already consumes the "type" keyword and the type.
            typeStatement(funcNode);

            //Consume "is" and the end of the line
            consumeKeyword("is");
            consumeEOS();

            //Check for the optional "variables" section
            if (matchKeyword("variables"))
            {
                consumeKeyword("variables");
                consumeEOS();

                Tree_Node varsNode = new Tree_Node("variablesBlock", null);
                funcNode.addChild(varsNode);

                // Parse variable statements until we hit the 'begin' keyword
                while (_currentToken != null && !matchKeyword("begin"))
                {
                    parseStatement(varsNode);
                }
            }

            // Consume "begin" to start the function body
            consumeKeyword("begin");
            consumeEOS();

            Tree_Node bodyNode = new Tree_Node("bodyBlock", null);
            funcNode.addChild(bodyNode);

            // Parse function body statements until we hit "endfun"
            while (_currentToken != null && !matchKeyword("endfun"))
            {
                parseStatement(bodyNode);
            }

            // Consume "endfun" and validate the closing identifier
            consumeKeyword("endfun");
            Token endIdToken = consume(token_types.identifier, "Expected function identifier after 'endfun'");

            // Semantic Check: Make sure the closing identifier matches the opening one
            if (endIdToken.value != idToken.value)
            {
                throw new Exception($"'endfun' identifier '{endIdToken.value}' does not match the function name '{idToken.value}'.");
            }

            funcNode.addChild(new Tree_Node("endfun", endIdToken.value));

            // Consume the final end of statement (newline)
            consumeEOS();

            Console.WriteLine($"Parsed: Function '{idToken.value}' successfully completed.");
        }
        private void returnStatement(Tree_Node parent)
        {
            // Consume the 'return' keyword
            consumeKeyword("return");

            // Create the tree node for this statement and attach it to the parent
            Tree_Node returnNode = new Tree_Node("returnStatement", null);
            parent.addChild(returnNode);

            // Parse the expression that follows the return keyword. 
            // The expression method automatically handles single identifiers, literals, and math.
            Tree_Node exprNode = expression(returnNode);
            returnNode.addChild(exprNode);

            // Consume the end of statement (newline)
            consumeEOS();

            Console.WriteLine("Parsed: Return Statement");
        }

        private void displayStatement(Tree_Node parent)
        {
            // Consume the 'display' keyword
            consumeKeyword("display");

            // Create the tree node for this statement and attach it to the parent
            Tree_Node displayNode = new Tree_Node("displayStatement", null);
            parent.addChild(displayNode);

            // Parse the first argument. Leveraging expression() natively handles 
            // identifiers, literals, and even mathematical expressions.
            displayNode.addChild(expression(displayNode));

            // Loop to handle infinite chaining separated by commas
            while (checkSpecial(","))
            {
                consumeSpecial(","); // Consume the comma separator

                // Parse the next chained argument and attach it to the display node
                displayNode.addChild(expression(displayNode));
            }

            // Consume the end of statement (newline)
            consumeEOS();

            Console.WriteLine("Parsed: Display Statement");
        }
        // --- EXPRESSION PARSING METHODS ---

        private Tree_Node expression(Tree_Node parent)
        {
            return relational(parent);
        }

        private Tree_Node relational(Tree_Node parent)
        {
            Tree_Node leftNode = additive(parent);

            while (checkOperator(">") || checkOperator("<") || checkOperator("="))
            {
                Token opToken = consumeCurrentToken();
                Tree_Node opNode = new Tree_Node("binaryOp", opToken.value);

                opNode.addChild(leftNode);
                opNode.addChild(additive(opNode));

                leftNode = opNode;
            }

            return leftNode;
        }

        private Tree_Node additive(Tree_Node parent)
        {
            Tree_Node leftNode = multiplicative(parent);

            while (checkOperator("+") || checkOperator("-"))
            {
                Token opToken = consumeCurrentToken();
                Tree_Node opNode = new Tree_Node("binaryOp", opToken.value);

                opNode.addChild(leftNode);
                opNode.addChild(multiplicative(opNode));

                leftNode = opNode;
            }

            return leftNode;
        }

        private Tree_Node multiplicative(Tree_Node parent)
        {
            Tree_Node leftNode = power(parent);

            while (checkOperator("*") || checkOperator("/"))
            {
                Token opToken = consumeCurrentToken();
                Tree_Node opNode = new Tree_Node("binaryOp", opToken.value);

                opNode.addChild(leftNode);
                opNode.addChild(power(opNode));

                leftNode = opNode;
            }

            return leftNode;
        }

        private Tree_Node power(Tree_Node parent)
        {
            Tree_Node leftNode = primary(parent);

            while (checkOperator("^"))
            {
                Token opToken = consumeCurrentToken();
                Tree_Node opNode = new Tree_Node("binaryOp", opToken.value);

                opNode.addChild(leftNode);
                opNode.addChild(primary(opNode));

                leftNode = opNode;
            }

            return leftNode;
        }

        private Tree_Node primary(Tree_Node parent)
        {
            if (_currentToken == null)
            {
                throw new Exception("Unexpected end of file inside expression.");
            }

            // Handle Grouping (Parentheses)
            if (checkOperator("("))
            {
                consumeOperator("(");
                Tree_Node exprNode = expression(parent);
                consumeOperator(")");
                return exprNode;
            }

            // Handle Literals
            if (_currentToken.token_type == token_types.literal ||
                _currentToken.token_type.ToString().Equals("INTEGER", StringComparison.OrdinalIgnoreCase) ||
                _currentToken.token_type.ToString().Equals("DOUBLE", StringComparison.OrdinalIgnoreCase) ||
                _currentToken.token_type.ToString().Equals("CHAR", StringComparison.OrdinalIgnoreCase))
            {
                Token literalToken = consumeCurrentToken();
                return new Tree_Node("literal", literalToken.value);
            }

            // Handle Identifiers (Variables or Function Calls)
            if (_currentToken.token_type == token_types.identifier)
            {
                Token idToken = consume(token_types.identifier, "Expected identifier");

                // If a '(' immediately follows an identifier, it's a function call
                if (checkOperator("("))
                {
                    Tree_Node funcNode = new Tree_Node("functionCall", idToken.value);
                    consumeOperator("(");

                    // Parse arguments if the parentheses aren't immediately closed
                    if (!checkOperator(")"))
                    {
                        funcNode.addChild(expression(funcNode));
                        // Loop here checking for commas if you support multiple arguments later
                    }

                    consumeOperator(")");
                    return funcNode;
                }

                // Otherwise, it's just a variable
                return new Tree_Node("variable", idToken.value);
            }

            throw new Exception($"Unexpected token in expression: '{_currentToken.value}'");
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
        //Grabs the current token and advances the index
        private Token consumeCurrentToken()
        {
            Token temp = _currentToken;
            getNextToken();
            return temp;
        }
        //Checks the current operator without consuming it
        private bool checkOperator(string op)
        {
            return _currentToken != null && _currentToken.token_type == token_types.operators && _currentToken.value == op;
        }
        //Checks the current special symbol without consuming it.
        private bool checkSpecial(string op)
        {
            return _currentToken != null && _currentToken.token_type == token_types.specialSymbols && _currentToken.value == op;
        }
        private void consumeOperator(string op)
        {
            if (_currentToken != null && _currentToken.token_type == token_types.operators && _currentToken.value == op)
            {
                getNextToken();
            }
            else { throw new Exception($"Expected operator '{op}' but found '{_currentToken?.value}'"); }
        }
        private void consumeSpecial(string op)
        {
            if (_currentToken != null && _currentToken.token_type == token_types.specialSymbols && _currentToken.value == op)
            {
                getNextToken();
            }
            else { throw new Exception($"Expected special symbol '{op}' but found '{_currentToken?.value}'"); }
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
        public string label {get; set;}
        public List<Tree_Node> children = new List<Tree_Node>();
        public string token_value {get; set;}
        public Tree_Node(string label, string token_value)
        {
            this.label = label;
            this.token_value = token_value;
        }

        public void addChild(Tree_Node child)
            {
                children.Add(child);
            }
    }
}

