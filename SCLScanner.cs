using System.Text.Json;
using System.Text.RegularExpressions;
namespace CPL_SCL_Project
{
    internal class SCLScanner
    {
        static void Main(string[] args)
        {
            string file_location = args[0];
            Console.WriteLine("TEST");
            tokenize(file_location);
        }
        public static List<Token> tokenize(string file_location)
        {

            List<Token> all_tokens = new List<Token>();
            int current_identifier_id = 1000;
            Dictionary<string, int> current_identifiers = new Dictionary<string, int>();
            try
            {
                using (StreamReader reader = new StreamReader(file_location))
                {
                    List<TokenDefinition> token_regex_patterns = new List<TokenDefinition>
                    {
                        new TokenDefinition(token_types.comment, @"//.*"),
                        new TokenDefinition(token_types.comment, @"/\*.*?\*/"),
                        new TokenDefinition(token_types.commentStart, @"/\*.*"),
                        new TokenDefinition(token_types.literal, @"""[^""]*"""), //string literal
                        new TokenDefinition(token_types.operators, @"[+\-*/^><=()]"), //operator check
                        new TokenDefinition(token_types.keyword, @"\b(import|implementations|function|main|return|type|integer|double|char|num|is|variables|define|of|begin|display|set|exit|endfun|symbol|end|input|structures|pointer|head|last|NULL|ChNode|using|reverse|while|endwhile|call|constants|float|array|for|to|do|endfor)\b"), //keyword check
                        new TokenDefinition(token_types.literal, @"\d+\.\d+"), //double check
                        new TokenDefinition(token_types.literal, @"\d+"), //integer check
                        new TokenDefinition(token_types.specialSymbols, @"[,\.]"), //special character check
                        new TokenDefinition(token_types.identifier, @"[a-zA-Z_][a-zA-Z0-9_]*") //identifier check
                    };
                    string remaining_line_text;
                    bool in_comment_block = false;
                    while (!reader.EndOfStream)
                    {
                        remaining_line_text = reader.ReadLine();
                        remaining_line_text = remaining_line_text.Trim();
                        if (in_comment_block )
                        {
                            // Check if this line closes the block
                            int closeIndex = remaining_line_text.IndexOf("*/");

                            if (closeIndex != -1)
                            {
                                // Found the closing "*/"
                                // Resume tokenizing AFTER the "*/" (add 2 for the length of */)
                                remaining_line_text = remaining_line_text.Substring(closeIndex + 2).TrimStart();
                                in_comment_block = false;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    
                        while (!string.IsNullOrEmpty(remaining_line_text))
                        {
                            bool match_found = false;
                            foreach (var pattern in token_regex_patterns)
                            {
                                var match = pattern.regex.Match(remaining_line_text);
                                if(match.Success)
                                {
                                    if (pattern.type == token_types.comment)
                                    {
                                    }
                                    else if (pattern.type == token_types.commentStart)
                                    {
                                        in_comment_block = true;
                                        remaining_line_text = "";
                                    }
                                    else if (pattern.type == token_types.keyword)
                                    {
                                        if (TokenTypes.TokenList.TryGetValue(pattern.type, out var dict) &&
                                            dict.TryGetValue(match.Value, out int code))
                                        {
                                            Console.WriteLine($"Token created: {pattern.type} id:{code} value:{match.Value}");
                                        }
                                    }
                                    else if (pattern.type == token_types.operators)
                                    {
                                        if (TokenTypes.TokenList.TryGetValue(pattern.type, out var dict) &&
                                            dict.TryGetValue(match.Value, out int code))
                                        {
                                            Console.WriteLine($"Token created: {pattern.type} id:{code} value:{match.Value}");
                                        }
                                    }
                                    else if (pattern.type == token_types.identifier)
                                    {
                                        if (current_identifiers.ContainsKey(match.Value))
                                        {
                                            int identifier_value = current_identifiers[match.Value];
                                            Console.WriteLine($"Token created: {pattern.type} id:{identifier_value} value:{match.Value}");
                                        }
                                        else
                                        {
                                            int identifier_value = current_identifier_id;
                                            Console.WriteLine($"Token created: {pattern.type} id:{identifier_value} value:{match.Value}");
                                            current_identifiers.Add(match.Value, identifier_value);
                                            current_identifier_id++;
                                        }
                                    }
                                    else if (pattern.type == token_types.specialSymbols)
                                    {
                                        if (TokenTypes.TokenList.TryGetValue(pattern.type, out var dict) &&
                                            dict.TryGetValue(match.Value, out int code))
                                        {
                                            Console.WriteLine($"Token created: {pattern.type} id:{code} value:{match.Value}");
                                        }
                                    }
                                    else if (pattern.type == token_types.literal)
                                    {

                                            Console.WriteLine($"Token created: {pattern.type} id:{600} value:{match.Value}");
                                    }

                                    if (!in_comment_block)
                                    {
                                        remaining_line_text = remaining_line_text.Substring(match.Length).TrimStart();
                                    }
                                    match_found = true;
                                    break;
                                }
                            }
                            if (!match_found)
                            {
                                Console.WriteLine(token_types.unknown + " " + remaining_line_text[0].ToString());
                                remaining_line_text = remaining_line_text.Substring(1).TrimStart();
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.Message);
            }
            return all_tokens;
        }
    }
}
