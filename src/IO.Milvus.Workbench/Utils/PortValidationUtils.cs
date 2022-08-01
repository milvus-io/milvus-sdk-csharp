namespace IO.Milvus.Workbench.Utils
{
    public static class PortValidationUtils
    {
        public static bool PortInRange(int port)
        {
            return port >= 0 && port <= 65535;
        }
    }
}
