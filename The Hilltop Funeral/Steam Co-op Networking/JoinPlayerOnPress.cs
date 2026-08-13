using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Script that will spawn a new player when a button on a device is pressed.
public class JoinPlayerOnPress : MonoBehaviour
{
    public bool pairingInProgress = false;
    public bool unpairingInProgress = false;
    private int playerCount = 0;
    private bool p1Ready = false;
    private bool p2Ready = false;

    [SerializeField] private StartSceneCanvas startSceneCanvas;
    [SerializeField] private GameObject playerOnePairer;
    [SerializeField] private GameObject playerTwoPairer;

    [SerializeField] private Sprite controllerSprite;
    [SerializeField] private Sprite keyboardSprite;

    [Header("Paired Display")]
    [SerializeField] private GameObject p1PairedDisplay;
    [SerializeField] private GameObject p2PairedDisplay;
    [SerializeField] private TextMeshProUGUI p1ControlText;
    [SerializeField] private TextMeshProUGUI p2ControlText;
    [SerializeField] private Image playerOneControlImage;
    [SerializeField] private Image playerTwoControlImage;

    [Header("In-Scene References")]
    [SerializeField] private GameObject playerOneObject;
    [SerializeField] private GameObject playerTwoObject;
    [SerializeField] private GameObject playerOneLight;
    [SerializeField] private GameObject playerTwoLight;

    [Header("Prefab")]
    [SerializeField] private GameObject localPlayerPrefab;

    [Header("Actions UI")]
    [SerializeField] private LocalizeStringEvent p1ReadyButtonText;
    [SerializeField] private LocalizeStringEvent p1CancelButtonText;
    [SerializeField] private Image p1ReadyButtonImage;
    [SerializeField] private Image p1CancelButtonImage;
    [SerializeField] private LocalizeStringEvent p2ReadyButtonText;
    [SerializeField] private LocalizeStringEvent p2CancelButtonText;
    [SerializeField] private Image p2ReadyButtonImage;
    [SerializeField] private Image p2CancelButtonImage;

    [Header("Start Button UI")]
    [SerializeField] private GameObject startButtonObject;
    [SerializeField] private Image startButtonImage;

    [Header("UI Sprites")]
    [SerializeField] private UIElementData uiData;

    // We want to remove the event listener we install through InputSystem.onAnyButtonPress
    // after we're done so remember it here.
    private IDisposable m_EventListener;

    private InputActions_Hilltop pairingInputsP1;
    private InputActions_Hilltop pairingInputsP2;
    private PlayerInput pairingPlayerInputP1;
    private PlayerInput pairingPlayerInputP2;

    // When enabled, we install our button press listener.
    void OnEnable()
    {
        ClearPairing();
        transform.localScale = Vector3.zero;
        p1PairedDisplay.SetActive(false);
        p2PairedDisplay.SetActive(false);
        playerOnePairer.SetActive(true);
        playerTwoPairer.SetActive(true);
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            m_EventListener =
                    InputSystem.onAnyButtonPress.Call(OnButtonPressed);
        });
    }

    // When disabled, we remove our button press listener.
    void OnDisable()
    {
        m_EventListener.Dispose();
        DOTween.Kill(transform);
        DOTween.Kill(playerOnePairer.transform);
        DOTween.Kill(playerTwoPairer.transform);
        DOTween.Kill(p1PairedDisplay.transform);
        DOTween.Kill(p2PairedDisplay.transform);
    }

    void OnButtonPressed(InputControl button)
    {
        var device = button.device;
        if (unpairingInProgress)
            return;

        // Only accept keyboard and controllers
        if (!(device is Keyboard) && !(device is Gamepad))
            return;

        // Ignore Escape key on keyboard
        if (device is Keyboard && button.name != "space")
        {
            return;
        }

        // Ignore Button East on Gamepad
        if (device is Gamepad && button.name != "buttonSouth")
        {
            return;
        }

        // Check if the device is already paired with a player, allow double pairing for keyboard
        if (device is Gamepad && DeviceManager.Instance.IsDevicePaired(device))
        {
            return;
        }

        if (playerCount >= 2)
        {
            return;
        }

        if (DeviceManager.Instance.PlayerOneDevice == null)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.PartTimerDefault);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ExitPause);

            DeviceManager.Instance.SetPlayerDevice(PlayerNumber.Player1, device);

            GameObject localP1 = Instantiate(localPlayerPrefab);
            pairingPlayerInputP1 = localP1.AddComponent<PlayerInput>();
            localP1.name = "Player1_Input";
            SetUpLocalMultiPairingControl(PlayerNumber.Player1, pairingPlayerInputP1);
            SetUpPairedDisplay(PlayerNumber.Player1);
            playerCount++;
            pairingInProgress = true;
        }
        else if (DeviceManager.Instance.PlayerTwoDevice == null)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UnderTakerDefault);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ExitPause);

            DeviceManager.Instance.SetPlayerDevice(PlayerNumber.Player2, device);
            GameObject localP2 = Instantiate(localPlayerPrefab);
            pairingPlayerInputP2 = localP2.AddComponent<PlayerInput>();
            localP2.name = "Player2_Input";
            SetUpLocalMultiPairingControl(PlayerNumber.Player2, pairingPlayerInputP2);
            SetUpPairedDisplay(PlayerNumber.Player2);
            playerCount++;
            if (DeviceManager.Instance.PlayerOneDevice is Keyboard && DeviceManager.Instance.PlayerTwoDevice is Keyboard)
                EventsMaster.Event_OnGameModeChange(GameMode.Singleplayer);
            else
                EventsMaster.Event_OnGameModeChange(GameMode.Local_Multiplayer);
        }
    }

    private void SetUpPairedDisplay(PlayerNumber playerNumber)
    {
        if (playerNumber == PlayerNumber.Player1)
        {
            playerOnePairer.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                //Turn on the paired display UI
                playerOneControlImage.sprite = DeviceManager.Instance.PlayerOneDevice is Keyboard ? keyboardSprite : controllerSprite;
                playerOneControlImage.preserveAspect = true;
                p1ReadyButtonImage.sprite = DeviceManager.Instance.PlayerOneDevice is Keyboard ? uiData.qKey : uiData.casketBackLift_Controller;
                p1CancelButtonImage.sprite = DeviceManager.Instance.PlayerOneDevice is Keyboard ? uiData.eKey : uiData.buttonEastKey;
                p1ReadyButtonImage.gameObject.SetActive(true);
                p1CancelButtonImage.gameObject.SetActive(true);
                p1ControlText.text = DeviceManager.Instance.PlayerOneDevice.displayName;
                p1ReadyButtonText.StringReference.TableEntryReference = "button_Ready";
                p1CancelButtonText.StringReference.TableEntryReference = "button_Unpair";
                p1PairedDisplay.SetActive(true);
                p1PairedDisplay.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

                //Turn on player one object and light
                playerOneObject.SetActive(true);
            });
        }
        else if (playerNumber == PlayerNumber.Player2)
        {
            playerTwoPairer.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                //Turn on the paired display UI
                playerTwoControlImage.sprite = DeviceManager.Instance.PlayerTwoDevice is Keyboard ? keyboardSprite : controllerSprite;
                playerTwoControlImage.preserveAspect = true;
                p2ReadyButtonImage.sprite = DeviceManager.Instance.PlayerTwoDevice is Keyboard ? uiData.uKey : uiData.casketBackLift_Controller;
                p2CancelButtonImage.sprite = DeviceManager.Instance.PlayerTwoDevice is Keyboard ? uiData.oKey : uiData.buttonEastKey;
                p2ReadyButtonImage.gameObject.SetActive(true);
                p2CancelButtonImage.gameObject.SetActive(true);
                p2ControlText.text = DeviceManager.Instance.PlayerTwoDevice.displayName;
                p2ReadyButtonText.StringReference.TableEntryReference = "button_Ready";
                p2CancelButtonText.StringReference.TableEntryReference = "button_Unpair";
                p2PairedDisplay.SetActive(true);
                p2PairedDisplay.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

                //Turn on player two object and light
                playerTwoObject.SetActive(true);
            });
        }
    }

    private bool AllowP2ControlFromP1Device()
    {
        if (DeviceManager.Instance == null)
        {
            Debug.LogWarning("DeviceManager instance is null when checking for shared keyboard. This should not happen.");
            return false;
        }
        if (DeviceManager.Instance.PlayerOneDevice == null || DeviceManager.Instance.PlayerTwoDevice == null)
        {
            return false;
        }
        Debug.Log("Checking if P2 can use P1's device: P1 Device = " + DeviceManager.Instance.PlayerOneDevice.displayName + ", P2 Device = " + DeviceManager.Instance.PlayerTwoDevice.displayName);
        Debug.Log("Are we sharing a keyboard? " + (DeviceManager.Instance.PlayerOneDevice is Keyboard && DeviceManager.Instance.PlayerTwoDevice is Keyboard));
        if (DeviceManager.Instance.PlayerOneDevice is Keyboard && DeviceManager.Instance.PlayerTwoDevice is Keyboard)
            return true;
        else
            return false;
    }

    private void SetUpLocalMultiPairingControl(PlayerNumber playerNumber, PlayerInput playerInput)
    {
        Debug.Log("Setting up local pairing menu input for " + playerNumber);

        if (playerNumber == PlayerNumber.Player1)
        {
            pairingInputsP1 = new InputActions_Hilltop();
            pairingPlayerInputP1 = playerInput;

            playerInput.actions = pairingInputsP1.asset;
            playerInput.defaultActionMap = "LocalControlPairing";
            playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;

            // Disable automatic control scheme switching - we manage devices manually
            playerInput.neverAutoSwitchControlSchemes = true;

            // Subscribe to pairing actions
            pairingInputsP1.LocalControlPairing.P1Ready.performed += ctx => OnP1Ready(ctx);
            pairingInputsP1.LocalControlPairing.P1Cancel.performed += ctx => OnP1Cancel(ctx);
            pairingInputsP1.LocalControlPairing.Start.performed += ctx => OnP1Start(ctx);

            // Always subscribe P2 actions to P1's input for keyboard support
            // They will be filtered by device check in the handlers
            pairingInputsP1.LocalControlPairing.P2Ready.performed += ctx => OnP2Ready(ctx);
            pairingInputsP1.LocalControlPairing.P2Cancel.performed += ctx => OnP2Cancel(ctx);

            if (playerInput.user.valid)
                playerInput.user.UnpairDevices();

            InputUser.PerformPairingWithDevice(DeviceManager.Instance.PlayerOneDevice, playerInput.user);
            playerInput.SwitchCurrentControlScheme(DeviceManager.Instance.PlayerOneDevice);

            pairingInputsP1.Enable();
        }
        else
        {
            pairingPlayerInputP2 = playerInput;
            // Skip creating separate P2 input if both are keyboards (same device)
            if (AllowP2ControlFromP1Device())
            {
                playerInput.enabled = false;
                return;
            }

            pairingInputsP2 = new InputActions_Hilltop();

            playerInput.actions = pairingInputsP2.asset;
            playerInput.defaultActionMap = "LocalControlPairing";
            playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;

            // Disable automatic control scheme switching - we manage devices manually
            playerInput.neverAutoSwitchControlSchemes = true;

            // Subscribe to pairing actions
            pairingInputsP2.LocalControlPairing.P2Ready.performed += ctx => OnP2Ready(ctx);
            pairingInputsP2.LocalControlPairing.P2Cancel.performed += ctx => OnP2Cancel(ctx);

            if (playerInput.user.valid)
                playerInput.user.UnpairDevices();

            InputUser.PerformPairingWithDevice(DeviceManager.Instance.PlayerTwoDevice, playerInput.user);
            playerInput.SwitchCurrentControlScheme(DeviceManager.Instance.PlayerTwoDevice);

            pairingInputsP2.Enable();
        }
    }

    private void OnP1Ready(InputAction.CallbackContext ctx)
    {
        Debug.Log($"OnP1Ready called - Device: {ctx.control.device.displayName}, P1Device: {DeviceManager.Instance.PlayerOneDevice?.displayName ?? "null"}");

        // Verify input is from the paired device
        if (ctx.control.device != DeviceManager.Instance.PlayerOneDevice)
        {
            Debug.Log("OnP1Ready rejected - device mismatch");
            return;
        }

        if (p1Ready)
        {
            Debug.Log("OnP1Ready rejected - already ready");
            return;
        }

        Debug.Log("OnP1Ready accepted - setting p1Ready = true");
        p1Ready = true;
        p1ReadyButtonImage.gameObject.SetActive(false);
        playerOneLight.SetActive(true);
        p1ReadyButtonText.StringReference.TableEntryReference = "button_Readied";
        p1CancelButtonText.StringReference.TableEntryReference = "button_cancel";
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.Spotlight);
        if (p1Ready && p2Ready)
        {
            startButtonObject.SetActive(true);
            startButtonImage.sprite = DeviceManager.Instance.PlayerOneDevice is Keyboard ? uiData.spaceKey : uiData.buttonSouthKey;
        }
    }

    private void OnP1Cancel(InputAction.CallbackContext ctx)
    {
        Debug.Log($"OnP1Cancel called - Device: {ctx.control.device.displayName}, P1Device: {DeviceManager.Instance.PlayerOneDevice?.displayName ?? "null"}, p1Ready: {p1Ready}");

        // Verify input is from the paired device
        if (ctx.control.device != DeviceManager.Instance.PlayerOneDevice)
        {
            Debug.Log("OnP1Cancel rejected - device mismatch");
            return;
        }

        Debug.Log($"OnP1Cancel accepted - p1Ready is {p1Ready}");
        if (p1Ready)
        {
            p1Ready = false;
            p1ReadyButtonImage.gameObject.SetActive(true);
            playerOneLight.SetActive(false);
            p1ReadyButtonText.StringReference.TableEntryReference = "button_Ready";
            p1CancelButtonText.StringReference.TableEntryReference = "button_Unpair";
            startButtonObject.SetActive(false);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.SpotlightOff);
        }
        else
        {
            UnpairPlayer(PlayerNumber.Player1);
        }
    }

    private void OnP1Start(InputAction.CallbackContext ctx)
    {
        // Verify input is from the paired device
        if (ctx.control.device != DeviceManager.Instance.PlayerOneDevice)
            return;

        if (p1Ready && p2Ready)
            DOVirtual.DelayedCall(0f, () => StartGame());
    }

    private void OnP2Ready(InputAction.CallbackContext ctx)
    {
        // For shared keyboard: allow if from P1's device AND both are keyboards
        // For separate devices: must match P2's device
        bool isValidInput = AllowP2ControlFromP1Device()
            ? ctx.control.device == DeviceManager.Instance.PlayerOneDevice
            : ctx.control.device == DeviceManager.Instance.PlayerTwoDevice;

        if (!isValidInput)
            return;

        if (p2Ready)
            return;

        p2Ready = true;
        p2ReadyButtonImage.gameObject.SetActive(false);
        p2ReadyButtonText.StringReference.TableEntryReference = "button_Readied";
        p2CancelButtonText.StringReference.TableEntryReference = "button_cancel";
        playerTwoLight.SetActive(true);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.Spotlight);
        if (p1Ready && p2Ready)
        {
            startButtonObject.SetActive(true);
            startButtonImage.sprite = DeviceManager.Instance.PlayerOneDevice is Keyboard ? uiData.spaceKey : uiData.buttonSouthKey;
        }
    }

    private void OnP2Cancel(InputAction.CallbackContext ctx)
    {
        bool isSharedKeyboard = AllowP2ControlFromP1Device();
        Debug.Log($"OnP2Cancel called - Device: {ctx.control.device.displayName}, P2Device: {DeviceManager.Instance.PlayerTwoDevice?.displayName ?? "null"}, SharedKeyboard: {isSharedKeyboard}, p2Ready: {p2Ready}");

        bool isValidInput = isSharedKeyboard
            ? ctx.control.device == DeviceManager.Instance.PlayerOneDevice
            : ctx.control.device == DeviceManager.Instance.PlayerTwoDevice;

        if (!isValidInput)
        {
            Debug.Log("OnP2Cancel rejected - device mismatch");
            return;
        }

        Debug.Log($"OnP2Cancel accepted - p2Ready is {p2Ready}");
        if (p2Ready)
        {
            p2Ready = false;
            p2ReadyButtonImage.gameObject.SetActive(true);
            playerTwoLight.SetActive(false);
            p2ReadyButtonText.StringReference.TableEntryReference = "button_Ready";
            p2CancelButtonText.StringReference.TableEntryReference = "button_Unpair";
            startButtonObject.SetActive(false);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.SpotlightOff);
        }
        else
        {
            UnpairPlayer(PlayerNumber.Player2);
        }
    }
    private void UnpairPlayer(PlayerNumber playerNumber)
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.CancelWoosh);
        if (playerNumber == PlayerNumber.Player1)
        {
            // Check if P2 was using shared keyboard BEFORE we remove P1's device
            bool wasSharedKeyboard = AllowP2ControlFromP1Device();

            playerOneObject.SetActive(false);
            playerOneLight.SetActive(false);
            p1Ready = false;
            DeviceManager.Instance.RemovePlayerDevice(playerNumber);
            CleanupPairingInputs(playerNumber);

            unpairingInProgress = true;
            p1PairedDisplay.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
            {
                p1PairedDisplay.SetActive(false);
                playerOnePairer.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    unpairingInProgress = false;
                });
            });

            playerCount--;

            // If P2 was using shared keyboard, set up independent input for P2
            if (wasSharedKeyboard && DeviceManager.Instance.PlayerTwoDevice != null && pairingPlayerInputP2 != null)
            {
                SetUpIndependentP2Input();
            }

            startButtonObject.SetActive(false);

        }
        else if (playerNumber == PlayerNumber.Player2)
        {
            playerTwoObject.SetActive(false);
            playerTwoLight.SetActive(false);
            DeviceManager.Instance.RemovePlayerDevice(playerNumber);
            CleanupPairingInputs(playerNumber);

            unpairingInProgress = true;
            p2PairedDisplay.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
            {
                p2PairedDisplay.SetActive(false);
                playerTwoPairer.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    unpairingInProgress = false;
                });
            });
            playerCount--;
        }
    }

    private void SetUpIndependentP2Input()
    {
        if (pairingPlayerInputP2 == null)
            return;

        pairingInputsP2 = new InputActions_Hilltop();

        pairingPlayerInputP2.enabled = true;
        pairingPlayerInputP2.actions = pairingInputsP2.asset;
        pairingPlayerInputP2.defaultActionMap = "LocalControlPairing";
        pairingPlayerInputP2.notificationBehavior = PlayerNotifications.InvokeUnityEvents;

        // Disable automatic control scheme switching
        pairingPlayerInputP2.neverAutoSwitchControlSchemes = true;

        // Subscribe to pairing actions
        pairingInputsP2.LocalControlPairing.P2Ready.performed += ctx => OnP2Ready(ctx);
        pairingInputsP2.LocalControlPairing.P2Cancel.performed += ctx => OnP2Cancel(ctx);

        if (pairingPlayerInputP2.user.valid)
            pairingPlayerInputP2.user.UnpairDevices();

        InputUser.PerformPairingWithDevice(DeviceManager.Instance.PlayerTwoDevice, pairingPlayerInputP2.user);
        pairingPlayerInputP2.SwitchCurrentControlScheme(DeviceManager.Instance.PlayerTwoDevice);

        pairingInputsP2.Enable();
    }

    private void CleanupPairingInputs(PlayerNumber playerNumber)
    {
        if (playerNumber == PlayerNumber.Player1)
        {
            // Cleanup Player 1
            if (pairingInputsP1 != null)
            {
                pairingInputsP1.Disable();
                pairingInputsP1.Dispose();
                pairingInputsP1 = null;
            }

            if (pairingPlayerInputP1 != null)
            {
                // Unpair devices from the InputUser
                if (pairingPlayerInputP1.user.valid)
                    pairingPlayerInputP1.user.UnpairDevices();

                // Destroy the instantiated GameObject (which destroys PlayerInput too)
                Destroy(pairingPlayerInputP1.gameObject);
                pairingPlayerInputP1 = null;
            }
        }

        else if (playerNumber == PlayerNumber.Player2)
        {
            // Cleanup Player 2
            if (pairingInputsP2 != null)
            {
                pairingInputsP2.Disable();
                pairingInputsP2.Dispose();
                pairingInputsP2 = null;
            }

            if (pairingPlayerInputP2 != null)
            {
                if (pairingPlayerInputP2.user.valid)
                    pairingPlayerInputP2.user.UnpairDevices();

                Destroy(pairingPlayerInputP2.gameObject);
                pairingPlayerInputP2 = null;
            }
        }

        if (DeviceManager.Instance.PlayerOneDevice == null && DeviceManager.Instance.PlayerTwoDevice == null)
        {
            pairingInProgress = false;
        }
    }

    private void ClearPairing()
    {
        DeviceManager.Instance.RemovePlayerDevices();
        playerCount = 0;
    }

    void StartGame()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.StartGameStinger);
        m_EventListener.Dispose();
        CleanupPairingInputs(PlayerNumber.Player1);
        CleanupPairingInputs(PlayerNumber.Player2);
        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            gameObject.SetActive(false);
            startSceneCanvas.LoadScene();
        });
    }

    public void ExitLocalMultiPairing()
    {
        if (m_EventListener != null)
        {
            m_EventListener.Dispose();
        }

        DeviceManager.Instance.RemovePlayerDevices();
        playerCount = 0;
        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            gameObject.SetActive(false);

        });
    }
}
