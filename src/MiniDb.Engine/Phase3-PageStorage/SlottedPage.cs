using System.Text;

namespace MiniDb.Engine.Phase3PageStorage;

public class SlottedPage
{
    private const int HEADER_SIZE = 6; // PageId (4B) + SlotCount (2B)
    private const int SLOT_SIZE = 8;   // Offset (4B) + Length (4B)

    public static ushort GetSlotCount(byte[] data)
    {
        return BitConverter.ToUInt16(data, 4);
    }

    private static void SetSlotCount(byte[] data, ushort count)
    {
        byte[] bytes = BitConverter.GetBytes(count);
        Array.Copy(bytes, 0, data, 4, 2);
    }

    public static bool InsertRecord(byte[] data, string key, string value)
    {
        ushort slotCount = GetSlotCount(data);
        string payload = $"{key}={value}";
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        int recordLen = payloadBytes.Length;

        // Calculate free space limits
        int currentSlotEnd = HEADER_SIZE + (slotCount * SLOT_SIZE);

        int lowestOffset = Page.PAGE_SIZE;
        for (int i = 0; i < slotCount; i++)
        {
            int slotPos = HEADER_SIZE + (i * SLOT_SIZE);
            int offset = BitConverter.ToInt32(data, slotPos);
            int len = BitConverter.ToInt32(data, slotPos + 4);
            if (len > 0 && offset < lowestOffset)
            {
                lowestOffset = offset;
            }
        }

        int freeSpace = lowestOffset - currentSlotEnd;
        if (freeSpace < (SLOT_SIZE + recordLen))
        {
            return false; // Page is full!
        }

        // Write record payload backwards from bottom
        int newOffset = lowestOffset - recordLen;
        Array.Copy(payloadBytes, 0, data, newOffset, recordLen);

        // Write slot entry
        int newSlotPos = HEADER_SIZE + (slotCount * SLOT_SIZE);
        Array.Copy(BitConverter.GetBytes(newOffset), 0, data, newSlotPos, 4);
        Array.Copy(BitConverter.GetBytes(recordLen), 0, data, newSlotPos + 4, 4);

        // Increment slot count
        SetSlotCount(data, (ushort)(slotCount + 1));
        return true;
    }

    public static string? GetRecord(byte[] data, string key)
    {
        ushort slotCount = GetSlotCount(data);

        for (int i = 0; i < slotCount; i++)
        {
            int slotPos = HEADER_SIZE + (i * SLOT_SIZE);
            int offset = BitConverter.ToInt32(data, slotPos);
            int len = BitConverter.ToInt32(data, slotPos + 4);

            if (len <= 0) continue; // Marked deleted

            string recordStr = Encoding.UTF8.GetString(data, offset, len);
            var parts = recordStr.Split('=', 2);
            if (parts.Length == 2 && parts[0] == key)
            {
                return parts[1];
            }
        }

        return null;
    }

    public static bool DeleteRecord(byte[] data, string key)
    {
        ushort slotCount = GetSlotCount(data);

        for (int i = 0; i < slotCount; i++)
        {
            int slotPos = HEADER_SIZE + (i * SLOT_SIZE);
            int offset = BitConverter.ToInt32(data, slotPos);
            int len = BitConverter.ToInt32(data, slotPos + 4);

            if (len <= 0) continue;

            string recordStr = Encoding.UTF8.GetString(data, offset, len);
            var parts = recordStr.Split('=', 2);
            if (parts.Length == 2 && parts[0] == key)
            {
                // Mark slot as deleted (Length = 0)
                Array.Copy(BitConverter.GetBytes(0), 0, data, slotPos + 4, 4);
                return true;
            }
        }

        return false;
    }
}