using System.Text;

namespace MiniDb.Engine.Phase6WAL;

public enum LogRecordType
{
    Insert,
    Delete,
    Update,
    Begin,
    Commit,
    Abort
}

public class WalRecord
{
    public long LSN { get; set; } // Log Sequence Number
    public LogRecordType Type { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        writer.Write(LSN);
        writer.Write((byte)Type);
        writer.Write(Key);
        writer.Write(Value);

        writer.Flush();
        return ms.ToArray();
    }

    public static WalRecord Deserialize(BinaryReader reader)
    {
        var record = new WalRecord
        {
            LSN = reader.ReadInt64(),
            Type = (LogRecordType)reader.ReadByte(),
            Key = reader.ReadString(),
            Value = reader.ReadString()
        };
        return record;
    }
}
