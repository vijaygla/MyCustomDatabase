using MiniDb.Engine.Phase3PageStorage;
using MiniDb.Engine.Phase4Indexing;
using MiniDb.Engine.Phase5QueryEngine;

Console.WriteLine("=== MiniDb Engine (Full SQL & Persistent B+ Tree Storage) ===");

using var diskManager = new DiskManager("minidb.bin");
var bufferPool = new BufferPoolManager(diskManager, poolSize: 10);
var bTree = new BPlusTree(bufferPool, rootPageId: 0, maxKeys: 3);
var executionEngine = new ExecutionEngine(bTree);

Console.WriteLine("\n=== SQL Interactive CLI Ready ===");
Console.WriteLine("Supported SQL Commands:");
Console.WriteLine("  CREATE TABLE users (id INT, name TEXT)");
Console.WriteLine("  INSERT INTO users VALUES ('1', 'vijay')");
Console.WriteLine("  SELECT * FROM users");
Console.WriteLine("  SELECT * FROM users WHERE id = 1");
Console.WriteLine("  DELETE FROM users WHERE id = 1");
Console.WriteLine("  DELETE FROM users");
Console.WriteLine("  DROP TABLE users");
Console.WriteLine("  LIST  (Raw Index View)");
Console.WriteLine("  EXIT\n");

while (true)
{
    Console.Write("minidb> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
    {
        bufferPool.FlushAllPages();
        Console.WriteLine("Flushing dirty pages to minidb.bin and exiting. Goodbye!");
        break;
    }

    if (input.Equals("LIST", StringComparison.OrdinalIgnoreCase))
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
        string result = executionEngine.Execute(input);
        Console.WriteLine(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Execution Error]: {ex.Message}");
    }
}

