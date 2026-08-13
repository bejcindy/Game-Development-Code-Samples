using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkGameEvents : NetworkBehaviour
{
    public static NetworkGameEvents Instance { get; private set; }

    [SyncVar] public bool p1Paused = false;
    [SyncVar] public bool p2Paused = false;

    // Add these SyncVars for multiplayer action confirmation
    [SyncVar] public bool pendingMultiplayerAction = false;
    [SyncVar] public int currentActionType = 0; // 0 = Restart, 1 = Reselect
    [SyncVar] public int requestingPlayerNumber = 0; // 1 = Player1, 2 = Player2

    // Reference to PauseMenuCanvas for LEVEL scenes
    private PauseMenuCanvas pauseMenuCanvasRef;

    public CompletePanel levelCompletePanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to scene loaded events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene loaded events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Called when a new scene is loaded. Checks if it's a LEVEL scene and finds PauseMenuCanvas.
    /// </summary>
    /// <param name="scene">The loaded scene</param>
    /// <param name="mode">The scene load mode</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if the scene name starts with "LEVEL"
        if (scene.name.StartsWith("LEVEL"))
        {
            // Find and store reference to PauseMenuCanvas
            StartCoroutine(FindPauseMenuCanvasDelayed());
            StartCoroutine(FindCompletePanelDelayed());
        }
        else
        {
            // Clear the reference if we're not in a LEVEL scene
            pauseMenuCanvasRef = null;
        }
    }

    /// <summary>
    /// Coroutine to find PauseMenuCanvas with a slight delay to ensure all objects are loaded
    /// </summary>
    private IEnumerator FindPauseMenuCanvasDelayed()
    {
        // Wait a frame to ensure all scene objects are properly initialized
        yield return null;

        pauseMenuCanvasRef = FindObjectOfType<PauseMenuCanvas>();

        if (pauseMenuCanvasRef == null)
        {
            Debug.LogWarning("NetworkGameEvents: PauseMenuCanvas not found in LEVEL scene");
        }
    }

    private IEnumerator FindCompletePanelDelayed()
    {
        yield return null;

        levelCompletePanel = FindObjectOfType<CompletePanel>(true);

        if(levelCompletePanel == null)
        {
            Debug.LogWarning("NetworkGameEvents: LevelCompletePanel not found in LEVEL scene");
        }
    }

    /// <summary>
    /// Public method to get the stored PauseMenuCanvas reference
    /// </summary>
    /// <returns>The PauseMenuCanvas reference if available, null otherwise</returns>
    public PauseMenuCanvas GetPauseMenuCanvas()
    {
        return pauseMenuCanvasRef;
    }

    [ClientRpc]
    public void RpcStartSceneFadeOut()
    {
        if (isServer) return; // Skip on server to prevent double fade

        CircleWipeController circleWipeController = FindObjectOfType<CircleWipeController>();
        circleWipeController.FadeOut();
    }

    [ClientRpc]
    public void RpcHideLoadingScreen()
    {
        LoadingManager.HideLoadingScreen();
    }

    [ClientRpc]
    public void RpcHideCompletePanel()
    {
        if(isServer) return;

        levelCompletePanel.HidePanel(null);
    }

    #region Multiplayer Pause State Management

    [Command(requiresAuthority = false)]
    public void CmdNotifyPlayerPaused(bool isPaused, PlayerNumber playerNumber)
    {
        Debug.Log($"CmdNotifyPlayerPaused received on server: isPaused={isPaused}, playerNumber={playerNumber}");

        // Update the server's pause state for the specific player
        UpdatePlayerPauseState(isPaused, playerNumber);

        // Now send the RPC to all clients with the updated state information
        RpcNotifyPauseStateChanged(isPaused, playerNumber, p1Paused, p2Paused);
    }

    [ClientRpc]
    public void RpcNotifyPauseStateChanged(bool isPaused, PlayerNumber pausingPlayer, bool p1CurrentlyPaused, bool p2CurrentlyPaused)
    {
        Debug.Log($"RpcNotifyPauseStateChanged received: isPaused={isPaused}, pausingPlayer={pausingPlayer}, p1Paused={p1CurrentlyPaused}, p2Paused={p2CurrentlyPaused}");

        // Update local pause state variables
        p1Paused = p1CurrentlyPaused;
        p2Paused = p2CurrentlyPaused;

        // Determine which player this client is
        PlayerNumber localPlayerNumber = GetLocalPlayerNumber();


        // Use the stored reference instead of FindObjectOfType for better performance
        PauseMenuCanvas pauseCanvas = pauseMenuCanvasRef ?? FindObjectOfType<PauseMenuCanvas>();
        if (pauseCanvas == null)
        {
            Debug.LogWarning("PauseMenuCanvas not found when trying to show/hide pause popup");
            return;
        }

        // Determine if this client should show the "other player paused" popup
        bool shouldShowPopup = ShouldShowOtherPlayerPausedPopup(localPlayerNumber, p1CurrentlyPaused, p2CurrentlyPaused);

        Debug.Log($"Should show popup: {shouldShowPopup}");

        if (shouldShowPopup)
        {
            pauseCanvas.ShowOtherPlayerPausedPopup();
        }
        else
        {
            pauseCanvas.HideOtherPlayerPausedPopup();
        }
    }

    public void UpdatePlayerPauseState(bool isPaused, PlayerNumber playerNumber)
    {
        if (playerNumber == PlayerNumber.Player1)
        {
            p1Paused = isPaused;
            Debug.Log($"Updated p1Paused to: {p1Paused}");
        }
        else if (playerNumber == PlayerNumber.Player2)
        {
            p2Paused = isPaused;
            Debug.Log($"Updated p2Paused to: {p2Paused}");
        }
    }

    private PlayerNumber GetLocalPlayerNumber()
    {
        // Get the network manager to check which player this client is
        THF_NetworkManager networkManager = (THF_NetworkManager)NetworkManager.singleton;
        if (networkManager == null) return PlayerNumber.Player1; // Default fallback

        // Check which network player belongs to this client
        if (networkManager.networkPlayerOne != null && networkManager.networkPlayerOne.isLocalPlayer)
        {
            return networkManager.networkPlayerOne.playerNumber;
        }
        else if (networkManager.networkPlayerTwo != null && networkManager.networkPlayerTwo.isLocalPlayer)
        {
            return networkManager.networkPlayerTwo.playerNumber;
        }

        // Fallback: check room players if network players don't exist
        if (networkManager.roomPlayerOne != null && networkManager.roomPlayerOne.isLocalPlayer)
        {
            return networkManager.roomPlayerOne.playerNumber;
        }
        else if (networkManager.roomPlayerTwo != null && networkManager.roomPlayerTwo.isLocalPlayer)
        {
            return networkManager.roomPlayerTwo.playerNumber;
        }

        return PlayerNumber.Player1; // Final fallback
    }

    private bool ShouldShowOtherPlayerPausedPopup(PlayerNumber localPlayer, bool p1CurrentlyPaused, bool p2CurrentlyPaused)
    {
        // Show popup if the OTHER player is paused, but not if THIS player is paused
        if (localPlayer == PlayerNumber.Player1)
        {
            // Local player is P1, show popup if P2 is paused (regardless of P1's state)
            return p2CurrentlyPaused && !p1CurrentlyPaused;
        }
        else if (localPlayer == PlayerNumber.Player2)
        {
            // Local player is P2, show popup if P1 is paused (regardless of P2's state)
            return p1CurrentlyPaused && !p2CurrentlyPaused;
        }

        return false;
    }

    // Public method to check if other player is paused (for PauseMenuCanvas to use)
    public bool IsOtherPlayerPaused(PlayerNumber localPlayer)
    {
        return ShouldShowOtherPlayerPausedPopup(localPlayer, p1Paused, p2Paused);
    }
    #endregion

    #region Multiplayer Action Confirmation
    [Command(requiresAuthority = false)]
    public void CmdRequestMultiplayerAction(PauseMenuCanvas.MultiplayerActionType actionType, PlayerNumber requestingPlayer)
    {
        Debug.Log($"CmdRequestMultiplayerAction received: actionType={actionType}, requestingPlayer={requestingPlayer}");

        // Update server state
        pendingMultiplayerAction = true;
        currentActionType = (int)actionType;
        requestingPlayerNumber = (int)requestingPlayer;

        // Send RPC to show confirmation to other player
        RpcRequestMultiplayerAction(actionType, requestingPlayer);
    }

    [ClientRpc]
    public void RpcRequestMultiplayerAction(PauseMenuCanvas.MultiplayerActionType actionType, PlayerNumber requestingPlayer)
    {
        Debug.Log($"RpcRequestMultiplayerAction received: actionType={actionType}, requestingPlayer={requestingPlayer}");

        // Update local state
        pendingMultiplayerAction = true;
        currentActionType = (int)actionType;
        requestingPlayerNumber = (int)requestingPlayer;

        // Determine which player this client is
        PlayerNumber localPlayerNumber = GetLocalPlayerNumber();

        // Only show confirmation panel to the player who didn't request the action
        if (localPlayerNumber != requestingPlayer)
        {
            // First, ensure their pause menu is open by calling GameLogic.Events_RequestPause
            EventsMaster.Event_OnPause();

            // Wait a frame to ensure the pause menu is properly opened, then show confirmation
            StartCoroutine(ShowConfirmationAfterPauseMenu(actionType, requestingPlayer));
        }
    }

    private IEnumerator ShowConfirmationAfterPauseMenu(PauseMenuCanvas.MultiplayerActionType actionType, PlayerNumber requestingPlayer)
    {
        // Wait one frame to ensure pause menu is fully opened
        yield return null;

        PauseMenuCanvas pauseCanvas = GetPauseMenuCanvas();
        if (pauseCanvas != null)
        {
            pauseCanvas.ShowMultiplayerConfirmationRequest(actionType, requestingPlayer);
        }
        else
        {
            Debug.LogWarning("PauseMenuCanvas not found when trying to show confirmation request");
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdRespondToMultiplayerAction(bool confirmed, PlayerNumber respondingPlayer)
    {
        Debug.Log($"CmdRespondToMultiplayerAction received: confirmed={confirmed}, respondingPlayer={respondingPlayer}");

        // Send RPC to handle the response
        RpcRespondToMultiplayerAction(confirmed, respondingPlayer);
    }

    [ClientRpc]
    public void RpcRespondToMultiplayerAction(bool confirmed, PlayerNumber respondingPlayer)
    {
        Debug.Log($"RpcRespondToMultiplayerAction received: confirmed={confirmed}, respondingPlayer={respondingPlayer}");

        if (!pendingMultiplayerAction) return;

        // Get the action type that was pending
        PauseMenuCanvas.MultiplayerActionType actionType = (PauseMenuCanvas.MultiplayerActionType)currentActionType;
        PlayerNumber originalRequester = (PlayerNumber)requestingPlayerNumber;

        // Clear pending state
        pendingMultiplayerAction = false;
        currentActionType = 0;
        requestingPlayerNumber = 0;

        // Find the PauseMenuCanvas and handle the response
        PauseMenuCanvas pauseCanvas = GetPauseMenuCanvas();
        if (pauseCanvas != null)
        {
            // Determine which player this client is
            PlayerNumber localPlayerNumber = GetLocalPlayerNumber();

            if (confirmed)
            {
                pauseCanvas.OnMultiplayerActionConfirmed();
                p1Paused = false;
                p2Paused = false;

                // Only the server should handle the actual scene transition
                if (isServer)
                {
                    PlayerStateMachine playerStateMachine = ObjectFinder.Instance.playerStateMachine;
                    if (playerStateMachine != null)
                    {
                        playerStateMachine.FadeOut(() =>
                        {
                            GameLogic.Instance.NotifyGameRunning();

                            var networkManager = (THF_NetworkManager)NetworkManager.singleton;
                            if (networkManager != null)
                            {
                                if (actionType == PauseMenuCanvas.MultiplayerActionType.Restart)
                                {
                                    networkManager.ChangeScene(SceneManager.GetActiveScene().name);
                                }
                                else // Reselect
                                {
                                    networkManager.ChangeScene("LevelSelectScene");
                                }
                            }
                        });
                    }
                }
            }
            else
            {
                // Action was cancelled - only the original requester should handle the response
                if (localPlayerNumber == originalRequester)
                {
                    pauseCanvas.HandleMultiplayerRequestRejected(actionType);
                }
            }
        }
        else
        {
            Debug.LogWarning("PauseMenuCanvas not found when trying to handle action response");
        }
    }
    #endregion

    // Add this method in the NetworkGameEvents class, after the existing RpcHideCompletePanel method:

    [ClientRpc]
    public void RpcSaveLevelData()
    {
        GameLogic.Instance.NotifyGameRunning();
        // Server already saves via CompletePanel button callbacks, only client needs this
        if (isServer) return;

        LevelState levelState = ObjectFinder.Instance.levelState;
        if (levelState != null)
        {
            Debug.Log("[NetworkGameEvents] Client saving level data via RPC");
            levelState.SaveLevelData();
        }
        else
        {
            Debug.LogWarning("[NetworkGameEvents] LevelState not found when trying to save level data on client");
        }
    }
}
