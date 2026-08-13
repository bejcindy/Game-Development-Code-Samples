using Steamworks;

public class SteamworksConstants
{
    public readonly CSteamID k_AppId_brokencigs = new CSteamID(3553350);
    
    // Matchmaking Server Test
    public readonly SteamIPAddress_t k_IpAddress208_78_165_233 = new SteamIPAddress_t(System.Net.IPAddress.Parse("208.78.165.233")); 
    public const uint k_IpAddress208_78_165_233_uint = 3494815209;
    public const ushort k_Port27015 = 27015;
    
    private static SteamworksConstants _instance;

    private SteamworksConstants() { }

    public static SteamworksConstants Instance {
        get {
            if (_instance == null) {
                _instance = new SteamworksConstants();
            }
            return _instance;
        }
    }
}