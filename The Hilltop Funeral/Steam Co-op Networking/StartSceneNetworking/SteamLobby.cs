using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SteamLobby : MonoBehaviour
{
    public static CSteamID currentLobbyID = CSteamID.Nil;
    // [SerializeField] 
    THF_NetworkManager networkManager;
    StartSceneCanvas startSceneCanvas;
    GameObject disconnectCanvas;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyInvite_t> lobbyInvite;
    protected Callback<LobbyKicked_t> lobbyKicked;
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;

    private const string HostAddressKey = "HostAddress";

    void Start()
    {
        networkManager = GetComponent<THF_NetworkManager>();
        if (!SteamManager.Initialized) { return; }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyInvite = Callback<LobbyInvite_t>.Create(OnLobbyInvite);
        lobbyKicked = Callback<LobbyKicked_t>.Create(OnLobbyKicked);
        //lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        // startSceneCanvas = FindObjectOfType<StartSceneCanvas>();
        disconnectCanvas = transform.GetChild(0).gameObject;

    }

    private void OnDestroy()
    {

    }

    void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create lobby: " + callback.m_eResult);
            return;
        }

        Debug.Log("Lobby created successfully with ID: " + callback.m_ulSteamIDLobby);
        networkManager.StartHost();
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(callback.m_ulSteamIDLobby));
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
    }

    void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        //called when player clicks an invite from Steam
        //move camera to target location
        Debug.Log("Game lobby join requested for lobby: " + callback.m_steamIDLobby);
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    void OnLobbyInvite(LobbyInvite_t callback)
    {
        //Turn off disconnect canvas
        if (disconnectCanvas.activeSelf)
            disconnectCanvas.SetActive(false);
        // Show invite popup
        string invitorName = SteamFriends.GetFriendPersonaName(new CSteamID(callback.m_ulSteamIDUser));
        EventsMaster.Event_OnReceiveInvite(invitorName, new CSteamID(callback.m_ulSteamIDLobby));
        Debug.Log("Lobby invite received from: " + invitorName + " to lobby: " + callback.m_ulSteamIDLobby);
    }

    void OnLobbyEntered(LobbyEnter_t callback)
    {
        CSteamID newLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // If we're already in a lobby (as host or client), we need to leave first
        if (NetworkServer.active || NetworkClient.active)
        {
            // If joining the same lobby we're already in, ignore
            if (currentLobbyID == newLobbyID)
            {
                Debug.Log("Already in this lobby, ignoring OnLobbyEntered");
                return;
            }

            // Leave current lobby/connection before joining new one
            Debug.Log("Already in a lobby, leaving current connection to join new lobby");
            networkManager.StopHost(); // This will stop server AND client if host, or just client if client-only
            CleanupNetworkReferences();

            // Leave Steam lobby
            if (currentLobbyID != CSteamID.Nil)
            {
                SteamMatchmaking.LeaveLobby(currentLobbyID);
            }
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(newLobbyID, HostAddressKey);
        networkManager.networkAddress = hostAddress;

        if (callback.m_EChatRoomEnterResponse != 1)
        {
            Debug.LogError("Failed to enter lobby: " + callback.m_EChatRoomEnterResponse);
            CleanupNetworkReferences();
            return;
        }

        EventsMaster.Event_OnAcceptInvite();
        Debug.Log("Successfully entered lobby with ID: " + callback.m_ulSteamIDLobby);
        currentLobbyID = newLobbyID;
        StartSceneCameraController.Instance.TurnOnCamera(4, () =>
        {
            networkManager.StartClient();
        });

    }

    void OnLobbyKicked(LobbyKicked_t callback)
    {
        Debug.Log("Kicked from lobby: " + callback.m_ulSteamIDLobby);

        // Clean up network state before resetting lobby ID
        networkManager.ResetConnection();

        SceneManager.LoadScene(0);
        LoadingManager.ShowLoadingScreen();
        disconnectCanvas.SetActive(true);
    }

    private void CleanupNetworkReferences()
    {
        Debug.Log("Cleaning up network references and destroying player objects");

        if (networkManager != null)
        {
            // Destroy room player objects first (they have DontDestroyOnLoad)
            if (networkManager.roomPlayerOne != null)
            {
                Debug.Log("Destroying roomPlayerOne GameObject");
                Destroy(networkManager.roomPlayerOne.gameObject);
            }

            if (networkManager.roomPlayerTwo != null)
            {
                Debug.Log("Destroying roomPlayerTwo GameObject");
                Destroy(networkManager.roomPlayerTwo.gameObject);
            }

            // Destroy game player objects (they may also persist)
            if (networkManager.networkPlayerOne != null)
            {
                Debug.Log("Destroying networkPlayerOne GameObject");
                Destroy(networkManager.networkPlayerOne.gameObject);
            }

            if (networkManager.networkPlayerTwo != null)
            {
                Debug.Log("Destroying networkPlayerTwo GameObject");
                Destroy(networkManager.networkPlayerTwo.gameObject);
            }

            Debug.Log("Clearing network manager player references");
            networkManager.roomPlayerOne = null;
            networkManager.roomPlayerTwo = null;
            networkManager.networkPlayerOne = null;
            networkManager.networkPlayerTwo = null;
            networkManager.readyPlayers = 0;
            Debug.Log("Network references cleanup and object destruction completed");
        }
        else
        {
            Debug.LogWarning("NetworkManager reference is null - cannot clean up references");
        }
    }
    public void CreateSteamLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    public void OpenSteamOverlay()
    {
        if (SteamManager.Initialized)
        {
            SteamFriends.ActivateGameOverlay("Friends"); // Opens the Friends overlay
        }
    }

    int GetLobbyMemberCount()
    {
        if (currentLobbyID == CSteamID.Nil) return 0;
        return SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
    }

    public ulong GetOtherPlayerSteamID()
    {
        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogWarning("Not in a lobby");
            return 0;
        }

        int numMembers = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
        CSteamID mySteamID = SteamUser.GetSteamID();

        for (int i = 0; i < numMembers; i++)
        {
            CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);

            // Return the member that isn't us
            if (memberID != mySteamID)
            {
                return memberID.m_SteamID;
            }
        }

        Debug.LogWarning("No other player found in lobby");
        return 0;
    }
}


