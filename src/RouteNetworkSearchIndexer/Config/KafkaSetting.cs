namespace RouteNetworkSearchIndexer.Config
{
    internal class KafkaSetting
    {
        public string Consumer { get; set; }
        public string Server { get; set; }
        public string PositionConnectionString { get; set; }
        public string Topic { get; set; }
    }
}
