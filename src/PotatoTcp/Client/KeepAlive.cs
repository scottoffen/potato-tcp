using System;

namespace PotatoTcp.Client
{
    [Serializable]
    public class KeepAlive
    {
        public string Message { get; set; } = "Ping";

        public Guid Id { get; set; }
    }
}