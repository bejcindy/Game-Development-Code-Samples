using Cinemachine;
using DG.Tweening;
using Mirror;
using NUnit.Framework;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using THP.Network;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.SceneManagement;

public class THF_NetworkManager : NetworkManager
{
    [SerializeField] private int minPlayer = 2;
    [Scene][SerializeField] private string menuScene = "StartScene";

    [Header("Room")]
    [SerializeField] private Network_LobbyPlayer roomPlayerPrefab = null;

    [Header("Game")]
    [SerializeField] private PlayerMovement gamePlayerOnePrefab = null;
    [SerializeField] private PlayerMovement gamePlayerTwoPrefab = null;

    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;
    public static event Action<NetworkConnection> OnServerReadied;
    public static event Action OnMultiplayerSceneChanged;

    [Header("Room Players")]
    public Network_LobbyPlayer roomPlayerOne;
    public Network_LobbyPlayer roomPlayerTwo;

    [Header("Game Players")]
    public PlayerMovement networkPlayerOne;
    public PlayerMovement networkPlayerTwo;

    [Header("Player States")]
    public int readyPlayers = 0;

    [Header("UI")]
    [SerializeField] private GameObject disconnectCanvas;
    public StartSceneCanvas startSceneCanvas;

    // Flag to prevent duplicate cleanup when disconnecting manually vs network error
    public bool isManualDisconnect = false;
    private bool disconnecting = false;

    void OnEnable()
    {
        if (DemoManager.Instance != null && DemoManager.Instance.isLocalDemo)
        {
            gameObject.SetActive(false);
            return;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
        {
            startSceneCanvas = FindObjectOfType<StartSceneCanvas>();
        }
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        OnClientConnected?.Invoke();
        // NetworkClient.RegisterPrefab
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        OnClientDisconnected?.Invoke();

        // Only handle disconnect cleanup if it wasn't a manual disconnect (ExitHouse)
        if (!isManualDisconnect)
        {
            // Check if we're in start scene - if so, only do minimal cleanup for unexpected disconnects
            bool isInStartScene = SceneManager.GetActiveScene().name == System.IO.Path.GetFileNameWithoutExtension(menuScene);

            if (isInStartScene)
            {
                // In start scene - check if this was a client that should become the new host
                if (!NetworkServer.active) // This is a client, not the host
                {
                    Debug.Log("Client disconnected unexpectedly in start scene - attempting to become new host");
                    HandleClientBecomeHost();
                }
            }
            else
            {
                // In game scene - full disconnect handling
                HandleDisconnection(true, "Client disconnected unexpectedly");
            }
        }

    }

    /// <summary>
    /// Handles the case where a client needs to become the new host after the original host disconnects
    /// </summary>
    private void HandleClientBecomeHost()
    {
        Debug.Log("Client becoming new host after original host disconnected");

        // Clean up the current client connection
        StopClient();
        NetworkClient.Shutdown();

        // Clear any existing player references from the old session
        DestroyAllPlayerObjectsAndClearReferences();

        // Leave the old Steam lobby
        if (SteamLobby.currentLobbyID != CSteamID.Nil)
        {
            Debug.Log($"Leaving old Steam lobby: {SteamLobby.currentLobbyID}");
            SteamMatchmaking.LeaveLobby(SteamLobby.currentLobbyID);
            SteamLobby.currentLobbyID = CSteamID.Nil;
        }

        // Create a new Steam lobby and become the host
        SteamLobby steamLobby = FindObjectOfType<SteamLobby>();
        if (steamLobby != null)
        {
            Debug.Log("Creating new Steam lobby as the new host");
            steamLobby.CreateSteamLobby();
        }
        else
        {
            Debug.LogError("SteamLobby component not found - cannot create new lobby");
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Handle lobby state cleanup first
        if (conn.identity != null)
        {
            Network_LobbyPlayer roomPlayer = conn.identity.GetComponent<Network_LobbyPlayer>();
            if (roomPlayer != null)
            {
                Debug.Log($"Player {roomPlayer.playerNumber} disconnecting, resetting lobby state");

                // Reset the disconnecting player's ready state before clearing reference
                roomPlayer.isReady = false;

                if (roomPlayerOne == roomPlayer)
                {
                    roomPlayerOne = null;
                }
                else if (roomPlayerTwo == roomPlayer)
                {
                    roomPlayerTwo = null;
                }

                // Reset the ready players count
                readyPlayers = 0;

                // Notify remaining players of the new state
                NotifyPlayersOfReadyState();
            }
        }

        base.OnServerDisconnect(conn);

        // Only handle disconnect cleanup if it wasn't a manual disconnect (ExitHouse)
        if (!isManualDisconnect)
        {
            // Check if we're in start scene
            bool isInStartScene = SceneManager.GetActiveScene().name == System.IO.Path.GetFileNameWithoutExtension(menuScene);

            if (isInStartScene)
            {
                // In start scene - server stays, only clean up the disconnected client's stuff
                Debug.Log($"Client {conn.connectionId} disconnected from start scene - server preserving own state");
                // No scene change, no panel, minimal cleanup already handled in lobby state cleanup above
            }
            else
            {
                // In game scene - full disconnect handling with scene change and panel
                HandleDisconnection(true, $"Player {conn.connectionId} disconnected unexpectedly");
            }
        }

        // Reset the flag for next time
        isManualDisconnect = false;
    }

    /// <summary>
    /// Centralized disconnection handling that avoids duplicate logic
    /// </summary>
    /// <param name="showDisconnectCanvas">Whether to show the disconnect canvas UI</param>
    /// <param name="reason">Reason for disconnection (for logging)</param>
    private void HandleDisconnection(bool showDisconnectCanvas, string reason)
    {
        if (disconnecting)
        {
            Debug.Log("Already handling disconnection, skipping duplicate call");
            return;
        }
        Debug.Log($"Handling disconnection: {reason}");

        DOTween.KillAll();

        disconnecting = true;
        GameLogic.Instance.currentFriendID = 0;
        // Return to start scene if not already there
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            LoadingManager.ShowLoadingScreen();
            SceneManager.LoadScene(0);
        }

        // Show disconnect canvas if requested (for unexpected disconnects)
        if (showDisconnectCanvas && disconnectCanvas != null)
        {
            disconnectCanvas.SetActive(true);
        }

        // Ensure all player objects and references are cleaned up
        DestroyAllPlayerObjectsAndClearReferences();

        // Clear current network connections
        if (NetworkServer.active)
        {
            StopHost();
        }
        else if (NetworkClient.active)
        {
            StopClient();
        }

        // Ensure network components are properly shutdown
        NetworkServer.Shutdown();
        NetworkClient.Shutdown();

        // Leave Steam lobby if connected
        if (SteamLobby.currentLobbyID != CSteamID.Nil)
        {
            Debug.Log($"Leaving Steam lobby: {SteamLobby.currentLobbyID}");
            SteamMatchmaking.LeaveLobby(SteamLobby.currentLobbyID);
            SteamLobby.currentLobbyID = CSteamID.Nil;
        }
        disconnecting = false;
    }

    /// <summary>
    /// Comprehensive cleanup method that destroys all player objects and clears references
    /// </summary>
    private void DestroyAllPlayerObjectsAndClearReferences()
    {
        Debug.Log("Starting comprehensive player cleanup - destroying objects and clearing references");

        // Destroy room player objects
        if (roomPlayerOne != null)
        {
            Debug.Log($"Destroying roomPlayerOne: {roomPlayerOne.name}");
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(roomPlayerOne.gameObject);
            }
            else
            {
                Destroy(roomPlayerOne.gameObject);
            }
        }

        if (roomPlayerTwo != null)
        {
            Debug.Log($"Destroying roomPlayerTwo: {roomPlayerTwo.name}");
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(roomPlayerTwo.gameObject);
            }
            else
            {
                Destroy(roomPlayerTwo.gameObject);
            }
        }

        // Destroy game player objects
        if (networkPlayerOne != null)
        {
            Debug.Log($"Destroying networkPlayerOne: {networkPlayerOne.name}");
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(networkPlayerOne.gameObject);
            }
            else
            {
                Destroy(networkPlayerOne.gameObject);
            }
        }

        if (networkPlayerTwo != null)
        {
            Debug.Log($"Destroying networkPlayerTwo: {networkPlayerTwo.name}");
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(networkPlayerTwo.gameObject);
            }
            else
            {
                Destroy(networkPlayerTwo.gameObject);
            }
        }

        // Clear all references
        Debug.Log("Clearing all network manager player references");
        roomPlayerOne = null;
        roomPlayerTwo = null;
        networkPlayerOne = null;
        networkPlayerTwo = null;
        readyPlayers = 0;

        Debug.Log("Player cleanup completed - all objects destroyed and references cleared");
    }

    /// <summary>
    /// Selective cleanup method that preserves the host's own room player when manually exiting
    /// </summary>
    private void DestroyClientPlayersOnly()
    {
        Debug.Log("Starting selective client player cleanup - preserving host's room player");

        // Find the host's own room player (the one that has authority/is local player)
        Network_LobbyPlayer hostRoomPlayer = null;
        if (roomPlayerOne != null && roomPlayerOne.isLocalPlayer)
        {
            hostRoomPlayer = roomPlayerOne;
            Debug.Log($"Host room player identified: {roomPlayerOne.name} (Player 1)");
        }
        else if (roomPlayerTwo != null && roomPlayerTwo.isLocalPlayer)
        {
            hostRoomPlayer = roomPlayerTwo;
            Debug.Log($"Host room player identified: {roomPlayerTwo.name} (Player 2)");
        }

        // Destroy non-host room player objects
        if (roomPlayerOne != null && roomPlayerOne != hostRoomPlayer)
        {
            Debug.Log($"Destroying client roomPlayerOne: {roomPlayerOne.name}");
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(roomPlayerOne.gameObject);
            }
            else
            {
                Destroy(roomPlayerOne.gameObject);
            }
            roomPlayerOne = null;
        }

        if (roomPlayerTwo != null && roomPlayerTwo != hostRoomPlayer)
        {
            Debug.Log($"Destroying client roomPlayerTwo: {roomPlayerTwo.name}");
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(roomPlayerTwo.gameObject);
            }
            else
            {
                Destroy(roomPlayerTwo.gameObject);
            }
            roomPlayerTwo = null;
        }

        // Destroy all game player objects (these should always be destroyed when returning to lobby)
        if (networkPlayerOne != null)
        {
            Debug.Log($"Destroying networkPlayerOne: {networkPlayerOne.name}");
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(networkPlayerOne.gameObject);
            }
            else
            {
                Destroy(networkPlayerOne.gameObject);
            }
            networkPlayerOne = null;
        }

        if (networkPlayerTwo != null)
        {
            Debug.Log($"Destroying networkPlayerTwo: {networkPlayerTwo.name}");
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(networkPlayerTwo.gameObject);
            }
            else
            {
                Destroy(networkPlayerTwo.gameObject);
            }
            networkPlayerTwo = null;
        }

        // Reset ready players count but keep the host's room player reference
        readyPlayers = 0;
        if (hostRoomPlayer != null)
        {
            hostRoomPlayer.isReady = false; // Reset host's ready state
            Debug.Log($"Preserved host room player: {hostRoomPlayer.name}, reset ready state");
        }

        Debug.Log("Selective client cleanup completed - host's room player preserved");
    }

    /// <summary>
    /// Manual disconnect cleanup for when someone manually exits from start scene
    /// Only cleans up their own stuff, no scene changes or panels
    /// </summary>
    /// <param name="isHost">Whether the person leaving is the host</param>
    public void ManualExitFromStartScene(bool isHost = false)
    {
        Debug.Log($"Manual exit from start scene - isHost: {isHost}");

        // Set flag to prevent duplicate cleanup in OnClientDisconnect/OnServerDisconnect
        isManualDisconnect = true;

        if (isHost)
        {
            // Host is leaving - disconnect clients but don't do full reset
            DisconnectAllClients();

            // Give clients a moment to receive the disconnect message, then do minimal cleanup
            StartCoroutine(DelayedManualExitCleanup());
        }
        else
        {
            // Client is leaving - just disconnect client, server will handle cleanup in OnServerDisconnect
            StopClient();
            NetworkClient.Shutdown();

            // Revert to singleplayer mode for the leaving client
            EventsMaster.Event_OnGameModeChange(GameMode.Singleplayer);
        }
    }

    /// <summary>
    /// Coroutine for delayed cleanup after manual exit from start scene
    /// </summary>
    /// <summary>
    /// Coroutine for delayed cleanup after manual exit from start scene
    /// </summary>
    private IEnumerator DelayedManualExitCleanup()
    {
        Debug.Log("Waiting for client disconnects to process...");
        yield return new WaitForSeconds(0.1f);

        Debug.Log("Performing complete cleanup after manual exit from start scene");

        // For manual exits, destroy everything and clear all references
        DestroyAllPlayerObjectsAndClearReferences();

        // Leave Steam lobby
        if (SteamLobby.currentLobbyID != CSteamID.Nil)
        {
            Debug.Log($"Leaving Steam lobby: {SteamLobby.currentLobbyID}");
            SteamMatchmaking.LeaveLobby(SteamLobby.currentLobbyID);
            SteamLobby.currentLobbyID = CSteamID.Nil;
        }

        // Stop networking
        StopHost();
        StopClient();
        NetworkServer.Shutdown();
        NetworkClient.Shutdown();

        // Revert to singleplayer mode
        EventsMaster.Event_OnGameModeChange(GameMode.Singleplayer);
    }

    /// <summary>
    /// Disconnect all connected clients when host manually leaves
    /// </summary>
    private void DisconnectAllClients()
    {
        if (!NetworkServer.active)
        {
            Debug.Log("Not server, cannot disconnect clients");
            return;
        }

        Debug.Log("Host manually leaving - disconnecting all clients");

        // Create a copy of the connections dictionary to avoid modification during iteration
        var connectionsToDisconnect = new List<NetworkConnectionToClient>();

        foreach (var kvp in NetworkServer.connections)
        {
            var conn = kvp.Value;
            if (conn != null && conn.isReady)
            {
                connectionsToDisconnect.Add(conn);
            }
        }

        // Disconnect all clients
        foreach (var conn in connectionsToDisconnect)
        {
            Debug.Log($"Disconnecting client connection {conn.connectionId}");
            conn.Disconnect();
        }

        Debug.Log($"Disconnected {connectionsToDisconnect.Count} clients");
    }

    /// <summary>
    /// Unified method for resetting network state - used by both manual exits and disconnections
    /// Call this for manual disconnections (like ExitHouse) to prevent duplicate handling
    /// </summary>
    /// <param name="isManual">True if this is a manual disconnect, false for network errors</param>
    public void ResetConnection(bool isManual = false)
    {
        Debug.Log($"Resetting connection - Manual: {isManual}");

        // Check if we're in start scene and this is a manual disconnect
        bool isInStartScene = SceneManager.GetActiveScene().name == System.IO.Path.GetFileNameWithoutExtension(menuScene);

        if (isManual && isInStartScene)
        {
            // Manual exit from start scene - use special handling
            bool isHost = NetworkServer.active && NetworkClient.active;
            ManualExitFromStartScene(isHost);
            return;
        }

        // Set flag to prevent duplicate cleanup in OnClientDisconnect/OnServerDisconnect
        isManualDisconnect = isManual;

        // If this is a manual disconnect and we're the host, disconnect all clients first
        if (isManual && NetworkServer.active && NetworkClient.active)
        {
            DisconnectAllClients();

            // Give clients a moment to receive the disconnect message
            StartCoroutine(DelayedHostShutdown(isManual));
            return;
        }

        // Continue with normal shutdown process
        PerformNetworkShutdown(isManual);
    }

    /// <summary>
    /// Coroutine to delay host shutdown to allow client disconnect messages to be sent
    /// </summary>
    private IEnumerator DelayedHostShutdown(bool isManual)
    {
        Debug.Log("Waiting for client disconnects to process...");
        yield return new WaitForSeconds(0.1f); // Small delay to ensure disconnect messages are sent

        Debug.Log("Proceeding with host shutdown");
        PerformNetworkShutdown(isManual);
    }

    /// <summary>
    /// Performs the actual network shutdown and cleanup
    /// </summary>
    private void PerformNetworkShutdown(bool isManual = false)
    {
        // Use selective cleanup for manual disconnects in start scene, full cleanup otherwise
        if (isManual && SceneManager.GetActiveScene().name == System.IO.Path.GetFileNameWithoutExtension(menuScene))
        {
            Debug.Log("Manual disconnect in start scene - preserving host's room player");
            DestroyClientPlayersOnly();
        }
        else
        {
            Debug.Log("Full cleanup - destroying all player objects");
            DestroyAllPlayerObjectsAndClearReferences();
        }

        // Leave Steam lobby if connected
        if (SteamLobby.currentLobbyID != CSteamID.Nil)
        {
            Debug.Log($"Leaving Steam lobby: {SteamLobby.currentLobbyID}");
            SteamMatchmaking.LeaveLobby(SteamLobby.currentLobbyID);
            SteamLobby.currentLobbyID = CSteamID.Nil;
        }

        StopHost();
        StopClient();
        // Stop the server component
        NetworkServer.Shutdown();

        // Stop the client component  
        NetworkClient.Shutdown();

    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        //// Only allow players to connect in the menu scene
        //if (SceneManager.GetActiveScene().name != System.IO.Path.GetFileNameWithoutExtension(menuScene))
        //{
        //    conn.Disconnect();
        //    return;
        //}

        if (numPlayers >= 2)
        {
            conn.Disconnect();
            return;
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        EventsMaster.Event_OnGameModeChange(GameMode.Online_Multiplayer);
        if (SceneManager.GetActiveScene().name == System.IO.Path.GetFileNameWithoutExtension(menuScene))
        {
            Debug.Log("Adding player in menu scene");
            bool isLeader = roomPlayerOne == null;

            Network_LobbyPlayer roomPlayerInstance = Instantiate(roomPlayerPrefab);

            roomPlayerInstance.IsLeader = isLeader;

            NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);
        }
        else if (SceneManager.GetActiveScene().name.StartsWith("LEVEL"))
        {

            bool isLeader = roomPlayerOne == null;

            Network_LobbyPlayer roomPlayerInstance = Instantiate(roomPlayerPrefab);

            roomPlayerInstance.IsLeader = isLeader;

            if (roomPlayerInstance.playerNumber == PlayerNumber.Player1)
            {
                var gamePlayerInstance = Instantiate(gamePlayerOnePrefab);
                gamePlayerInstance.SetPlayerNumber(roomPlayerInstance.playerNumber);
                NetworkServer.AddPlayerForConnection(conn, gamePlayerInstance.gameObject);
            }
            else if (roomPlayerInstance.playerNumber == PlayerNumber.Player2)
            {
                var gamePlayerInstance = Instantiate(gamePlayerTwoPrefab);
                gamePlayerInstance.SetPlayerNumber(roomPlayerInstance.playerNumber);
                NetworkServer.AddPlayerForConnection(conn, gamePlayerInstance.gameObject);
            }
        }
    }
    // Call this to change scenes
    public void ChangeScene(string sceneName)
    {
        if (SceneManager.GetActiveScene().name.StartsWith("LEVEL"))
        {
            ReassignConnectionsToExistingRoomPlayers();
            StartCoroutine(WaitAndChangeScene(sceneName));
        }
        else
        {
            CircleWipeController circleWipeController = FindObjectOfType<CircleWipeController>();
            if (NetworkGameEvents.Instance != null)
                NetworkGameEvents.Instance.RpcStartSceneFadeOut();
            circleWipeController.FadeOut(() => ServerChangeScene(sceneName));
        }
    }

    public override void ServerChangeScene(string sceneName)
    {
        readyPlayers = 0;
        LoadingManager.ShowLoadingScreen();
        Debug.Log($"Server changing scene to {sceneName}");
        base.ServerChangeScene(sceneName);
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        // Show loading screen BEFORE scene loads
        LoadingManager.ShowLoadingScreen();
        Debug.Log($"Client changing scene to {newSceneName}");

        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }


    private void ReassignConnectionsToExistingRoomPlayers()
    {
        // Find current game players and get their connections before they get destroyed
        if (networkPlayerOne != null && networkPlayerTwo != null && roomPlayerOne != null && roomPlayerTwo != null)
        {

            // Get the connections from current game players
            var connectionOne = networkPlayerOne.connectionToClient;
            var connectionTwo = networkPlayerTwo.connectionToClient;

            // Determine which connection belongs to which room player based on player numbers
            if (networkPlayerOne.playerNumber == roomPlayerOne.playerNumber)
            {
                // Player 1 connection goes to room player 1, Player 2 connection goes to room player 2
                NetworkServer.ReplacePlayerForConnection(connectionOne, roomPlayerOne.gameObject, ReplacePlayerOptions.Destroy);
                NetworkServer.ReplacePlayerForConnection(connectionTwo, roomPlayerTwo.gameObject, ReplacePlayerOptions.Destroy);
            }
            else
            {
                // Player 1 connection goes to room player 2, Player 2 connection goes to room player 1  
                NetworkServer.ReplacePlayerForConnection(connectionOne, roomPlayerTwo.gameObject, ReplacePlayerOptions.Destroy);
                NetworkServer.ReplacePlayerForConnection(connectionTwo, roomPlayerOne.gameObject, ReplacePlayerOptions.Destroy);
            }

            // Clear game player references since they'll be destroyed with the scene
            networkPlayerOne = null;
            networkPlayerTwo = null;

            string playerObjectName = roomPlayerOne.connectionToClient.identity.gameObject != null ? roomPlayerOne.connectionToClient.identity.gameObject.gameObject.name : "No player object";
            Debug.Log($"Room player one reassigned: {playerObjectName}");
            string playerObjectName2 = roomPlayerTwo.connectionToClient.identity.gameObject != null ? roomPlayerTwo.connectionToClient.identity.gameObject.gameObject.name : "No player object";
            Debug.Log($"Room player two reassigned: {playerObjectName2}");
        }
        else
        {
            Debug.LogWarning("Could not reassign connections - missing references to players");
            Debug.LogWarning($"networkPlayerOne: {networkPlayerOne}, networkPlayerTwo: {networkPlayerTwo}");
            Debug.LogWarning($"roomPlayerOne: {roomPlayerOne}, roomPlayerTwo: {roomPlayerTwo}");
        }
    }

    private IEnumerator WaitAndChangeScene(string sceneName)
    {
        yield return new WaitForSeconds(0.2f); // Adjust the delay as needed
        Debug.Log($"[Coroutine] Proceeding to change scene to {sceneName}");
        ServerChangeScene(sceneName);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        // Only create game players for LEVEL scenes
        if (sceneName.StartsWith("LEVEL"))
        {
            Debug.Log($"Entering level scene: {sceneName}");
            CreateGamePlayersFromRoomPlayers();
        }
        else
        {
            Debug.Log($"Non-level scene: {sceneName} - staying with room players");
        }

        base.OnServerSceneChanged(sceneName);
    }

    private void CreateGamePlayersFromRoomPlayers()
    {
        if (roomPlayerOne == null || roomPlayerTwo == null)
        {
            Debug.LogError("Room players are null when trying to create game players!");
            return;
        }
        if (roomPlayerOne.connectionToClient == null || roomPlayerTwo.connectionToClient == null)
        {
            Debug.LogError("Room player connections are null!");
            return;
        }
        var playerOne = roomPlayerOne.connectionToClient;
        var gamePlayerOneInstance = Instantiate(gamePlayerOnePrefab);
        gamePlayerOneInstance.SetPlayerNumber(roomPlayerOne.playerNumber);
        NetworkServer.ReplacePlayerForConnection(playerOne, gamePlayerOneInstance.gameObject, ReplacePlayerOptions.KeepActive);
        string playerObjectName = gamePlayerOneInstance.connectionToClient.identity.gameObject != null ? gamePlayerOneInstance.connectionToClient.identity.gameObject.gameObject.name : "No player object";
        Debug.Log($"Game player one created: {playerObjectName}");

        var playerTwo = roomPlayerTwo.connectionToClient;
        var gamePlayerTwoInstance = Instantiate(gamePlayerTwoPrefab);
        gamePlayerTwoInstance.SetPlayerNumber(roomPlayerTwo.playerNumber);
        NetworkServer.ReplacePlayerForConnection(playerTwo, gamePlayerTwoInstance.gameObject, ReplacePlayerOptions.KeepActive);
        string playerObjectName2 = gamePlayerTwoInstance.connectionToClient.identity.gameObject != null ? gamePlayerTwoInstance.connectionToClient.identity.gameObject.gameObject.name : "No player object";
        Debug.Log($"Game player two created: {playerObjectName2}");

        // Store references to the network players for potential reassignment
        networkPlayerOne = gamePlayerOneInstance;
        networkPlayerTwo = gamePlayerTwoInstance;

        Debug.Log("Game players created and assigned to connections");
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);
        if (SceneManager.GetActiveScene().name.StartsWith("LEVEL"))
        {
            readyPlayers++;

            string playerObjectName = conn.identity != null ? conn.identity.gameObject.name : "No player object";
            Debug.Log($"Player {conn.connectionId} is ready. Player object: {playerObjectName}. Total ready players: {readyPlayers}");
            if (readyPlayers >= minPlayer)
            {
                Debug.Log("Minimum players reached, notifying players to start the game.");
                EventsMaster.Event_OnPlayerStateChange(new LevelIntroState(ObjectFinder.Instance.playerStateMachine));
                LoadingManager.HideLoadingScreen();
                NetworkGameEvents.Instance.RpcHideLoadingScreen();
                // Trigger input setup for all clients now that both players are ready
                ObjectFinder.Instance.playerStateMachine.RpcSetupInput();
            }
        }
        else if (SceneManager.GetActiveScene().name.StartsWith("LevelSelect"))
        {
            readyPlayers++;

            string playerObjectName = conn.identity != null ? conn.identity.gameObject.name : "No player object";
            Debug.Log($"Player {conn.connectionId} is ready. Player object: {playerObjectName}. Total ready players: {readyPlayers}");
            if (readyPlayers >= minPlayer)
            {
                Debug.Log("Minimum players reached, notifying players to start the game.");
                LevelSelectController levelSelectController = FindObjectOfType<LevelSelectController>();
                if (levelSelectController != null)
                {
                    LoadingManager.HideLoadingScreen();
                    NetworkGameEvents.Instance.RpcHideLoadingScreen();
                    levelSelectController.MultiplayerAllowNavigation();
                }
            }
        }
    }

    public override void OnClientSceneChanged()
    {
        Debug.Log($"OnClientSceneChanged called for scene: {SceneManager.GetActiveScene().name}");

        base.OnClientSceneChanged();
    }

    public override void OnStopClient()
    {
        Debug.Log("Client stopped");

        // Only handle if it wasn't a manual disconnect and we're not also a server
        if (!isManualDisconnect && !NetworkServer.active)
        {
            HandleDisconnection(true, "Client connection lost");
        }
        else if (isManualDisconnect)
        {
            // Even for manual disconnects, ensure cleanup happens
            DestroyAllPlayerObjectsAndClearReferences();
        }
        // Reset the flag for next time
        isManualDisconnect = false;
        base.OnStopClient();
    }

    public override void OnStopHost()
    {
        Debug.Log("Host stopped");

        // Only handle if it wasn't a manual disconnect
        if (!isManualDisconnect)
        {
            bool isInStartScene = SceneManager.GetActiveScene().name == System.IO.Path.GetFileNameWithoutExtension(menuScene);

            if (!isInStartScene)
            {
                HandleDisconnection(true, "Host connection lost");
            }
        }
        // For manual disconnects, DelayedManualExitCleanup already handled everything
        // so we don't need to do anything here

        base.OnStopHost();
    }

    public override void OnStopServer()
    {
        Debug.Log("Server stopped");

        // Only handle if it wasn't a manual disconnect
        if (!isManualDisconnect)
        {
            bool isInStartScene = SceneManager.GetActiveScene().name == System.IO.Path.GetFileNameWithoutExtension(menuScene);
            // If this was a dedicated server (not host), handle differently
            if (!NetworkClient.active && !isInStartScene)
            {
                HandleDisconnection(false, "Dedicated server stopped");
            }
            // If this was a host, OnStopHost will handle it

            // Always clear references for unexpected disconnects
            roomPlayerOne = null;
            roomPlayerTwo = null;
            networkPlayerOne = null;
            networkPlayerTwo = null;
        }
        // For manual disconnects, DelayedManualExitCleanup already handled everything

        base.OnStopServer();
    }

    public void NotifyPlayersOfReadyState()
    {
        roomPlayerOne?.HandleReadyToStart(IsReadyToStart());
        roomPlayerTwo?.HandleReadyToStart(IsReadyToStart());
        Debug.Log("Lobby notified ready");
    }

    public void UpdatePlayersUI()
    {
        roomPlayerOne?.UpdateUI();
        roomPlayerTwo?.UpdateUI();
    }

    private bool IsReadyToStart()
    {
        if (roomPlayerOne == null || roomPlayerTwo == null)
            return false;
        // Check if all players are ready
        return roomPlayerOne.isReady && roomPlayerTwo.isReady;
    }

    public void StartGame()
    {
        if (SceneManager.GetActiveScene().name == Path.GetFileNameWithoutExtension(menuScene) && IsReadyToStart())
        {
            roomPlayerOne?.RpcHideLobbyUI();
            roomPlayerTwo?.RpcHideLobbyUI();
            EventsMaster.Event_OnGameModeChange(GameMode.Online_Multiplayer);
            SteamLobby steamLobby = FindObjectOfType<SteamLobby>();
            if (steamLobby != null)
            {
                GameLogic.Instance.currentFriendID = steamLobby.GetOtherPlayerSteamID();
            }
            var tutorialProgress = GameLogic.Instance.GetTutorialProgress();
            if (tutorialProgress == null || !tutorialProgress.TryGetValue("P2Walk", out var p2Walk))
            {
                // Handle missing or null dictionary/key/value
                ChangeScene("Cutscene");
            }
            else
            {
                ChangeScene(p2Walk ? "LevelSelectScene" : "Cutscene");
            }
        }
    }

    public void NotifyPlayerPaused(bool isPaused)
    {
        Debug.Log($"NotifyPlayerPaused called: isPaused={isPaused}, NetworkServer.active={NetworkServer.active}");

        if (NetworkGameEvents.Instance == null)
        {
            Debug.LogWarning("NetworkGameEvents.Instance is null when trying to notify pause state");
            return;
        }

        // Determine which player is calling this
        PlayerNumber pausingPlayer = GetPausingPlayerNumber();
        Debug.Log($"Pausing player determined as: {pausingPlayer}");

        if (NetworkServer.active)
        {
            // Server/Host: Update pause state and send RPC directly
            Debug.Log("Server updating pause state and calling RpcNotifyPauseStateChanged directly");
            NetworkGameEvents.Instance.UpdatePlayerPauseState(isPaused, pausingPlayer);
            NetworkGameEvents.Instance.RpcNotifyPauseStateChanged(isPaused, pausingPlayer,
                NetworkGameEvents.Instance.p1Paused, NetworkGameEvents.Instance.p2Paused);
        }
        else
        {
            // Client: send command to server
            Debug.Log("Client calling CmdNotifyPlayerPaused to inform server");
            NetworkGameEvents.Instance.CmdNotifyPlayerPaused(isPaused, pausingPlayer);
        }
    }

    private PlayerNumber GetPausingPlayerNumber()
    {
        // In gameplay scenes, check network players
        if (networkPlayerOne != null && networkPlayerOne.isLocalPlayer)
        {
            return networkPlayerOne.playerNumber;
        }
        else if (networkPlayerTwo != null && networkPlayerTwo.isLocalPlayer)
        {
            return networkPlayerTwo.playerNumber;
        }

        // In lobby scenes, check room players
        if (roomPlayerOne != null && roomPlayerOne.isLocalPlayer)
        {
            return roomPlayerOne.playerNumber;
        }
        else if (roomPlayerTwo != null && roomPlayerTwo.isLocalPlayer)
        {
            return roomPlayerTwo.playerNumber;
        }

        return PlayerNumber.Player1; // Default fallback
    }
}
