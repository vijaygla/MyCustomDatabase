namespace MiniDb.Engine.Phase3PageStorage;

public class Page
{
    public const int PAGE_SIZE = 4096; // Standard 4KB Page

    public int PageId { get; set; }
    public byte[] Data { get; private set; } = new byte[PAGE_SIZE];
    public bool IsDirty { get; set; }
    public int PinCount { get; set; }

    public Page(int pageId)
    {
        PageId = pageId;
        IsDirty = false;
        PinCount = 0;
    }

    public void Clear()
    {
        Array.Clear(Data, 0, Data.Length);
        IsDirty = false;
        PinCount = 0;
    }
}
