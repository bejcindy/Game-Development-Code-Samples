using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Steamworks;
using TMPro;
using NUnit.Framework;
using UnityEngine.Localization;
using DG.Tweening;
using FMODUnity;
using FMOD.Studio;

public class LeaderBoardCanvas : MonoBehaviour
{
    protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;
    public bool isOn;
    [Header("Components")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Transform leaderboardListUI;
    [SerializeField] GameObject leaderboardItemPrefab;
    [SerializeField] GameObject coopLeaderboardItemPrefab;
    [SerializeField] GameObject itemParentPrefab;
    [SerializeField] GameObject myEntryItem;
    [SerializeField] Color firstPlaceColor;
    [SerializeField] Color secondPlaceColor;
    [SerializeField] Color thirdPlaceColor;

    [Header("Dropdown UIs")]
    // [SerializeField] private CustomDropdown levelSelectDropdownUI;
    // [SerializeField] private CustomDropdown difficultyDropdownUI;
    [SerializeField] private TMP_Dropdown levelSelectDropdownUI;
    [SerializeField] private TMP_Dropdown difficultyDropdownUI;
    [SerializeField] private TMP_Dropdown coopDropdownUI;
    [SerializeField] private TextMeshProUGUI selectedLevelText;
    [SerializeField] private TextMeshProUGUI selectedDifficultyText;
    [SerializeField] private TextMeshProUGUI selectedCoopText;

    [Header("Localization")]
    [SerializeField] private LocalizedString[] levelNames;
    [SerializeField] private LocalizedString[] difficultyNames;
    [SerializeField] private LocalizedString[] coopNames;

    [SerializeField] private LEVEL_ID selectedLevel = LEVEL_ID.LEVEL_0;
    [SerializeField] private bool selectedCoop = false;
    [SerializeField] private DIFFICULTY selectedDifficulty = DIFFICULTY.NORMAL;

    [Header("Unused")]
    // [SerializeField] private ProgressBar death_pays_well_Bar;
    // [SerializeField] private ListView statsListUI;

    private CanvasGroup leaderboardCanvas;
    protected List<LeaderboardEntry> m_Leaderboard = new List<LeaderboardEntry>();
    float lowerYVaue = -1400f;
    EventInstance filmEventInstance;
    private void Awake()
    {
        InitializeComponents();
        leaderboardCanvas = GetComponent<CanvasGroup>();
        // leaderboardCanvas.alpha = 0;
    }

    void Start()
    {
        if (SteamManager.Initialized)
        {
            string name = SteamFriends.GetPersonaName();
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[LeaderBoardCanvas] Steam initialized. User: {name}");

            // RequestLeaderboardForLevel(LEVEL_ID.LEVEL_0, DIFFICULTY.NORMAL);
        }
        else
        {
            if (GameLogic.Instance.GetReleaseType() == RELEASE_TYPE.PRODUCTION_LOCAL)
            {
                gameObject.SetActive(false);
            }
            Debug.LogWarning("[LeaderBoardCanvas] Steam NOT initialized!");
        }

        InitializeLevelSelectDropdown();
        levelNames[0].StringChanged += (value) => LocalizeLevelSelectDropdownText();
        difficultyNames[0].StringChanged += (value) => LocalizeDifficultyDropdownText();
        coopNames[0].StringChanged += (value) => LocalizeCoopDropdownText();
    }

    private void OnEnable()
    {
        SteamEvents.OnLeaderboardResults += Event_OnLeaderboardResult;
        SteamEvents.OnStatsResults += Event_OnStatsResults;
    }

    private void OnDisable()
    {
        SteamEvents.OnLeaderboardResults -= Event_OnLeaderboardResult;
        SteamEvents.OnStatsResults -= Event_OnStatsResults;
        if (filmEventInstance.isValid())
        {
            filmEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            filmEventInstance.release();
        }
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void InitializeComponents()
    {
        if (mainMenu == null)
            Debug.LogWarning("[LeaderBoardCanvas] Main menu game object is null!");
        if (leaderboardCanvas == null)
            Debug.LogWarning("[LeaderBoardCanvas] Game overlay canvas can't be null");
        if (levelSelectDropdownUI == null)
            Debug.LogWarning("[LeaderBoardCanvas] Level Select Dropdown UI game object is null!");
        if (difficultyDropdownUI == null)
            Debug.LogWarning("[LeaderBoardCanvas] Difficulty Dropdown UI game object is null!");
        // if (statsListUI == null)
        //     Debug.LogWarning("[LeaderBoardCanvas] Stats scroll list is null!");
        if (leaderboardListUI == null)
            Debug.LogWarning("[LeaderBoardCanvas] Leaderboard scroll list is null!");
    }

    void InitializeLevelSelectDropdown()
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[LeaderBoardCanvas] Initializing dropdown with {GameLogic.Instance.TOTAL_LEVELS_COUNT} levels");

        // Clear existing options
        levelSelectDropdownUI.ClearOptions();
        // difficultyDropdownUI.ClearOptions();

        // Add level options
        List<string> levelOptions = new List<string>();
        for (int i = 0; i < GameLogic.Instance.TOTAL_LEVELS_COUNT + 1; ++i)
        {
            levelOptions.Add("Level: " + i.ToString());
        }
        levelSelectDropdownUI.AddOptions(levelOptions);

        // Add listeners
        // levelSelectDropdownUI.onValueChanged.AddListener(OnSelectLevelUI);
        // difficultyDropdownUI.onValueChanged.AddListener(OnSelectDifficultyUI);
    }

    void LocalizeLevelSelectDropdownText()
    {
        List<string> localizedLevelOptions = new List<string>();
        for (int i = 0; i < GameLogic.Instance.TOTAL_LEVELS_COUNT; ++i)
        {
            if (i < levelNames.Length)
            {
                localizedLevelOptions.Add(levelNames[i].GetLocalizedString());
            }
            else
            {
                localizedLevelOptions.Add("Level: " + i.ToString());
            }
        }

        int currentSelection = levelSelectDropdownUI.value;
        levelSelectDropdownUI.ClearOptions();
        levelSelectDropdownUI.AddOptions(localizedLevelOptions);
        levelSelectDropdownUI.value = currentSelection;

        if (currentSelection < levelNames.Length)
        {
            selectedLevelText.text = levelNames[currentSelection].GetLocalizedString();
        }
    }

    void LocalizeDifficultyDropdownText()
    {
        List<string> localizedDifficultyOptions = new List<string>();
        for (int i = 0; i < difficultyNames.Length; ++i)
        {
            localizedDifficultyOptions.Add(difficultyNames[i].GetLocalizedString());
        }

        int currentSelection = difficultyDropdownUI.value;
        difficultyDropdownUI.ClearOptions();
        difficultyDropdownUI.AddOptions(localizedDifficultyOptions);
        difficultyDropdownUI.value = currentSelection;

        if (currentSelection < difficultyNames.Length)
        {
            selectedDifficultyText.text = difficultyNames[currentSelection].GetLocalizedString();
        }
    }

    void LocalizeCoopDropdownText()
    {
        List<string> localizedCoopOptions = new List<string>();
        for (int i = 0; i < coopNames.Length; ++i)
        {
            localizedCoopOptions.Add(coopNames[i].GetLocalizedString());
        }

        int currentSelection = coopDropdownUI.value;
        coopDropdownUI.ClearOptions();
        coopDropdownUI.AddOptions(localizedCoopOptions);
        coopDropdownUI.value = currentSelection;

        if (currentSelection < coopNames.Length)
        {
            selectedCoopText.text = coopNames[currentSelection].GetLocalizedString();
        }
    }

    bool updatingLeaderboardUI;
    private IEnumerator UpdateLeaderBoardUI()
    {
        if (updatingLeaderboardUI)
            yield break;
        updatingLeaderboardUI = true;
        if (HilltopConstants.DEBUG_STEAM_API)
        {
            Debug.Log($"[LeaderBoardCanvas] Updating UI for {selectedLevel}: {m_Leaderboard?.Count ?? 0} entries");
        }
        //loading animation is played in RequestLeaderboardForLevel, as well as destroying old list


        GameObject newItemParent = Instantiate(itemParentPrefab, leaderboardListUI);
        GameObject itemPrefabToUse = selectedCoop ? coopLeaderboardItemPrefab : leaderboardItemPrefab;
        if (m_Leaderboard != null)
        {
            for (int i = 0; i < m_Leaderboard.Count; i++)
            {
                if (m_Leaderboard[i].userID.m_SteamID > 0)
                {
                    // ListView.ListItem item = CreateLeaderboardListItem(i);
                    // leaderboardListUI.listItems.Add(item);

                    GameObject newItem = Instantiate(itemPrefabToUse, newItemParent.transform);
                    newItem.transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = "#" + m_Leaderboard[i].rank;

                    if (m_Leaderboard[i].rank == 1)
                    {
                        newItem.transform.GetChild(newItem.transform.childCount - 1).GetComponent<Image>().color = firstPlaceColor;
                        newItem.transform.GetChild(newItem.transform.childCount - 1).gameObject.SetActive(true);
                    }
                    else if (m_Leaderboard[i].rank == 2)
                    {
                        newItem.transform.GetChild(newItem.transform.childCount - 1).GetComponent<Image>().color = secondPlaceColor;
                        newItem.transform.GetChild(newItem.transform.childCount - 1).gameObject.SetActive(true);
                    }
                    else if (m_Leaderboard[i].rank == 3)
                    {
                        newItem.transform.GetChild(newItem.transform.childCount - 1).GetComponent<Image>().color = thirdPlaceColor;
                        newItem.transform.GetChild(newItem.transform.childCount - 1).gameObject.SetActive(true);
                    }
                    newItem.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = m_Leaderboard[i].personalName;
                    if (selectedCoop)
                        newItem.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = m_Leaderboard[i].friendName;
                    string gradeObjName = GetGradeRating(m_Leaderboard[i].highScore);
                    newItem.transform.GetChild(2).Find(gradeObjName).gameObject.SetActive(true);
                    newItem.transform.GetChild(3).GetComponentInChildren<TextMeshProUGUI>().text = FormatTimeDisplay(m_Leaderboard[i].time);
                }
            }
        }
        else
        {
            Debug.LogWarning("[LeaderBoardCanvas] m_Leaderboard is NULL!");
        }

        //after everything is loaded, set active (prefab default to inactive)
        leaderboardListUI.GetChild(0).gameObject.SetActive(false);
        newItemParent.SetActive(true);
        Debug.Log("Finished Loading Leaderboard.");

        HighlightPersonalEntry();
    }

    private void HighlightPersonalEntry()
    {
        if (leaderboardListUI.GetChild(1).childCount == 0)
        {
            myEntryItem.transform.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = "-";
            myEntryItem.transform.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text = SteamFriends.GetPersonaName();
            foreach (Transform child in myEntryItem.transform.GetChild(3))
            {
                child.gameObject.SetActive(false);
            }
            myEntryItem.transform.GetChild(3).Find("NoScore").gameObject.SetActive(true);
            myEntryItem.transform.GetChild(4).GetComponentInChildren<TextMeshProUGUI>().text = "- -";
            updatingLeaderboardUI = false;
            return;
        }

        for (int i = 0; i < leaderboardListUI.GetChild(1).childCount; i++)
        {
            if (m_Leaderboard[i].mine)
            {
                Transform itemObject = leaderboardListUI.GetChild(1).GetChild(i);
                itemObject.name = "PlayerEntry";
                if (itemObject != null)
                {
                    // Highlight it - change background color
                    Image bg = itemObject.GetChild(itemObject.childCount - 2).GetComponent<Image>();
                    if (bg != null)
                    {
                        bg.enabled = true;
                    }
                }
                myEntryItem.transform.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = "#" + m_Leaderboard[i].rank;
                Debug.Log("My Rank: " + m_Leaderboard[i].rank);
                if (m_Leaderboard[i].rank == 1)
                {
                    myEntryItem.transform.GetChild(myEntryItem.transform.childCount - 1).GetComponent<Image>().color = firstPlaceColor;
                    myEntryItem.transform.GetChild(myEntryItem.transform.childCount - 1).gameObject.SetActive(true);
                }
                else if (m_Leaderboard[i].rank == 2)
                {
                    myEntryItem.transform.GetChild(myEntryItem.transform.childCount - 1).GetComponent<Image>().color = secondPlaceColor;
                    myEntryItem.transform.GetChild(myEntryItem.transform.childCount - 1).gameObject.SetActive(true);
                }
                else if (m_Leaderboard[i].rank == 3)
                {
                    myEntryItem.transform.GetChild(myEntryItem.transform.childCount - 1).GetComponent<Image>().color = thirdPlaceColor;
                    myEntryItem.transform.GetChild(myEntryItem.transform.childCount - 1).gameObject.SetActive(true);
                }
                else
                {
                    myEntryItem.transform.GetChild(myEntryItem.transform.childCount - 1).gameObject.SetActive(false);
                }
                // myEntryItem.transform.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text = m_Leaderboard[i].personalName;
                foreach (Transform child in myEntryItem.transform.GetChild(3))
                {
                    child.gameObject.SetActive(false);
                }
                string gradeObjName = GetGradeRating(m_Leaderboard[i].highScore);
                myEntryItem.transform.GetChild(3).Find(gradeObjName).gameObject.SetActive(true);
                myEntryItem.transform.GetChild(4).GetComponentInChildren<TextMeshProUGUI>().text = FormatTimeDisplay(m_Leaderboard[i].time);
                break;
            }
        }
        myEntryItem.transform.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text = SteamFriends.GetPersonaName();
        updatingLeaderboardUI = false;
    }

    private bool ShouldDisplayStat(STATS_ID_STEAMWORKS statsId)
    {
        string statName = statsId.ToString();
        return statName == "TOTAL_DISTANCE" || statName == "TOTAL_COINS" || statName == "TOTAL_DROPS";
    }


    string GetGradeRating(int highScore)
    {
        return highScore switch
        {
            4 => "SS",
            3 => "S",
            2 => "A",
            1 => "B",
            0 => "C",
            _ => "NoScore"
        };
    }

    private string FormatTimeDisplay(float rawTime)
    {
        int hours = (int)rawTime / 3600;
        int minutes = ((int)rawTime % 3600) / 60;
        int seconds = (int)rawTime % 60;

        if (hours > 0)
            return $"{hours}h {minutes}min {seconds}s";
        else
            return $"{minutes}min {seconds}s";
    }

    private void Event_OnLeaderboardResult(List<LeaderboardEntry> r_Leaderboard, LEVEL_ID levelID)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
        {
            Debug.Log($"[LeaderBoardCanvas] Received leaderboard for {levelID}: {r_Leaderboard?.Count ?? 0} entries");
        }

        if (r_Leaderboard == null)
        {
            Debug.LogError("[LeaderBoardCanvas] Received NULL leaderboard list!");
            return;
        }

        if (selectedLevel == levelID)
        {
            m_Leaderboard = r_Leaderboard;
            StartCoroutine(UpdateLeaderBoardUI());
        }
        else if (HilltopConstants.DEBUG_STEAM_API)
        {
            Debug.LogWarning($"[LeaderBoardCanvas] Level mismatch: expected {selectedLevel}, got {levelID}");
        }
    }

    private void Event_OnStatsResults()
    {
        // UpdateStatsScrollUI();
    }


    private void DisableLeaderboardMenu()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.CancelWoosh);
        if (InputManager.Instance.CurrentControlScheme != "Keyboard")
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        // leaderboardCanvas.alpha = 0;
        leaderboardCanvas.GetComponent<RectTransform>().DOAnchorPosY(lowerYVaue, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            isOn = false;
        });
        leaderboardCanvas.interactable = false;
        leaderboardCanvas.blocksRaycasts = false;
    }

    private void EnableLeaderboardMenu()
    {
        isOn = true;
        // leaderboardCanvas.alpha = 1;
        filmEventInstance = AudioManager.Instance.CreateInstance(FMODEvents.Instance.Film);
        filmEventInstance.start();
        leaderboardCanvas.GetComponent<RectTransform>().DOAnchorPosY(0, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            if (InputManager.Instance.CurrentControlScheme == "Keyboard")
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            filmEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            filmEventInstance.release();
        });
        leaderboardCanvas.interactable = true;
        leaderboardCanvas.blocksRaycasts = true;
    }

    private void RequestLeaderboardForLevel(LEVEL_ID levelNumber, DIFFICULTY difficulty, bool coop = false)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[LeaderBoardCanvas] Requesting leaderboard for {levelNumber}{difficulty}{coop}");

        //Play Loading Animation
        leaderboardListUI.GetChild(0).gameObject.SetActive(true);
        if (leaderboardListUI.childCount > 1)
        {
            Debug.Log("trying to destroy old list");
            Destroy(leaderboardListUI.GetChild(1).gameObject);
        }


        SteamEvents.Event_RequestLeaderboardForLevel(levelNumber, difficulty, coop);
    }

    public void EnterLeaderBoardMenu()
    {
        EnableLeaderboardMenu();

        if (SteamManager.Initialized)
        {
            RequestLeaderboardForLevel(LEVEL_ID.LEVEL_0, DIFFICULTY.NORMAL, false);
        }
    }

    public void ExitLeaderBoardMenu()
    {
        DisableLeaderboardMenu();
    }

    public void StatsSelected()
    {
        if (GameLogic.Instance.steamServiceState == SteamServiceState.Running)
        {
            // Reserved for future implementation
        }
    }

    public void LeaderboardSelected()
    {
        // Reserved for future implementation
    }

    public void AchievementsSelected()
    {
        int coinCount = GameLogic.Instance.GetTotalCoinProgressForAllLevels();
        // death_pays_well_Bar.currentPercent = (float)coinCount;
        // death_pays_well_Bar.isOn = false;
    }


    public void OnSelectLevelUI(int level)
    {
        selectedLevel = (LEVEL_ID)levelSelectDropdownUI.value;

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[LeaderBoardCanvas] Selected {selectedLevel}");

        // RequestLeaderboardForLevel(selectedLevel);
        Debug.Log("Selected Level: " + selectedLevel + "Selected Difficulty: " + selectedDifficulty);
        //TODO: CHANGE DIFFICULTY SELECTION BUTTON FUNCTION TO CORRESPONDING LEVEL
        RequestLeaderboardForLevel(selectedLevel, selectedDifficulty, selectedCoop);

    }

    public void OnSelectDifficultyUI(int difficulty)
    {
        selectedDifficulty = (DIFFICULTY)difficulty;

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[LeaderBoardCanvas] Selected Difficulty: {selectedDifficulty}");
        Debug.Log("Selected Level: " + selectedLevel + "Selected Difficulty: " + selectedDifficulty);
        RequestLeaderboardForLevel(selectedLevel, selectedDifficulty, selectedCoop);
    }

    public void OnSelectCoopUI(int coop)
    {
        selectedCoop = coop == 1;
        RequestLeaderboardForLevel(selectedLevel, selectedDifficulty, selectedCoop);
    }


    public void DeselectButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }



}