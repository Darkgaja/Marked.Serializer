namespace Marked.Serializer
{
    public enum BinaryDataToken : byte
    {
        None = 0,
        NodeStart = 1,
        NodeEnd = 2,
        Content = 3,
        ArrayLength = 4,
        Type = 5,
        Id = 6,
        RefId = 7
    }
}