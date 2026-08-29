using System.Text;
using MiniDb.Engine.Phase3PageStorage;

namespace MiniDb.Engine.Phase4Indexing;

public static class BPlusTreeNodeSerializer
{
    // Page Layout:
    // [0..0]   : IsLeaf (1 byte: 1 for Leaf, 0 for Internal)
    // [1..2]   : KeyCount (2 bytes)
    // [3..6]   : NextPageId (4 bytes)
    // [7.....] : Dynamic Keys, Values, or Child Page IDs

    public static BPlusTreeNode Deserialize(Page page)
    {
        byte[] data = page.Data;
        bool isLeaf = data[0] == 1;
        ushort keyCount = BitConverter.ToUInt16(data, 1);
        int nextPageId = BitConverter.ToInt32(data, 3);

        var node = new BPlusTreeNode(page.PageId, isLeaf)
        {
            NextPageId = nextPageId
        };

        int offset = 7;
        for (int i = 0; i < keyCount; i++)
        {
            ushort keyLen = BitConverter.ToUInt16(data, offset);
            offset += 2;
            string key = Encoding.UTF8.GetString(data, offset, keyLen);
            offset += keyLen;

            node.Keys.Add(key);

            if (isLeaf)
            {
                ushort valLen = BitConverter.ToUInt16(data, offset);
                offset += 2;
                string value = Encoding.UTF8.GetString(data, offset, valLen);
                offset += valLen;

                node.Values[key] = value;
            }
        }

        if (!isLeaf)
        {
            for (int i = 0; i <= keyCount; i++)
            {
                int childId = BitConverter.ToInt32(data, offset);
                offset += 4;
                node.ChildrenPageIds.Add(childId);
            }
        }

        return node;
    }

    public static void Serialize(BPlusTreeNode node, Page page)
    {
        byte[] data = page.Data;
        Array.Clear(data, 0, data.Length);

        data[0] = (byte)(node.IsLeaf ? 1 : 0);
        Array.Copy(BitConverter.GetBytes((ushort)node.Keys.Count), 0, data, 1, 2);
        Array.Copy(BitConverter.GetBytes(node.NextPageId), 0, data, 3, 4);

        int offset = 7;
        foreach (var key in node.Keys)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            Array.Copy(BitConverter.GetBytes((ushort)keyBytes.Length), 0, data, offset, 2);
            offset += 2;
            Array.Copy(keyBytes, 0, data, offset, keyBytes.Length);
            offset += keyBytes.Length;

            if (node.IsLeaf)
            {
                string val = node.Values[key];
                byte[] valBytes = Encoding.UTF8.GetBytes(val);
                Array.Copy(BitConverter.GetBytes((ushort)valBytes.Length), 0, data, offset, 2);
                offset += 2;
                Array.Copy(valBytes, 0, data, offset, valBytes.Length);
                offset += valBytes.Length;
            }
        }

        if (!node.IsLeaf)
        {
            foreach (var childId in node.ChildrenPageIds)
            {
                Array.Copy(BitConverter.GetBytes(childId), 0, data, offset, 4);
                offset += 4;
            }
        }
    }
}
