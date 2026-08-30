namespace MiniDb.Engine.Phase5QueryEngine;

public enum TokenType
{
    Keyword,      // SELECT, INSERT, CREATE, TABLE, FROM, WHERE, VALUES, INTO
    Identifier,   // Table names, Column names (e.g., users, id, name)
    StringLiteral,// 'vijay', 'kumar'
    NumberLiteral,// 1, 42
    Symbol,       // *, =, ( , ) , ,
    EOF           // End of Query
}

public record Token(TokenType Type, string Value);

public class SqlLexer
{
    private readonly string _text;
    private int _position;

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "CREATE", "TABLE", "INSERT", "INTO", "VALUES", "SELECT", "FROM", "WHERE", "INT", "TEXT"
    };

    public SqlLexer(string text)
    {
        _text = text ?? string.Empty;
        _position = 0;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_position < _text.Length)
        {
            char current = _text[_position];

            // Skip Whitespaces
            if (char.IsWhiteSpace(current))
            {
                _position++;
                continue;
            }

            // Symbols (*, =, (, ), ,)
            if (current is '*' or '=' or '(' or ')' or ',')
            {
                tokens.Add(new Token(TokenType.Symbol, current.ToString()));
                _position++;
                continue;
            }

            // String Literals ('vijay')
            if (current == '\'')
            {
                _position++; // Skip opening quote
                int start = _position;
                while (_position < _text.Length && _text[_position] != '\'')
                {
                    _position++;
                }
                string strVal = _text.Substring(start, _position - start);
                if (_position < _text.Length && _text[_position] == '\'')
                {
                    _position++; // Skip closing quote
                }
                tokens.Add(new Token(TokenType.StringLiteral, strVal));
                continue;
            }

            // Numbers (1, 100)
            if (char.IsDigit(current))
            {
                int start = _position;
                while (_position < _text.Length && char.IsDigit(_text[_position]))
                {
                    _position++;
                }
                string numVal = _text.Substring(start, _position - start);
                tokens.Add(new Token(TokenType.NumberLiteral, numVal));
                continue;
            }

            // Keywords or Identifiers
            if (char.IsLetter(current) || current == '_')
            {
                int start = _position;
                while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || _text[_position] == '_'))
                {
                    _position++;
                }
                string word = _text.Substring(start, _position - start);

                if (Keywords.Contains(word))
                {
                    tokens.Add(new Token(TokenType.Keyword, word.ToUpper()));
                }
                else
                {
                    tokens.Add(new Token(TokenType.Identifier, word));
                }
                continue;
            }

            // Unknown character safety skip
            _position++;
        }

        tokens.Add(new Token(TokenType.EOF, string.Empty));
        return tokens;
    }
}
