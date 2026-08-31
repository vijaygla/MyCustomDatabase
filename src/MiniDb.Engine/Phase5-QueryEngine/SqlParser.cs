namespace MiniDb.Engine.Phase5QueryEngine;

// Transaction AST Nodes
public class BeginTransactionStatement : SqlStatement { }
public class CommitTransactionStatement : SqlStatement { }
public class RollbackTransactionStatement : SqlStatement { }

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
        string cmd = Current.Value.ToUpper();

        // Transaction Handling
        if (cmd == "BEGIN" || cmd == "START")
        {
            _position++;
            if (Current.Value.Equals("TRANSACTION", StringComparison.OrdinalIgnoreCase))
            {
                _position++;
            }
            return new BeginTransactionStatement();
        }

        if (cmd == "COMMIT")
        {
            _position++;
            if (Current.Value.Equals("TRANSACTION", StringComparison.OrdinalIgnoreCase))
            {
                _position++;
            }
            return new CommitTransactionStatement();
        }

        if (cmd == "ROLLBACK")
        {
            _position++;
            if (Current.Value.Equals("TRANSACTION", StringComparison.OrdinalIgnoreCase))
            {
                _position++;
            }
            return new RollbackTransactionStatement();
        }

        return cmd switch
        {
            "CREATE" => ParseCreateTable(),
            "INSERT" => ParseInsert(),
            "SELECT" => ParseSelect(),
            "DELETE" => ParseDelete(),
            "UPDATE" => ParseUpdate(),
            "DROP" => ParseDropTable(),
            _ => throw new Exception($"Unsupported command or keyword: '{Current.Value}'")
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

            if (Current.Type != TokenType.Keyword && Current.Type != TokenType.Identifier)
            {
                throw new Exception($"Syntax Error: Expected Data Type, found '{Current.Value}'");
            }
            string dataType = Current.Value;
            _position++;

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
            // Accept both Literals (strings) and Identifiers/Numbers for values
            if (Current.Type != TokenType.Literal && Current.Type != TokenType.Identifier && Current.Type != TokenType.Keyword)
            {
                throw new Exception($"Syntax Error: Expected value, found '{Current.Value}'");
            }

            stmt.Values.Add(Current.Value);
            _position++;

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

        if (Current.Value.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
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

        if (Current.Value.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            Match(TokenType.Keyword, "WHERE");
            stmt.WhereColumn = Match(TokenType.Identifier).Value;
            Match(TokenType.Symbol, "=");
            stmt.WhereValue = Current.Value;
            _position++;
        }

        return stmt;
    }

    private UpdateStatement ParseUpdate()
    {
        Match(TokenType.Keyword, "UPDATE");

        var stmt = new UpdateStatement
        {
            TableName = Match(TokenType.Identifier).Value
        };

        Match(TokenType.Keyword, "SET");
        stmt.ColumnName = Match(TokenType.Identifier).Value;
        Match(TokenType.Symbol, "=");
        stmt.NewValue = Current.Value;
        _position++;

        if (Current.Value.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
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
