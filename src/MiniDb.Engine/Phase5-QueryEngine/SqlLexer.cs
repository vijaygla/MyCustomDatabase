namespace MiniDb.Engine.Phase5QueryEngine;

public enum TokenType
{
    Keyword,
    Identifier,
    Literal,
    Symbol,
    EOF
}

public class Token
{
    public TokenType Type { get; set; }
    public string Value { get; set; } = string.Empty;

    public override string ToString() => $"Token({Type}, '{Value}')";
}

public class SqlLexer
{
    private readonly string _text;
    private int _position;

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "CREATE", "TABLE", "INSERT", "INTO", "VALUES",
        "SELECT", "FROM", "WHERE", "DELETE", "DROP",
        "UPDATE", "SET",
        "BEGIN", "COMMIT", "ROLLBACK", "TRANSACTION", "START"
    };

    public SqlLexer(string text)
    {
        _text = text;
        _position = 0;
    }

    private char Current => _position < _text.Length ? _text[_position] : '\0';

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_position < _text.Length)
        {
            if (char.IsWhiteSpace(Current))
            {
                _position++;
                continue;
            }

            // Handle SQL Comments (-- to end of line)
            if (Current == '-' && _position + 1 < _text.Length && _text[_position + 1] == '-')
            {
                while (_position < _text.Length && Current != '\n' && Current != '\r')
                {
                    _position++;
                }
                continue;
            }

            if (Current is '(' or ')' or ',' or '=' or '*')
            {
                tokens.Add(new Token { Type = TokenType.Symbol, Value = Current.ToString() });
                _position++;
                continue;
            }

            if (Current is '\'' or '"')
            {
                tokens.Add(ReadLiteral());
                continue;
            }

            if (char.IsLetterOrDigit(Current) || Current == '_')
            {
                tokens.Add(ReadIdentifierOrKeyword());
                continue;
            }

            throw new Exception($"Unexpected character: '{Current}' at position {_position}");
        }

        tokens.Add(new Token { Type = TokenType.EOF, Value = "" });
        return tokens;
    }

    private Token ReadLiteral()
    {
        char quote = Current;
        _position++;
        int start = _position;

        while (_position < _text.Length && Current != quote)
        {
            _position++;
        }

        string val = _text.Substring(start, _position - start);
        _position++;

        return new Token { Type = TokenType.Literal, Value = val };
    }

    private Token ReadIdentifierOrKeyword()
    {
        int start = _position;

        while (_position < _text.Length && (char.IsLetterOrDigit(Current) || Current == '_'))
        {
            _position++;
        }

        string word = _text.Substring(start, _position - start);
        var type = Keywords.Contains(word) ? TokenType.Keyword : TokenType.Identifier;

        return new Token { Type = type, Value = word };
    }
}
