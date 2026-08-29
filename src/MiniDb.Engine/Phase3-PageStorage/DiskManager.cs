namespace MiniDb.Engine.Phase3PageStorage;

public class DiskManager : IDisposable
{
    private readonly FileStream _fileStream;
    private readonly string _dbFilePath;

    public DiskManager(string dbFilePath = "minidb.bin")
    {
        _dbFilePath = dbFilePath;
        _fileStream = new FileStream(
            _dbFilePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None
        );
    }

    public void WritePage(int pageId, byte[] pageData)
    {
        if (pageData.Length != Page.PAGE_SIZE)
        {
            throw new ArgumentException($"Page data must be exactly {Page.PAGE_SIZE} bytes.");
        }

        long offset = (long)pageId * Page.PAGE_SIZE;
        _fileStream.Seek(offset, SeekOrigin.Begin);
        _fileStream.Write(pageData, 0, Page.PAGE_SIZE);
        _fileStream.Flush();
    }

    public byte[] ReadPage(int pageId)
    {
        byte[] buffer = new byte[Page.PAGE_SIZE];
        long offset = (long)pageId * Page.PAGE_SIZE;

        if (offset >= _fileStream.Length)
        {
            return buffer; // Return empty 4KB buffer for new page
        }

        _fileStream.Seek(offset, SeekOrigin.Begin);
        _fileStream.Read(buffer, 0, Page.PAGE_SIZE);
        return buffer;
    }

    public void Dispose()
    {
        _fileStream?.Dispose();
    }
}
