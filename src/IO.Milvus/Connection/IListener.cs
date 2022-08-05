namespace IO.Milvus.Connection
{
    public interface IListener
    {
        bool HeartBeat(ServerSetting serverSetting);
    }
}
