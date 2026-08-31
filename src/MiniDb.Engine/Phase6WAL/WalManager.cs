using System.Text;

namespace MiniDb.Engine.Phase6WAL;

public class WalManager : IDisposable
{
    private readonly FileStream _walStream;
    private readonly string _walFilePath;
    private long _nextLsn = 1;

    public WalManager(string walFilePath = "minidb.wal")
    {
        _walFilePath = walFilePath;

        // Fixed FileShare from None to ReadWrite to prevent startup lock/freeze on app restart
        _walStream = new FileStream(
            _walFilePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite
        );

        // Seek to end for append-only writing
        _walStream.Seek(0, SeekOrigin.End);
    }

    public long WriteRecord(LogRecordType type, string key, string value)
    {
        var record = new WalRecord
        {
            LSN = _nextLsn++,
            Type = type,
            Key = key,
            Value = value
        };

        byte[] recordBytes = record.Serialize();

        // Write record length followed by binary data
        using var writer = new BinaryWriter(_walStream, Encoding.UTF8, leaveOpen: true);
        writer.Write(recordBytes.Length);
        writer.Write(recordBytes);

        // Direct disk flush for absolute durability
        _walStream.Flush(flushToDisk: true);

        return record.LSN;
    }

    public List<WalRecord> RecoverLogRecords()
    {
        var records = new List<WalRecord>();
        _walStream.Seek(0, SeekOrigin.Begin);

        using var reader = new BinaryReader(_walStream, Encoding.UTF8, leaveOpen: true);

        while (_walStream.Position < _walStream.Length)
        {
            try
            {
                int length = reader.ReadInt32();
                byte[] data = reader.ReadBytes(length);

                using var recordStream = new MemoryStream(data);
                using var recordReader = new BinaryReader(recordStream, Encoding.UTF8);

                var record = WalRecord.Deserialize(recordReader);
                records.Add(record);

                if (record.LSN >= _nextLsn)
                {
                    _nextLsn = record.LSN + 1;
                }
            }
            catch (EndOfStreamException)
            {
                // Partial write recovery guard
                break;
            }
        }

        return records;
    }

    public void ClearLog()
    {
        _walStream.SetLength(0);
        _walStream.Flush(flushToDisk: true);
        _nextLsn = 1;
    }

    public void Dispose()
    {
        try
        {
            _walStream?.Flush(true);
            _walStream?.Dispose();
        }
        catch
        {
            // Ignore stream dispose errors during force exit
        }
    }
}
