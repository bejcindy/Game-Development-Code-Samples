using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VInspector;

namespace THP.Network
{
    public class Network_LobbyPlayer : NetworkBehaviour
    {
        [Header("Player Info")]
        [SyncVar(hook = nameof(OnSteamIdChanged))]
        public ulong SteamId;

        [SyncVar(hook = nameof(OnDisplayNameChanged))]
        public string DisplayName = "Loading...";

        [SyncVar]
        public PlayerNumber playerNumber;

        [Foldout("UI")]
        [SerializeField] private GameObject lobbyUI = null;

        [Header("Player UI Elements")]
        [SerializeField] private GameObject p1UI = null;
        [SerializeField] private GameObject p2UI = null;
        [SerializeField] private TextMeshProUGUI p1Name = null;
        [SerializeField] private TextMeshProUGUI p2Name = null;
        [SerializeField] private TextMeshProUGUI readyButtonText = null;
        [SerializeField] private GameObject p1Ready = null;
        [SerializeField] private GameObject p2Ready = null;
        [SerializeField] private Image p1Avatar = null;
        [SerializeField] private Image p2Avatar = null;
        [SerializeField] private TextMeshProUGUI pingText = null;
        [SerializeField] private float ping;

        [Header("Buttons")]
        [SerializeField] private GameObject buttonGroup = null;
        [SerializeField] private GameObject startGameButton = null;
        [SerializeField] private GameObject leaveButton = null;
        [SerializeField] private GameObject inviteButton = null;
        [SerializeField] private Button readyButton;

        [Header("Button Images")]
        [SerializeField] private Image startButtonImage;
        [SerializeField] private Image readyButtonImage;
        [SerializeField] private Image leaveButtonImage;
        [SerializeField] private Image inviteButtonImage;

        [Header("Localized Text")]
        [SerializeField] private LocalizedString readyText;
        [SerializeField] private LocalizedString cancelText;
        [SerializeField] private LocalizedString waitingForText;
        [SerializeField] private LocalizedString player1Text;
        [SerializeField] private LocalizedString player2Text;
        [SerializeField] private LocalizeStringEvent p1NameLocalizer;
        [SerializeField] private LocalizeStringEvent p2NameLocalizer;
        [SerializeField] private LocalizeStringEvent readyButtonTextLocalizer;

        [Header("UI Element Reader")]
        [SerializeField] private UIElementData uIElementData;


        private bool isLeader = false;
        private InputActions_Hilltop roomPlayerInputs;
        private ModeSelector modeSelector;
        public bool IsLeader
        {
            set
            {
                isLeader = value;
                // if (startGameButton != null)
                // {
                //     startGameButton.SetActive(isLeader);
                // }
            }
        }

        [SyncVar(hook = nameof(OnReadyStateChanged))]
        public bool isReady = false;

        private THF_NetworkManager room;
        private THF_NetworkManager Room
        {
            get
            {
                if (room == null)
                {
                    room = (THF_NetworkManager)NetworkManager.singleton;
                }
                return room;
            }
        }

        private void OnEnable()
        {
            InputManager.OnControlSchemeChanged += OnControlSchemeChanged;
        }

        private void OnDisable()
        {
            InputManager.OnControlSchemeChanged -= OnControlSchemeChanged;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (NetworkServer.connections.Count == 1)
                playerNumber = PlayerNumber.Player1;
            else
                playerNumber = PlayerNumber.Player2;
        }

        public override void OnStartClient()
        {
            DontDestroyOnLoad(gameObject);

            if (Room.roomPlayerOne == null)
            {
                Room.roomPlayerOne = this;
            }
            else if (Room.roomPlayerTwo == null)
            {
                Room.roomPlayerTwo = this;
            }

            roomPlayerInputs = new InputActions_Hilltop();
            roomPlayerInputs.MultiRoomPlayerInput.Ready.performed += ctx => OnReadyOrStart(ctx);
            roomPlayerInputs.MultiRoomPlayerInput.Cancel.performed += ctx => OnCancelReady(ctx);
            roomPlayerInputs.MultiRoomPlayerInput.Invite.performed += ctx => OnInvite(ctx);
            roomPlayerInputs.MultiRoomPlayerInput.Enable();
            Debug.Log($"Network_LobbyPlayer OnStartClient: PlayerNumber={playerNumber}");
            EventsMaster.Event_OnGameModeChange(GameMode.Online_Multiplayer);
            // UpdateUI();
            Room.UpdatePlayersUI();
        }


        public override void OnStartAuthority()
        {
            //TODO: change this to Steam name later
            if (GameLogic.Instance.steamServiceState == SteamServiceState.Running)
            {
                DisplayName = SteamFriends.GetPersonaName();
                ulong steamId = SteamUser.GetSteamID().m_SteamID;
                if (isServer)
                {
                    SteamId = steamId;
                }
                else
                {
                    CmdSetSteamId(steamId);
                }
            }
            else
            {
                DisplayName = "Player " + playerNumber.ToString();
            }
            CmdSetDisplayName(DisplayName);
            if (SceneManager.GetActiveScene().name == "StartScene")
            {
                modeSelector = FindObjectOfType<ModeSelector>();
                modeSelector.myLobbyPlayer = this;
                Room.UpdatePlayersUI();
                OnControlSchemeChanged(InputManager.Instance.CurrentControlScheme, "");
                TurnOnUI();
            }
            base.OnStartAuthority();
        }

        [Command]
        private void CmdSetSteamId(ulong steamId)
        {
            SteamId = steamId;
        }

        void TurnOnUI()
        {
            lobbyUI.SetActive(true);
            //if is leader, show invite friend button
            if (isLeader)
            {
                inviteButton.SetActive(true);
                inviteButton.GetComponent<Button>().interactable = true;
            }
        }

        public override void OnStopClient()
        {
            if (Room.roomPlayerOne == this)
            {
                Room.roomPlayerOne = null;
            }
            else if (Room.roomPlayerTwo == this)
            {
                Room.roomPlayerTwo = null;
            }

            if (roomPlayerInputs != null)
                roomPlayerInputs.Dispose();
            // UpdateUI();

            if (SceneManager.GetActiveScene().name != "StartScene")
                return;
            Room.UpdatePlayersUI();
            Debug.Log($"Network_LobbyPlayer OnStopClient: PlayerNumber={playerNumber}");
        }

        public void OnDisplayNameChanged(string oldValue, string newValue)
        {
            // Update the display name in the UI
            Room.UpdatePlayersUI();
            Debug.Log($"Display name changed from {oldValue} to {newValue}");
        }


        public void OnCloseFriendListPanel()
        {
            if (!isLocalPlayer) return;

            if (SceneManager.GetActiveScene().name == "StartScene")
            {
                inviteButton.SetActive(true);
                inviteButton.GetComponent<Button>().interactable = true;
                buttonGroup.SetActive(true);
                p1UI.SetActive(true);
                p2UI.SetActive(true);
            }
        }

        public void UpdateUI()
        {
            if (!isLocalPlayer) return;

            if (Room.roomPlayerOne != null)
            {
                p1NameLocalizer.enabled = false;
                p1Name.text = Room.roomPlayerOne.DisplayName;
                p1Ready.SetActive(Room.roomPlayerOne.isReady);

                SteamAvatarHelper.GetAvatarForUser(new CSteamID(Room.roomPlayerOne.SteamId), p1Avatar, SteamAvatarHelper.AvatarSize.Medium);
                p1Avatar.gameObject.SetActive(true);
                Room.startSceneCanvas.ToggleP1Model(true);
                Room.startSceneCanvas.ToggleP1Light(Room.roomPlayerOne.isReady);
            }
            else
            {
                p1NameLocalizer.enabled = true;
                p1NameLocalizer.RefreshString();
                p1Avatar.gameObject.SetActive(false);
                readyButton.gameObject.SetActive(false);
                startGameButton.SetActive(false);
                p1Ready.SetActive(false);
                Room.startSceneCanvas.ToggleP1Model(false);
                Room.startSceneCanvas.ToggleP1Light(false);
            }

            if (Room.roomPlayerTwo != null)
            {
                p2NameLocalizer.enabled = false;
                p2Name.text = Room.roomPlayerTwo.DisplayName;
                p2Ready.SetActive(Room.roomPlayerTwo.isReady);

                SteamAvatarHelper.GetAvatarForUser(new CSteamID(Room.roomPlayerTwo.SteamId), p2Avatar, SteamAvatarHelper.AvatarSize.Medium);
                p2Avatar.gameObject.SetActive(true);
                readyButton.gameObject.SetActive(true);
                Room.startSceneCanvas.ToggleP2Model(true);
                inviteButton.SetActive(false);
                Room.startSceneCanvas.ToggleP2Light(Room.roomPlayerTwo.isReady);
            }
            else
            {
                //StartCoroutine(LoadWaitingForPlayerText(p2Name, player2Text));
                p2NameLocalizer.enabled = true;
                p2NameLocalizer.RefreshString();
                p2Ready.SetActive(false);
                p2Avatar.gameObject.SetActive(false);
                readyButton.gameObject.SetActive(false);
                startGameButton.SetActive(false);
                Room.startSceneCanvas.ToggleP2Model(false);
                Room.startSceneCanvas.ToggleP2Light(false);
                inviteButton.SetActive(true);
                inviteButton.GetComponent<Button>().interactable = true;
            }

        }

        private IEnumerator LoadWaitingForPlayerText(TextMeshProUGUI textComponent, LocalizedString playerText)
        {
            Debug.Log("LoadWaitingForPlayerText started");

            // Wait for localization system
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                Debug.Log("Waiting for localization initialization...");
                yield return LocalizationSettings.InitializationOperation;
            }

            if (waitingForText.IsEmpty)
            {
                Debug.LogError("waitingForText is empty! Check Inspector assignment.");
                textComponent.text = "Waiting for Player...";
                yield break;
            }

            // Load waiting text
            var waitingOperation = waitingForText.GetLocalizedStringAsync();
            yield return waitingOperation;

            string waitingResult = null;
            if (waitingOperation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                waitingResult = waitingOperation.Result;
                Debug.Log($"waitingOperation Result: '{waitingResult}'");
            }
            else
            {
                Debug.LogError($"waitingOperation failed: {waitingOperation.OperationException}");
            }

            // Load player text  
            var playerOperation = playerText.GetLocalizedStringAsync();
            yield return playerOperation;

            string playerResult = null;
            if (playerOperation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                playerResult = playerOperation.Result;
                Debug.Log($"playerOperation Result: '{playerResult}'");
            }
            else
            {
                Debug.LogError($"playerOperation failed: {playerOperation.OperationException}");
            }

            // Use the stored results instead of checking operations again
            if (!string.IsNullOrEmpty(waitingResult) && !string.IsNullOrEmpty(playerResult))
            {
                string combinedText = $"{waitingResult} {playerResult}...";
                Debug.Log($"SUCCESS: Setting text to '{combinedText}'");
                textComponent.text = combinedText;
            }
            else
            {
                Debug.Log("FALLBACK: Using fallback text - one or both strings were null/empty");
                string playerNumber = playerText == player1Text ? "1" : "2";
                textComponent.text = $"Waiting for Player {playerNumber}...";
            }
        }


        [Command]
        private void CmdSetDisplayName(string displayName)
        {
            DisplayName = displayName;
        }

        [Command]
        public void CmdSetReady()
        {
            isReady = !isReady;
            Room.NotifyPlayersOfReadyState();

        }

        [Command]
        public void CmdStartGame()
        {
            if (!isLeader) return;

            Debug.Log("Starting game...");
            Room.StartGame();
        }

        public void OnReadyStateChanged(bool oldValue, bool newValue)
        {
            // UpdateUI();
            if (SceneManager.GetActiveScene().name == "StartScene")
            {
                Room.UpdatePlayersUI();
                Debug.Log($"Ready state changed from {oldValue} to {newValue}");
                if (isLocalPlayer)
                {
                    UpdateReadyButtonText(newValue);
                }
            }

        }

        private void UpdateReadyButtonText(bool isReadyState)
        {
            if (isReadyState)
            {
                readyButtonTextLocalizer.StringReference.TableEntryReference = "button_Unready";
                readyButtonImage.sprite = InputManager.Instance.CurrentControlScheme == "Keyboard" ? uIElementData.escKey : uIElementData.buttonSouthKey;
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(CmdSetPlayerNotReady);
                leaveButton.SetActive(false);
            }
            else
            {
                readyButtonTextLocalizer.StringReference.TableEntryReference = "button_Ready";
                readyButtonImage.sprite = InputManager.Instance.CurrentControlScheme == "Keyboard" ? uIElementData.spaceKey : uIElementData.casketBackLift_Controller;
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(CmdSetPlayerReady);
                leaveButton.SetActive(true);
            }
        }

        private void OnInvite(InputAction.CallbackContext ctx)
        {
            if (!isLocalPlayer) return;

            if (!HasAllPlayers())
                ShowFriendListPanel();
        }

        private void OnReadyOrStart(InputAction.CallbackContext ctx)
        {
            if (!isLocalPlayer) return;
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ButtonHover);
            if (IsReadyToStart())
            {
                CmdStartGame();
            }
            else
            {
                if (HasAllPlayers())
                    CmdSetPlayerReady();
            }
        }

        private void OnCancelReady(InputAction.CallbackContext ctx)
        {
            if (!isLocalPlayer) return;

            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.SpotlightOff);

            if (isReady)
                CmdSetPlayerNotReady();
            else
            {
                if (FriendListPanel.Instance.IsVisible)
                {
                    FriendListPanel.Instance.Hide();
                }
                else
                {
                    LeaveLobby();
                }
            }
        }

        [Command]
        private void CmdSetPlayerReady()
        {
            isReady = true;
            Room.NotifyPlayersOfReadyState();
        }

        [Command]
        private void CmdSetPlayerNotReady()
        {
            isReady = false;
            Room.NotifyPlayersOfReadyState();
        }

        public void LeaveLobby()
        {
            modeSelector.ExitHouse();
        }

        private bool IsReadyToStart()
        {
            if (Room.roomPlayerOne == null || Room.roomPlayerTwo == null)
                return false;
            // Check if all players are ready
            return Room.roomPlayerOne.isReady && Room.roomPlayerTwo.isReady;
        }

        private bool HasAllPlayers()
        {
            return Room.roomPlayerOne != null && Room.roomPlayerTwo != null;
        }

        public void HandleReadyToStart(bool readyToStart)
        {
            if (!isLeader) return;

            // CmdStartGame();
            startGameButton.SetActive(readyToStart);
        }

        [ClientRpc]
        public void RpcHideLobbyUI()
        {
            lobbyUI?.SetActive(false);
            Debug.Log("Called HideLobbyUI on " + playerNumber);
        }

        private void OnSteamIdChanged(ulong oldValue, ulong newValue)
        {
            // Update UI when Steam ID is received/changed
            Room.UpdatePlayersUI();
        }

        public void ShowFriendListPanel()
        {
            if (!isLocalPlayer) return;
            // FindObjectOfType<StartSceneCanvas>().friendListPanel.SetActive(true);            
            FriendListPanel.Instance.Show();
            inviteButton.SetActive(false);
            inviteButton.GetComponent<Button>().interactable = false;
            buttonGroup.SetActive(false);
            p1UI.SetActive(false);
            p2UI.SetActive(false);
        }

        private void OnControlSchemeChanged(string newScheme, string oldScheme)
        {
            if (!isLocalPlayer) return;

            startButtonImage.sprite = newScheme == "Keyboard" ? uIElementData.spaceKey : uIElementData.casketBackLift_Controller;
            leaveButtonImage.sprite = newScheme == "Keyboard" ? uIElementData.escKey : uIElementData.buttonEastKey;
            inviteButtonImage.sprite = newScheme == "Keyboard" ? uIElementData.spaceKey : uIElementData.buttonSouthKey;

            if (isReady)
            {
                readyButtonImage.sprite = newScheme == "Keyboard" ? uIElementData.escKey : uIElementData.buttonSouthKey;
            }
            else
            {
                readyButtonImage.sprite = newScheme == "Keyboard" ? uIElementData.spaceKey : uIElementData.casketBackLift_Controller;
            }
        }
    }
}

