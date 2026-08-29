# Folder Structure
```
MyCustomDatabase/
├── MyCustomDatabase.sln
│
├── src/
│   ├── MiniDb.Engine/                   <-- Core Database Engine (Class Library)
│   │   ├── MiniDb.Engine.csproj
│   │   │
│   │   ├── Phase1-InMemory/            <-- Phase 1: In-Memory Key-Value Logic
│   │   │   └── InMemoryStore.cs
│   │   │
│   │   ├── Phase2-AppendOnly/          <-- Phase 2: Log-Structured Disk Storage
│   │   │   ├── AppendOnlyLog.cs
│   │   │   └── KeyDirIndex.cs
│   │   │
│   │   ├── Phase3-PageStorage/          <-- Phase 3: Binary Page & Buffer Pool
│   │   │   ├── DiskManager.cs
│   │   │   ├── Page.cs
│   │   │   └── SlottedPage.cs
│   │   │
│   │   └── Phase4-Indexing/            <-- Phase 4: B+ Tree Indexing
│   │       ├── BTreeIndex.cs
│   │       └── BTreeNode.cs
│   │
│   └── MiniDb.Cli/                      <-- REPL / Interactive CLI (Console App)
│       ├── MiniDb.Cli.csproj
│       └── Program.cs
│
└── tests/
    └── MiniDb.Tests/                    <-- Unit Tests (Optional)
        ├── MiniDb.Tests.csproj
        ├── Phase1Tests.cs
        └── Phase2Tests.cs
```
# How Everything Works Together
```
                   USER
                     │
                     ▼
               Program.cs
                     │
          ┌──────────┴──────────┐
          │                     │
          ▼                     ▼
      SET/GET/DELETE       PAGE COMMANDS
          │                     │
          ▼                     ▼
   AppendOnlyStore      BufferPoolManager
          │                     │
          ▼                     ▼
    Dictionary RAM         Page Objects
          │                     │
          ▼                     ▼
       data.db             DiskManager
                                │
                                ▼
                           minidb.bin
```

# 
Remove-Item minidb.bin -ErrorAction SilentlyContinue
dotnet run --project src/MiniDb.Cli/MiniDb.Cli.csproj