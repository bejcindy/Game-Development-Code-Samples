using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Steamworks;
using UnityEngine.UI;
using DG.Tweening;
using THP.Network;
using UnityEngine.Localization.Components;

public class SteamFriendInviter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI friendName;
    [SerializeField] private Button inviteButton;
    [SerializeField] private LocalizeStringEvent buttonText;

    private FriendListPanel friendListPanel;

    public void SetUpInviter(CSteamID friendID)
    {
        friendName.text = SteamFriends.GetFriendPersonaName(friendID);
        inviteButton.onClick.AddListener(() => InviteFriend(friendID));
    }

    public void InviteFriend(CSteamID friendID)
    {
        SteamMatchmaking.InviteUserToLobby(SteamLobby.currentLobbyID, friendID);
        buttonText.StringReference.TableEntryReference = "button_Invited";
        inviteButton.interactable = false;
        friendListPanel.Hide();
    }

    public void ResetButtonText()
    {
        buttonText.StringReference.TableEntryReference = "button_Invite";
        inviteButton.interactable = true;
    }

    public void SetFriendListPanel(FriendListPanel friendPanel)
    {
        friendListPanel = friendPanel;
    }

    public string GetFriendName()
    {
        return friendName.text;
    }
}
