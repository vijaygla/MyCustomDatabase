using MiniDb.Cli;
using MiniDb.Engine.Phase3PageStorage;
using MiniDb.Engine.Phase4Indexing;
using MiniDb.Engine.Phase5QueryEngine;
using MiniDb.Engine.Phase6WAL;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("==================================================================");
Console.WriteLine(" Welcome to Custom Database Engine Shell v1.0 (Interactive REPL)");
Console.WriteLine(" Supported: SQL Queries, Transactions (BEGIN/COMMIT/ROLLBACK), WAL");
Console.WriteLine(" Type .help for meta commands, or .exit to quit.");
Console.WriteLine("==================================================================");
Console.ResetColor();

string dbPath = "database.bin";
string walPath = "database.wal";

using var diskManager = new DiskManager(dbPath);
using var walManager = new WalManager(walPath);

var bufferPool = new BufferPoolManager(diskManager, poolSize: 10);
var bTree = new BPlusTree(bufferPool, rootPageId: 0, maxKeys: 3);
var executionEngine = new ExecutionEngine(bTree, walManager);

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("db> ");
    Console.ResetColor();

    string? input = Console.ReadLine();

    if (input == null)
    {
        PerformSafeExit(executionEngine, bufferPool);
        break;
    }

    input = input.Trim();
    if (string.IsNullOrWhiteSpace(input))
        continue;

    string normalizedInput = input.TrimEnd(';').Trim();

    if (normalizedInput.StartsWith("."))
    {
        if (HandleMetaCommand(normalizedInput, bTree, bufferPool, executionEngine, dbPath))
            break;
        continue;
    }

    if (normalizedInput.Equals("SHOW TABLES", StringComparison.OrdinalIgnoreCase))
    {
        HandleMetaCommand(".tables", bTree, bufferPool, executionEngine, dbPath);
        continue;
    }

    if (normalizedInput.Equals("SHOW DATABASES", StringComparison.OrdinalIgnoreCase) ||
        normalizedInput.Equals("SHOW DATABASE", StringComparison.OrdinalIgnoreCase))
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- Databases ---");
        Console.WriteLine($" • main ({dbPath})");
        Console.ResetColor();
        continue;
    }

    if (normalizedInput.Equals("SHOW SCHEMA", StringComparison.OrdinalIgnoreCase) ||
        normalizedInput.Equals("SHOW SCHEMAS", StringComparison.OrdinalIgnoreCase) ||
        normalizedInput.StartsWith("DESCRIBE ", StringComparison.OrdinalIgnoreCase) ||
        normalizedInput.StartsWith("DESC ", StringComparison.OrdinalIgnoreCase))
    {
        HandleMetaCommand(".schema", bTree, bufferPool, executionEngine, dbPath);
        continue;
    }

    if (normalizedInput.Equals("EXIT", StringComparison.OrdinalIgnoreCase) ||
        normalizedInput.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
    {
        PerformSafeExit(executionEngine, bufferPool);
        break;
    }

    if (normalizedInput.Equals("LIST", StringComparison.OrdinalIgnoreCase))
    {
        var allData = bTree.GetAllKeys();
        if (allData.Count == 0)
        {
            Console.WriteLine("(empty database)");
        }
        else
        {
            Console.WriteLine($"--- Total Keys in B+ Tree Index: {allData.Count} ---");
            foreach (var kvp in allData)
            {
                Console.WriteLine($"{kvp.Key} => \"{kvp.Value}\"");
            }
        }
        continue;
    }

    try
    {
        string result = executionEngine.Execute(normalizedInput);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(result);
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Execution Error]: {ex.Message}");
        Console.ResetColor();
    }
}

static void PerformSafeExit(ExecutionEngine executionEngine, BufferPoolManager bufferPool)
{
    try
    {
        string rollbackResult = executionEngine.Execute("ROLLBACK");
        if (!rollbackResult.Contains("No active transaction", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[Transaction Safety Warning]: Active uncommitted transaction detected!");
            Console.WriteLine($"[Auto-Rollback]: {rollbackResult}");
            Console.ResetColor();
        }
    }
    catch { }

    bufferPool.FlushAllPages();
    Console.WriteLine("Flushing dirty pages to disk and exiting. Goodbye!");
}

static bool HandleMetaCommand(string command, BPlusTree bTree, BufferPoolManager bufferPool, ExecutionEngine executionEngine, string dbPath)
{
    string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    string meta = parts[0].ToLower();

    switch (meta)
    {
        case ".exit":
        case ".quit":
            PerformSafeExit(executionEngine, bufferPool);
            return true;

        case ".help":
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n======================= META COMMANDS ========================");
            Console.WriteLine(" .help            Show this detailed help menu");
            Console.WriteLine(" .tables          List all existing tables (Alias: SHOW TABLES)");
            Console.WriteLine(" .schema          Show catalog/schema for tables (Alias: SHOW SCHEMA / DESC)");
            Console.WriteLine(" LIST             Display all raw key-value pairs stored in B+ Tree");
            Console.WriteLine(" .clear           Clear terminal console screen");
            Console.WriteLine(" .exit / .quit    Flush buffer pool dirty pages to disk and exit");
            Console.WriteLine("======================== SQL STATEMENTS ======================");
            Console.WriteLine(" SHOW TABLES      List all active tables in database");
            Console.WriteLine(" SHOW DATABASES   Show current active database files");
            Console.WriteLine(" CREATE TABLE     CREATE TABLE users (id INT, name TEXT)");
            Console.WriteLine(" INSERT INTO      INSERT INTO users VALUES ('1', 'vijay')");
            Console.WriteLine(" SELECT ALL       SELECT * FROM users");
            Console.WriteLine(" SELECT WHERE     SELECT * FROM users WHERE id = 1");
            Console.WriteLine(" DELETE WHERE     DELETE FROM users WHERE id = 1");
            Console.WriteLine(" TRANSACTIONS     BEGIN | COMMIT | ROLLBACK");
            Console.WriteLine("==============================================================\n");
            Console.ResetColor();
            break;

        case ".tables":
            // ExecutionEngine ki _tables dictionary se table names fetch karna
            var tableNames = executionEngine.GetTableNames();

            if (tableNames.Count == 0)
            {
                Console.WriteLine("No tables found.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("--- Tables ---");
                foreach (var t in tableNames)
                {
                    Console.WriteLine($" • {t}");
                }
                Console.ResetColor();
            }
            break;

        case ".schema":
            var rawKeysForSchema = bTree.GetAllKeys();
            var detectedTables = executionEngine.GetTableNames();

            if (detectedTables.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("--- Detected Active Tables ---");
                foreach (var tbl in detectedTables)
                {
                    Console.WriteLine($" Table: {tbl}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("No table schemas found.");
            }
            break;

        case ".clear":
            Console.Clear();
            break;

        default:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unknown meta command '{command}'. Type .help for options.");
            Console.ResetColor();
            break;
    }

    return false;
}
