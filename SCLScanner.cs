using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
namespace CPL_SCL_Project
{
    internal class SCLScanner
    {
        static void Main(string[] args)
        {
            string file_location = args[0];
            List<Token> token_list = tokenize(file_location);
            string jsonstring = JsonSerializer.Serialize(token_list, new JsonSerializerOptions {WriteIndented = true, Converters = {new JsonStringEnumConverter() }, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            File.WriteAllText(file_location + "_tokenized.json", jsonstring);
            Console.WriteLine("Successfully saved tokens to " + file_location + "_tokenized.json");
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
                        if (string.IsNullOrEmpty(remaining_line_text)) continue;
                        if (in_comment_block)
                        {
                            // Check if this line closes the block
                            int closeIndex = remaining_line_text.IndexOf("*/");
                            if (closeIndex != -1)
                            {
                                // Found the closing "*/"
                                // Resume tokenizing AFTER the "*/" (add 2 for the length of */)
                                remaining_line_text = remaining_line_text.Substring(closeIndex + 2).TrimStart();
                                in_comment_block = false;
                                // If the line is empty after the comment ends, don't try to tokenize
                                if (string.IsNullOrEmpty(remaining_line_text)) continue;
                            }
                            else continue;
                        }
                        // Flag to see if we actually generated real tokens on this line
                        bool lineHadTokens = false;

                        while (!string.IsNullOrEmpty(remaining_line_text))
                        {
                            bool match_found = false;
                            foreach (var pattern in token_regex_patterns)
                            {
                                var match = pattern.regex.Match(remaining_line_text);
                                if(match.Success)
                                {
                                    if (pattern.type != token_types.comment && pattern.type != token_types.commentStart)
                                    {
                                        lineHadTokens = true;
                                    }
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
                                            all_tokens.Add(new Token(pattern.type, code, match.Value));
                                            Console.WriteLine($"Token created: {pattern.type} id:{code} value:{match.Value}");
                                        }
                                    }
                                    else if (pattern.type == token_types.operators)
                                    {
                                        if (TokenTypes.TokenList.TryGetValue(pattern.type, out var dict) &&
                                            dict.TryGetValue(match.Value, out int code))
                                        {
                                            all_tokens.Add(new Token(pattern.type, code, match.Value));
                                            Console.WriteLine($"Token created: {pattern.type} id:{code} value:{match.Value}");
                                        }
                                    }
                                    else if (pattern.type == token_types.identifier)
                                    {
                                        if (current_identifiers.ContainsKey(match.Value))
                                        {
                                            int identifier_value = current_identifiers[match.Value];
                                            all_tokens.Add(new Token(pattern.type, identifier_value, match.Value));
                                            Console.WriteLine($"Token created: {pattern.type} id:{identifier_value} value:{match.Value}");
                                        }
                                        else
                                        {
                                            int identifier_value = current_identifier_id;
                                            all_tokens.Add(new Token(pattern.type, identifier_value, match.Value));
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
                                            all_tokens.Add(new Token(pattern.type, code, match.Value));
                                            Console.WriteLine($"Token created: {pattern.type} id:{code} value:{match.Value}");
                                        }
                                    }
                                    else if (pattern.type == token_types.literal)
                                    {
                                        string cleanValue = match.Value;
                                        // If the value starts and ends with a quote, strip them
                                        if (cleanValue.StartsWith("\"") && cleanValue.EndsWith("\""))
                                        {
                                            cleanValue = cleanValue.Substring(1, cleanValue.Length - 2);
                                        }
                                        all_tokens.Add(new Token(pattern.type, 500, cleanValue));
                                        Console.WriteLine($"Token created: {pattern.type} id:{500} value:{match.Value}");
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
                                all_tokens.Add(new Token(token_types.unknown, 700, remaining_line_text[0].ToString()));
                                Console.WriteLine($"Token created: {token_types.unknown} id:{700} value:{remaining_line_text[0].ToString()}");
                                remaining_line_text = remaining_line_text.Substring(1).TrimStart();
                            }
                        }
                        if (lineHadTokens)
                        {
                            all_tokens.Add(new Token(token_types.endOfStatement, 600, "EOS"));
                            Console.WriteLine($"Token created: {token_types.endOfStatement} id:{600} value:{"EOS"}");
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
