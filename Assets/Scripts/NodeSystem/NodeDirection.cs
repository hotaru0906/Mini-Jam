public enum NodeDirection
{
    North,
    South,
    East,
    West
}

public static class NodeDirectionExtensions
{
    // Trả về hướng ngược lại (dùng khi tự động link 2 chiều)
    public static NodeDirection Opposite(this NodeDirection dir)
    {
        switch (dir)
        {
            case NodeDirection.North: return NodeDirection.South;
            case NodeDirection.South: return NodeDirection.North;
            case NodeDirection.East:  return NodeDirection.West;
            case NodeDirection.West:  return NodeDirection.East;
            default:                  return NodeDirection.North;
        }
    }
}
