namespace IO.Milvus.Workbench.Models
{
    public enum NodeState
    {
        None,
        Success,
        Connecting,
        Error,
        Closed,
    }

    public static class NodeStateUtils
    {
        public static bool CanConnect(this NodeState nodeState)
        {
            return nodeState != NodeState.Connecting;
        }
    }
}
