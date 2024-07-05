namespace Omada.OpenApi.Client.Responses;

public record ClientStats
{
    public int Total { get; init; }
    public int Wireless { get; init; }
    public int Wired { get; init; }
    public int Num2g { get; init; }
    public int Num5g { get; init; }
    public int Num6g { get; init; }
    public int NumUser { get; init; }
    public int NumGuest { get; init; }
    public int NumWirelessUser { get; init; }
    public int NumWirelessGuest { get; init; }
    public int Num2gUser { get; init; }
    public int Num5gUser { get; init; }
    public int Num6gUser { get; init; }
    public int Num2gGuest { get; init; }
    public int Num5gGuest { get; init; }
    public int Num6gGuest { get; init; }
    public int Poor { get; init; }
    public int Fair { get; init; }
    public int NoData { get; init; }
    public int Good { get; init; }
}