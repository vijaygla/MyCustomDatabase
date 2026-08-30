namespace MiniDb.Engine.Phase3PageStorage;

public class BufferPoolManager
{
    private readonly DiskManager _diskManager;
    private readonly Dictionary<int, Page> _pageTable = new();
    private readonly int _poolSize;

    public BufferPoolManager(DiskManager diskManager, int poolSize = 10)
    {
        _diskManager = diskManager;
        _poolSize = poolSize;
    }

    public Page FetchPage(int pageId)
    {
        if (_pageTable.TryGetValue(pageId, out var page))
        {
            page.PinCount++;
            return page;
        }

        // RAM Pool full hone par unpinned/dirty pages flush karein
        if (_pageTable.Count >= _poolSize)
        {
            FlushAllPages();
        }

        page = new Page(pageId);
        _diskManager.ReadPage(pageId, page.Data);
        page.PinCount = 1;

        _pageTable[pageId] = page;
        return page;
    }

    public void UnpinPage(int pageId, bool isDirty)
    {
        if (_pageTable.TryGetValue(pageId, out var page))
        {
            if (isDirty) page.IsDirty = true;
            if (page.PinCount > 0) page.PinCount--;
        }
    }

    public void FlushAllPages()
    {
        foreach (var kvp in _pageTable)
        {
            var page = kvp.Value;
            if (page.IsDirty)
            {
                _diskManager.WritePage(page.PageId, page.Data);
                page.IsDirty = false;
            }
        }
    }
}
