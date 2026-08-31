using MiniDb.Engine.Phase3PageStorage;

namespace MiniDb.Engine.Phase4Indexing;

public class BPlusTree
{
    private readonly BufferPoolManager _bufferPool;
    private readonly int _maxKeys;
    public int RootPageId { get; private set; }
    private int _nextPageId;

    public BPlusTree(BufferPoolManager bufferPool, int rootPageId = 0, int maxKeys = 3)
    {
        _bufferPool = bufferPool;
        _maxKeys = maxKeys;
        RootPageId = rootPageId;

        Page rootPage = _bufferPool.FetchPage(RootPageId);
        ushort keyCount = BitConverter.ToUInt16(rootPage.Data, 1);

        // Clear or initialize root page if empty
        if (keyCount == 0 && rootPage.Data[0] == 0)
        {
            var initialRoot = new BPlusTreeNode(RootPageId, isLeaf: true);
            BPlusTreeNodeSerializer.Serialize(initialRoot, rootPage);
            _bufferPool.UnpinPage(RootPageId, isDirty: true);
            _nextPageId = 1;
        }
        else
        {
            _bufferPool.UnpinPage(RootPageId, isDirty: false);
            _nextPageId = GetMaxPageIdFromDisk() + 1;
        }
    }

    public string? Search(string key)
    {
        var leaf = FindLeafNode(RootPageId, key);
        return leaf.Values.TryGetValue(key, out var val) ? val : null;
    }

    /// <summary>
    /// Inserts a new key-value pair.
    /// Throws InvalidOperationException if key already exists (Primary Key Violation).
    /// </summary>
    public void Insert(string key, string value)
    {
        var leaf = FindLeafNode(RootPageId, key);

        // DUPLICATE PRIMARY KEY CHECK
        if (leaf.Keys.Contains(key))
        {
            throw new InvalidOperationException($"Primary key violation: Duplicate key '{key}' already exists in table.");
        }

        leaf.Keys.Add(key);
        leaf.Keys.Sort();
        leaf.Values[key] = value;

        SaveNode(leaf);

        if (leaf.Keys.Count > _maxKeys)
        {
            SplitLeafNode(leaf);
        }
    }

    /// <summary>
    /// Updates an existing key's value in the B+ Tree.
    /// Returns false if key is not found.
    /// </summary>
    public bool Update(string key, string newValue)
    {
        var leaf = FindLeafNode(RootPageId, key);
        if (!leaf.Keys.Contains(key)) return false;

        leaf.Values[key] = newValue;
        SaveNode(leaf);
        return true;
    }

    public bool Delete(string key)
    {
        var leaf = FindLeafNode(RootPageId, key);
        if (!leaf.Keys.Contains(key)) return false;

        leaf.Keys.Remove(key);
        leaf.Values.Remove(key);
        SaveNode(leaf);
        return true;
    }

    public Dictionary<string, string> GetAllKeys()
    {
        var result = new Dictionary<string, string>();
        int currentLeafId = GetLeftmostLeafId(RootPageId);

        while (currentLeafId != -1)
        {
            Page page = _bufferPool.FetchPage(currentLeafId);
            var leaf = BPlusTreeNodeSerializer.Deserialize(page);
            _bufferPool.UnpinPage(currentLeafId, isDirty: false);

            foreach (var kvp in leaf.Values)
            {
                result[kvp.Key] = kvp.Value;
            }

            currentLeafId = leaf.NextPageId;
        }

        return result;
    }

    private int GetLeftmostLeafId(int pageId)
    {
        Page page = _bufferPool.FetchPage(pageId);
        var node = BPlusTreeNodeSerializer.Deserialize(page);
        _bufferPool.UnpinPage(pageId, isDirty: false);

        if (node.IsLeaf) return node.PageId;
        return GetLeftmostLeafId(node.ChildrenPageIds[0]);
    }

    private BPlusTreeNode FindLeafNode(int pageId, string key)
    {
        Page page = _bufferPool.FetchPage(pageId);
        var node = BPlusTreeNodeSerializer.Deserialize(page);
        _bufferPool.UnpinPage(pageId, isDirty: false);

        if (node.IsLeaf) return node;

        int i = 0;
        while (i < node.Keys.Count && string.Compare(key, node.Keys[i]) >= 0)
        {
            i++;
        }
        return FindLeafNode(node.ChildrenPageIds[i], key);
    }

    private void SplitLeafNode(BPlusTreeNode leaf)
    {
        var newLeaf = new BPlusTreeNode(_nextPageId++, isLeaf: true);
        int mid = leaf.Keys.Count / 2;

        var rightKeys = leaf.Keys.Skip(mid).ToList();
        foreach (var k in rightKeys)
        {
            newLeaf.Keys.Add(k);
            newLeaf.Values[k] = leaf.Values[k];

            leaf.Keys.Remove(k);
            leaf.Values.Remove(k);
        }

        newLeaf.NextPageId = leaf.NextPageId;
        leaf.NextPageId = newLeaf.PageId; // Linked list next node updated

        SaveNode(leaf);
        SaveNode(newLeaf);

        if (leaf.PageId == RootPageId)
        {
            var newRoot = new BPlusTreeNode(_nextPageId++, isLeaf: false);
            newRoot.Keys.Add(newLeaf.Keys[0]);
            newRoot.ChildrenPageIds.Add(leaf.PageId);
            newRoot.ChildrenPageIds.Add(newLeaf.PageId);

            RootPageId = newRoot.PageId;
            SaveNode(newRoot);
        }
    }

    private void SaveNode(BPlusTreeNode node)
    {
        Page page = _bufferPool.FetchPage(node.PageId);
        BPlusTreeNodeSerializer.Serialize(node, page);
        _bufferPool.UnpinPage(node.PageId, isDirty: true);
    }

    private int GetMaxPageIdFromDisk()
    {
        return _nextPageId > 0 ? _nextPageId : 10;
    }
}
