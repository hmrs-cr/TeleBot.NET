namespace TeleBotService.Model;

public record SpeedtestResult
{
    public string? Type { get; set; }
    public DateTime? Timestamp { get; set; }
    public PingResult? Ping { get; set; }
    public SpeedResult? Download { get; set; }
    public SpeedResult? Upload { get; set; }
    public double PacketLoss { get; set; }
    public string? Isp { get; set; }
    public InterfaceData? Interface { get; set; }
    public ServerData? ServerResult { get; set; }
    public ResultData? Result { get; set; }

    public record InterfaceData
    {
        public string? InternalIp { get; set; }
        public string? Name { get; set; }
        public string? MacAddr { get; set; }
        public bool IsVpn { get; set; }
        public string? ExternalIp { get; set; }
    }

    public record LatencyResult
    {
        public double Iqm { get; set; }
        public double Low { get; set; }
        public double High { get; set; }
        public double Jitter { get; set; }
    }

    public record PingResult
    {
        public double Jitter { get; set; }
        public double Latency { get; set; }
        public double Low { get; set; }
        public double High { get; set; }
    }

    public record ResultData
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public bool Persisted { get; set; }
    }

    public record ServerData
    {
        public int Id { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? Country { get; set; }
        public string? Ip { get; set; }
    }

    public record SpeedResult
    {
        public int Bandwidth { get; set; }
        public int Bytes { get; set; }
        public int Elapsed { get; set; }
        public LatencyResult? Latency { get; set; }
    }
}