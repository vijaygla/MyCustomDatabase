using MiniDb.Engine.Phase3PageStorage;
using MiniDb.Engine.Phase4Indexing;

Console.WriteLine("=== MiniDb Engine (Phase 4: Persistent B+ Tree Engine) ===");

// 1. Initialize Disk Storage Engine & Buffer Pool Cache Manager (Phase 3)
using var diskManager = new DiskManager("minidb.bin");
var bufferPool = new BufferPoolManager(diskManager, poolSize: 10);

// 2. Initialize Persistent B+ Tree Index Engine (Phase 4)
var bTree = new BPlusTree(bufferPool, rootPageId: 0, maxKeys: 3);

Console.WriteLine("=== Interactive CLI Started ===");
Console.WriteLine("Commands:");
Console.WriteLine("  SET <key> <value>  : Insert or Update key-value pair");
Console.WriteLine("  GET <key>          : Lookup value by key (O(log N))");
Console.WriteLine("  DELETE <key>       : Remove key from database");
Console.WriteLine("  LIST               : Scan all keys stored in B+ Tree");
Console.WriteLine("  EXIT               : Flush pages to disk and exit\n");

while (true)
{
    Console.Write("minidb> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
    var command = parts[0].ToUpper();

    if (command == "EXIT")
    {
        bufferPool.FlushAllPages();
        Console.WriteLine("Flushing dirty pages to minidb.bin and exiting. Goodbye!");
        break;
    }

    switch (command)
    {
        case "SET":
            if (parts.Length < 3)
            {
                Console.WriteLine("Error: SET requires both key and value. Usage: SET <key> <value>");
            }
            else
            {
                bTree.Insert(parts[1], parts[2]);
                Console.WriteLine("OK (Indexed & Persisted to 4KB Pages)");
            }
            break;

        case "GET":
            if (parts.Length < 2)
            {
                Console.WriteLine("Error: GET requires a key. Usage: GET <key>");
            }
            else
            {
                var val = bTree.Search(parts[1]);
                Console.WriteLine(val != null ? $"\"{val}\"" : "(nil)");
            }
            break;

        case "DELETE":
            if (parts.Length < 2)
            {
                Console.WriteLine("Error: DELETE requires a key. Usage: DELETE <key>");
            }
            else
            {
                bool deleted = bTree.Delete(parts[1]);
                Console.WriteLine(deleted ? "OK" : "(nil)");
            }
            break;

        case "LIST":
            var allData = bTree.GetAllKeys();
            if (allData.Count == 0)
            {
                Console.WriteLine("(empty database)");
            }
            else
            {
                Console.WriteLine($"--- Total Keys: {allData.Count} ---");
                foreach (var kvp in allData)
                {
                    Console.WriteLine($"{kvp.Key} : \"{kvp.Value}\"");
                }
            }
            break;

        default:
            Console.WriteLine($"Unknown command: '{command}'");
            break;
    }
}
