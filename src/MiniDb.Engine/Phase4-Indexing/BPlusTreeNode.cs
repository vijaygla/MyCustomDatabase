namespace MiniDb.Engine.Phase4Indexing;

public class BPlusTreeNode
{
    public bool IsLeaf { get; set; }
    public List<string> Keys { get; set; } = new();

    // Internal Nodes ke liye: Child Page IDs
    public List<int> ChildrenPageIds { get; set; } = new();

    // Leaf Nodes ke liye: Key-Value storage (या Target Page Offset)
    public Dictionary<string, string> Values { get; set; } = new();

    // Leaf Nodes linking ke liye (Range Queries Support)
    public int NextPageId { get; set; } = -1;
    public int PageId { get; set; }

    public BPlusTreeNode(int pageId, bool isLeaf)
    {
        PageId = pageId;
        IsLeaf = isLeaf;
    }
}
