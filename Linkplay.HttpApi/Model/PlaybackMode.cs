namespace Linkplay.HttpApi.Model;

public enum PlaybackMode
{
    Idling = 0, // Idling
    AirplayStreaming = 1, // airplay streaming
    DLNAStreaming = 2, // DLNA streaming
    Network = 10, // Playing network content, e.g. vTuner, Home Media Share, Amazon Music, Deezer, etc.
    USB = 11, // playing UDISK(Local USB disk on Arylic Device)
    HTTPAPI = 20, // playback start by HTTPAPI
    SpotifyStreaming = 31, // Spotify Connect streaming
    LineIn = 40, // Line-In input mode
    Bluetooth = 41, // Bluetooth input mode
    Optical = 43, // Optical input mode
    LineIn2 = 47, // Line-In #2 input mode
    USBDAC = 51, // USBDAC input mode
    Guest = 99, // The Device is a Guest in a Multiroom Zone
}
