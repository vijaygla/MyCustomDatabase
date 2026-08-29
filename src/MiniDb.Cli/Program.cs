using MiniDb.Engine.Phase1InMemory;

IKeyValueStore db = new InMemoryStore();

Console.WriteLine("=== MiniDb Engine (Phase 1: In-Memory) ===");
Console.WriteLine("Commands: SET <key> <value> | GET <key> | DELETE <key> | EXIT\n");

while (true)
{
    Console.Write("minidb> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
    var command = parts[0].ToUpper();

    if (command == "EXIT")
        break;

    switch (command)
    {
        case "SET":
            if (parts.Length < 3)
            {
                Console.WriteLine("Error: SET requires both key and value. Usage: SET <key> <value>");
            }
            else
            {
                db.Set(parts[1], parts[2]);
                Console.WriteLine("OK");
            }
            break;

        case "GET":
            if (parts.Length < 2)
            {
                Console.WriteLine("Error: GET requires a key. Usage: GET <key>");
            }
            else
            {
                var val = db.Get(parts[1]);
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
                var removed = db.Delete(parts[1]);
                Console.WriteLine(removed ? "OK" : "(nil)");
            }
            break;

        default:
            Console.WriteLine($"Unknown command: '{command}'");
            break;
    }
}
