namespace MiniDb.Engine.Phase5QueryEngine;

public class SqlParser
{
    private readonly List<Token> _tokens;
    private int _position;

    public SqlParser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    private Token Match(TokenType type, string? value = null)
    {
        var token = Current;
        if (token.Type != type || (value != null && !token.Value.Equals(value, StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception($"Syntax Error: Expected '{value ?? type.ToString()}', found '{token.Value}'");
        }
        _position++;
        return token;
    }

    public SqlStatement Parse()
    {
        if (Current.Type != TokenType.Keyword)
        {
            throw new Exception("Invalid Query: Command must start with a SQL Keyword");
        }

        return Current.Value.ToUpper() switch
        {
            "CREATE" => ParseCreateTable(),
            "INSERT" => ParseInsert(),
            "SELECT" => ParseSelect(),
            "DELETE" => ParseDelete(),
            "DROP" => ParseDropTable(),
            _ => throw new Exception($"Unsupported command: '{Current.Value}'")
        };
    }

    private CreateTableStatement ParseCreateTable()
    {
        Match(TokenType.Keyword, "CREATE");
        Match(TokenType.Keyword, "TABLE");

        var stmt = new CreateTableStatement
        {
            TableName = Match(TokenType.Identifier).Value
        };

        Match(TokenType.Symbol, "(");
        while (true)
        {
            string colName = Match(TokenType.Identifier).Value;
            string dataType = Match(TokenType.Keyword).Value;
            stmt.Columns.Add(new ColumnDefinition { Name = colName, DataType = dataType });

            if (Current.Value == ")") break;
            Match(TokenType.Symbol, ",");
        }
        Match(TokenType.Symbol, ")");

        return stmt;
    }

    private InsertStatement ParseInsert()
    {
        Match(TokenType.Keyword, "INSERT");
        Match(TokenType.Keyword, "INTO");

        var stmt = new InsertStatement
        {
            TableName = Match(TokenType.Identifier).Value
        };

        Match(TokenType.Keyword, "VALUES");
        Match(TokenType.Symbol, "(");

        while (true)
        {
            string val = Current.Value;
            _position++;
            stmt.Values.Add(val);

            if (Current.Value == ")") break;
            Match(TokenType.Symbol, ",");
        }
        Match(TokenType.Symbol, ")");

        return stmt;
    }

    private SelectStatement ParseSelect()
    {
        Match(TokenType.Keyword, "SELECT");
        Match(TokenType.Symbol, "*");
        Match(TokenType.Keyword, "FROM");

        var stmt = new SelectStatement
        {
            TableName = Match(TokenType.Identifier).Value
        };

        if (Current.Type == TokenType.Keyword && Current.Value.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            Match(TokenType.Keyword, "WHERE");
            stmt.WhereColumn = Match(TokenType.Identifier).Value;
            Match(TokenType.Symbol, "=");
            stmt.WhereValue = Current.Value;
            _position++;
        }

        return stmt;
    }

    private DeleteStatement ParseDelete()
    {
        Match(TokenType.Keyword, "DELETE");
        Match(TokenType.Keyword, "FROM");

        var stmt = new DeleteStatement
        {
            TableName = Match(TokenType.Identifier).Value
        };

        if (Current.Type == TokenType.Keyword && Current.Value.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            Match(TokenType.Keyword, "WHERE");
            stmt.WhereColumn = Match(TokenType.Identifier).Value;
            Match(TokenType.Symbol, "=");
            stmt.WhereValue = Current.Value;
            _position++;
        }

        return stmt;
    }

    private DropTableStatement ParseDropTable()
    {
        Match(TokenType.Keyword, "DROP");
        Match(TokenType.Keyword, "TABLE");

        return new DropTableStatement
        {
            TableName = Match(TokenType.Identifier).Value
        };
    }
}
