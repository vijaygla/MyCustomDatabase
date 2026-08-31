namespace MiniDb.Engine.Phase3PageStorage;

public class DiskManager : IDisposable
{
    private readonly FileStream _fileStream;
    private readonly string _dbFilePath;

    public DiskManager(string dbFilePath = "minidb.bin")
    {
        _dbFilePath = dbFilePath;

        // Fixed FileShare from None to ReadWrite to prevent startup deadlock/freeze on app restart
        _fileStream = new FileStream(
            _dbFilePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite
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

    public void ReadPage(int pageId, byte[] pageData)
    {
        if (pageData.Length != Page.PAGE_SIZE)
        {
            throw new ArgumentException($"Buffer must be exactly {Page.PAGE_SIZE} bytes.", nameof(pageData));
        }

        long offset = (long)pageId * Page.PAGE_SIZE;

        if (offset >= _fileStream.Length)
        {
            Array.Clear(pageData, 0, Page.PAGE_SIZE);
            return;
        }

        _fileStream.Seek(offset, SeekOrigin.Begin);

        int totalBytesRead = 0;
        while (totalBytesRead < Page.PAGE_SIZE)
        {
            int bytesRead = _fileStream.Read(pageData, totalBytesRead, Page.PAGE_SIZE - totalBytesRead);
            if (bytesRead == 0) break;
            totalBytesRead += bytesRead;
        }
    }

    public void Dispose()
    {
        try
        {
            _fileStream?.Flush(true); // Force OS to write to disk
            _fileStream?.Dispose();
        }
        catch
        {
            // Ignore stream dispose errors during force exit
        }
    }
}
