using MiniDb.Engine.Phase3PageStorage;

namespace MiniDb.Tests;

public class Phase3StorageTests : IDisposable
{
    private readonly string _testDbPath = "test_phase3.bin";
    private readonly DiskManager _diskManager;
    private readonly BufferPoolManager _bufferPool;

    public Phase3StorageTests()
    {
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        _diskManager = new DiskManager(_testDbPath);
        _bufferPool = new BufferPoolManager(_diskManager, poolSize: 3);
    }

    [Fact]
    public void DiskManager_ShouldReadAndWritePageData()
    {
        int pageId = 0;
        byte[] writeBuffer = new byte[Page.PAGE_SIZE];
        string testData = "DiskManager Test Data";

        System.Text.Encoding.UTF8.GetBytes(testData).CopyTo(writeBuffer, 0);
        _diskManager.WritePage(pageId, writeBuffer);

        byte[] readBuffer = new byte[Page.PAGE_SIZE];
        _diskManager.ReadPage(pageId, readBuffer);

        string readData = System.Text.Encoding.UTF8.GetString(readBuffer, 0, testData.Length);

        Assert.Equal(testData, readData);
    }

    [Fact]
    public void BufferPoolManager_ShouldFetchAndEvictPagesCorrectly()
    {
        // Fetch 3 pages (fills buffer pool capacity)
        var p0 = _bufferPool.FetchPage(0);
        var p1 = _bufferPool.FetchPage(1);
        var p2 = _bufferPool.FetchPage(2);

        Assert.NotNull(p0);
        Assert.NotNull(p1);
        Assert.NotNull(p2);

        // Mark page 0 dirty and unpin all with isDirty flag
        _bufferPool.UnpinPage(0, isDirty: true);
        _bufferPool.UnpinPage(1, isDirty: false);
        _bufferPool.UnpinPage(2, isDirty: false);

        // Fetching 4th page forces pool flush when capacity reached
        var p3 = _bufferPool.FetchPage(3);
        Assert.NotNull(p3);
    }

    public void Dispose()
    {
        _diskManager.Dispose();
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
    }
}
