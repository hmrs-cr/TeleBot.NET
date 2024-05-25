﻿namespace Linkplay.HttpApi.Model;

public class DeviceStatus
{
    public string Uuid { get; set; }
    public string DeviceName { get; set; }
    public string GroupName { get; set; }
    public string Ssid { get; set; }
    public string Language { get; set; }
    public string Firmware { get; set; }
    public string Hardware { get; set; }
    public string Build { get; set; }
    public string Project { get; set; }
    public string PrivPrj { get; set; }
    public string ProjectBuildName { get; set; }
    public string Release { get; set; }
    public string TempUuid { get; set; }
    public bool HideSSID { get; set; }
    public string SSIDStrategy { get; set; }
    public string Branch { get; set; }
    public int Group { get; set; }
    public string WmrmVersion { get; set; }
    public bool Internet { get; set; }
    public string MAC { get; set; }
    public string STAMAC { get; set; }
    public string CountryCode { get; set; }
    public string CountryRegion { get; set; }
    public int Netstat { get; set; }
    public HexedString Essid { get; set; }
    public string Apcli0 { get; set; }
    public string Eth2 { get; set; }
    public string Ra0 { get; set; }
    public bool EthDhcp { get; set; }
    public string EthStaticIp { get; set; }
    public string EthStaticMask { get; set; }
    public string EthStaticGateway { get; set; }
    public string EthStaticDns1 { get; set; }
    public string EthStaticDns2 { get; set; }
    public bool VersionUpdate { get; set; }
    public string NewVer { get; set; }
    public string McuVer { get; set; }
    public string McuVerNew { get; set; }
    public string DspVer { get; set; }
    public string DspVerNew { get; set; }
    public string Date { get; set; }
    public string Time { get; set; }
    public string Tz { get; set; }
    public string DstEnable { get; set; }
    public string Region { get; set; }
    public bool PromptStatus { get; set; }
    public string IotVer { get; set; }
    public string UpnpVersion { get; set; }
    public string Cap1 { get; set; }
    public string Capability { get; set; }
    public string Languages { get; set; }
    public string StreamsAll { get; set; }
    public string Streams { get; set; }
    public string External { get; set; }
    public string PlmSupport { get; set; }
    public string PresetKey { get; set; }
    public bool SpotifyActive { get; set; }
    public string LbcSupport { get; set; }
    public string PrivacyMode { get; set; }
    public string WifiChannel { get; set; }
    public string RSSI { get; set; }
    public string BSSID { get; set; }
    public bool Battery { get; set; }
    public string BatteryPercent { get; set; }
    public string Securemode { get; set; }
    public string Auth { get; set; }
    public string Encry { get; set; }
    public string UpnpUuid { get; set; }
    public string UartPassPort { get; set; }
    public string CommunicationPort { get; set; }
    public string WebFirmwareUpdateHide { get; set; }
    public string IgnoreTalkstart { get; set; }
    public string WebLoginResult { get; set; }
    public string SilenceOTATime { get; set; }
    public string IgnoreSilenceOTATime { get; set; }
    public string NewTuneinPresetAndAlarm { get; set; }
    public string IheartradioNew { get; set; }
    public string NewIheartPodcast { get; set; }
    public string TidalVersion { get; set; }
    public string ServiceVersion { get; set; }
    public string Security { get; set; }
    public string SecurityVersion { get; set; }
}
