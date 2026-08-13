using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using UnityEngine.UI;
using DG.Tweening;
using THP.Network;
using UnityEngine.EventSystems;
using FMODUnity;
using FMOD.Studio;

public class FriendListPanel : MonoBehaviour
{
    public static FriendListPanel Instance { get; private set; }

    public bool IsVisible => canvasGroup.alpha == 1;
    [SerializeField] GameObject friendInviter;
    [SerializeField] Transform friendListParent;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 originalPos;

    private List<SteamFriendInviter> friendInviters = new List<SteamFriendInviter>();
    EventInstance filmEventInstance;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        originalPos = ((RectTransform)transform).anchoredPosition;
    }

    void OnEnable()
    {
        ClearChildren(friendListParent);
        if (DemoManager.Instance != null && DemoManager.Instance.isLocalDemo)
        {
            return;
        }
        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendSteamID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            GameObject _nameObj = Instantiate(friendInviter, friendListParent);
            SteamFriendInviter steamFriendInviter = _nameObj.GetComponent<SteamFriendInviter>();
            steamFriendInviter.SetUpInviter(friendSteamID);
            steamFriendInviter.SetFriendListPanel(this);
            friendInviters.Add(steamFriendInviter);
        }

        // Sort by friend name alphabetically
        friendInviters.Sort((a, b) => string.Compare(a.GetFriendName(), b.GetFriendName(), System.StringComparison.OrdinalIgnoreCase));

        // Reorder the UI hierarchy to match the sorted list
        for (int i = 0; i < friendInviters.Count; i++)
        {
            friendInviters[i].transform.SetSiblingIndex(i);
        }

        StartCoroutine(WaitAndRebuild());
        InputManager.OnControlSchemeChanged += OnControlSchemeChange;
    }
    void OnDisable()
    {
        InputManager.OnControlSchemeChanged -= OnControlSchemeChange;
        if (filmEventInstance.isValid())
        {
            filmEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            filmEventInstance.release();
        }
    }
    IEnumerator WaitAndRebuild()
    {
        yield return new WaitForEndOfFrame();
        friendListParent.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(friendListParent.parent.GetComponent<RectTransform>().sizeDelta.x, friendListParent.GetComponent<RectTransform>().sizeDelta.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public void Show()
    {
        filmEventInstance = AudioManager.Instance.CreateInstance(FMODEvents.Instance.Film);
        filmEventInstance.start();
        canvasGroup.alpha = 1;
        rectTransform.DOAnchorPosY(0, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            filmEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            filmEventInstance.release();
            if (InputManager.Instance.CurrentControlScheme != "Keyboard")
            {
                EventSystem.current.SetSelectedGameObject(friendListParent.GetComponentsInChildren<Button>()[0].gameObject);
            }
        });
    }

    public void Hide()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.CancelWoosh);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.DOAnchorPosY(originalPos.y, 0.3f).SetEase(Ease.InCubic).OnComplete(() =>
        {
            canvasGroup.alpha = 0;
            OnCloseFriendListPanel();
            foreach (var inviter in friendInviters)
            {
                inviter.ResetButtonText();
            }
        });
    }

    private void OnCloseFriendListPanel()
    {
        Network_LobbyPlayer[] network_LobbyPlayers = FindObjectsOfType<Network_LobbyPlayer>();
        foreach (var player in network_LobbyPlayers)
        {
            player.OnCloseFriendListPanel();
            break;
        }
    }
    void ClearChildren(Transform parent)
    {
        if (parent.childCount == 0) return; // No children to clear
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnControlSchemeChange(string newScheme, string oldScheme)
    {
        if (IsVisible)
        {
            EventSystem.current.SetSelectedGameObject(newScheme == "Keyboard" ? null : friendListParent.GetComponentsInChildren<Button>()[0].gameObject);
        }
    }
}
