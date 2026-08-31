using MiniDb.Engine.Phase3PageStorage;
using MiniDb.Engine.Phase4Indexing;
using MiniDb.Engine.Phase5QueryEngine;

namespace MiniDb.Tests;

public class Phase5QueryEngineTests : IDisposable
{
    private readonly string _testDbPath = "test_phase5.bin";
    private readonly DiskManager _diskManager;
    private readonly BufferPoolManager _bufferPool;
    private readonly BPlusTree _bTree;
    private readonly ExecutionEngine _engine;

    public Phase5QueryEngineTests()
    {
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        _diskManager = new DiskManager(_testDbPath);
        _bufferPool = new BufferPoolManager(_diskManager, poolSize: 10);
        _bTree = new BPlusTree(_bufferPool, rootPageId: 0, maxKeys: 3);
        _engine = new ExecutionEngine(_bTree);
    }

    [Fact]
    public void SqlLexer_ShouldTokenizeComplexQuery()
    {
        var sql = "SELECT * FROM users WHERE id = 10";
        var lexer = new SqlLexer(sql);
        var tokens = lexer.Tokenize();

        Assert.Equal(9, tokens.Count);
        Assert.Equal(TokenType.Keyword, tokens[0].Type);
        Assert.Equal("SELECT", tokens[0].Value);
        Assert.Equal(TokenType.Symbol, tokens[1].Type);
        Assert.Equal("*", tokens[1].Value);
    }

    [Fact]
    public void SqlParser_ShouldParseInsertAndSelectStatements()
    {
        var sqlInsert = "INSERT INTO students VALUES ('1', 'vijay')";
        var lexer = new SqlLexer(sqlInsert);
        var parser = new SqlParser(lexer.Tokenize());
        var stmt = parser.Parse();

        Assert.IsType<InsertStatement>(stmt);
        var insert = (InsertStatement)stmt;
        Assert.Equal("students", insert.TableName);
        Assert.Equal("1", insert.Values[0]);
    }

    [Fact]
    public void ExecutionEngine_ShouldExecuteFullSqlLifecycle()
    {
        // 1. CREATE TABLE
        var createRes = _engine.Execute("CREATE TABLE students (id INT, name TEXT)");
        Assert.Contains("created successfully", createRes);

        // 2. INSERT
        var insertRes = _engine.Execute("INSERT INTO students VALUES (1, vijay)");
        Assert.Contains("1 row inserted", insertRes);

        // 3. SELECT WHERE
        var selectWhereRes = _engine.Execute("SELECT * FROM students WHERE id = 1");
        Assert.Contains("vijay", selectWhereRes);

        // 4. SELECT ALL
        var selectAllRes = _engine.Execute("SELECT * FROM students");
        Assert.Contains("Table: students", selectAllRes);

        // 5. DELETE
        var deleteRes = _engine.Execute("DELETE FROM students WHERE id = 1");
        Assert.Contains("deleted", deleteRes);

        // 6. VERIFY DELETED
        var checkRes = _engine.Execute("SELECT * FROM students WHERE id = 1");
        Assert.Equal("(0 rows returned)", checkRes);
    }

    public void Dispose()
    {
        _diskManager.Dispose();
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
    }
}
