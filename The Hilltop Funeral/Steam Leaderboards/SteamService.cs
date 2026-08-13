using UnityEngine;
using System.Collections.Generic;
using Steamworks;

// Used Steamworks Template from https://steamworks.github.io/

internal class MonoPInvokeCallbackAttribute : System.Attribute
{
    public MonoPInvokeCallbackAttribute() { }
}

public class SteamService : MonoBehaviour
{
    #region Private Fields

    private bool m_bInitialized = false;
    public static SteamService m_SteamService = null;
    public LEVEL_ID requestedLevel = LEVEL_ID.LEVEL_0;

    private SteamAPIWarningMessageHook_t SteamAPIWarningMessageHook;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (!InitializeSingleton())
            return;

        if (!DemoManager.Instance.isLocalDemo)
            InitializeSteamAPI();
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("SteamService Start");
    }

    private void OnEnable()
    {
        // Handle assembly reload
        if (m_SteamService == null)
            m_SteamService = this;

        if (!m_bInitialized)
            return;

        RegisterEventCallbacks();
        SetupWarningMessageHook();
        GameLogic.Instance.NotifySteamServiceLoaded();

        if (GameLogic.Instance.steamServiceState == SteamServiceState.Running)
            SteamEvents.Event_RequestSteamworksStats();
    }

    protected virtual void OnDisable()
    {
        UnregisterEventCallbacks();
    }

    private void Update()
    {
        if (!m_bInitialized)
            return;

        SteamAPI.RunCallbacks();
    }

    private void OnDestroy()
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("Steam Service Destroyed");

        if (!m_bInitialized)
            return;

        SteamAPI.Shutdown();
    }

    private void OnGUI()
    {
        if (!m_bInitialized && HilltopConstants.DEBUG_STEAM_API)
        {
            GUILayout.Label("Steamworks is not Initialized");
        }
    }

    #endregion

    #region Initialization

    private bool InitializeSingleton()
    {
        if (m_SteamService != null)
        {
            Destroy(gameObject);
            return false;
        }

        m_SteamService = this;
        DontDestroyOnLoad(gameObject);
        return true;
    }

    private void InitializeSteamAPI()
    {
        if (!ValidateSteamAPI())
            return;

        try
        {
            m_bInitialized = SteamAPI.Init();
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError($"[Steamworks] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n{e}", this);
            Application.Quit();
            return;
        }

        if (!m_bInitialized)
        {
            Debug.LogError("SteamAPI_Init() failed", this);
            return;
        }

        SetupWarningMessageHook();
    }

    private bool ValidateSteamAPI()
    {
        if (!Packsize.Test())
        {
            throw new System.Exception("Packsize is wrong! You are likely using a Linux/OSX build on Windows or vice versa.");
        }

        if (!DllCheck.Test())
        {
            throw new System.Exception("DllCheck returned false.");
        }

        return true;
    }

    private void SetupWarningMessageHook()
    {
        if (SteamAPIWarningMessageHook == null)
        {
            SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
            SteamClient.SetWarningMessageHook(SteamAPIWarningMessageHook);
        }
    }

    [MonoPInvokeCallback]
    private static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
    {
        Debug.LogWarning(pchDebugText);
    }

    #endregion

    #region Event Registration

    private void RegisterEventCallbacks()
    {
        // Game events
        EventsMaster.OnLevelComplete += Event_OnLevelComplete;
        EventsMaster.RequestSaveLevelData += Event_RequestSaveLevelData;
        EventsMaster.OnCollectableCollect += Event_OnItemCollected;

        // Steam events
        SteamEvents.SteamworksConnected += Event_SteamworksConnected;
        SteamEvents.SteamOverlaySet += Event_SteamOverlaySet;
        SteamEvents.OnLeaderboardLoaded += Event_OnLeaderboardLoaded;
        SteamEvents.RequestLeaderboardForLevel += Event_RequestLeaderboardForLevel;
        SteamEvents.LevelCompleteSteamworks += Event_OnLevelCompleteSteamworks;
        SteamEvents.IncrementSteamworksStat += Event_IncrementSteamworksStat;
        SteamEvents.UpdateSteamworksStat += Event_UpdateSteamworksStat;
        SteamEvents.RequestSteamworksStats += Event_RequestSteamworksStats;
    }

    private void UnregisterEventCallbacks()
    {
        // Game events
        EventsMaster.OnLevelComplete -= Event_OnLevelComplete;
        EventsMaster.RequestSaveLevelData -= Event_RequestSaveLevelData;
        EventsMaster.OnCollectableCollect -= Event_OnItemCollected;

        // Steam events
        SteamEvents.SteamworksConnected -= Event_SteamworksConnected;
        SteamEvents.SteamOverlaySet -= Event_SteamOverlaySet;
        SteamEvents.OnLeaderboardLoaded -= Event_OnLeaderboardLoaded;
        SteamEvents.RequestLeaderboardForLevel -= Event_RequestLeaderboardForLevel;
        SteamEvents.IncrementSteamworksStat -= Event_IncrementSteamworksStat;
        SteamEvents.UpdateSteamworksStat -= Event_UpdateSteamworksStat;
        SteamEvents.LevelCompleteSteamworks -= Event_OnLevelCompleteSteamworks;
        SteamEvents.RequestSteamworksStats -= Event_RequestSteamworksStats;
    }

    #endregion

    #region Leaderboard API

    /// <summary>
    /// Loads the leaderboard for the specified level.
    /// </summary>
    public void LoadLeaderboardForCurrentLevel(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        SteamLeaderboards leaderboardManager = GetLeaderboardManager();
        if (leaderboardManager == null)
            return;

        List<LeaderboardEntry> leaderboardList = leaderboardManager.GetLeaderboardEntriesForLevel(levelID, difficulty, coop);
        if (leaderboardList != null)
        {
            SteamEvents.Event_OnLeaderboardResult(leaderboardList, levelID);
        }
    }

    /// <summary>
    /// Uploads the completion time for the specified level.
    /// </summary>
    public void UploadTimeForCurrentLevel(int levelTimeMs, LEVEL_ID levelID, DIFFICULTY difficulty = DIFFICULTY.NORMAL, ulong friendID = 0)
    {
        SteamLeaderboards leaderboardManager = GetLeaderboardManager();
        if (leaderboardManager == null)
            return;

        Debug.Log($"[SteamService] Uploading time to leaderboard for level {levelID} ({difficulty}): {levelTimeMs}ms");

        bool coop;
        coop = friendID == 0 ? false : true;

        // Ensure leaderboard is loaded first - this will trigger a find if not already cached
        LoadLeaderboardForCurrentLevel(levelID, difficulty, coop);

        // Get the high score for this level
        LevelData levelData = GameLogic.Instance.GetLevelData(levelID, difficulty, friendID);
        int highScore = levelData?.highScore ?? 0;

        // Get leaderboard handle - might be invalid if not found yet
        SteamLeaderboard_t leaderboard = leaderboardManager.GetLeaderboardForLevel(levelID, difficulty, coop);

        if (leaderboard.m_SteamLeaderboard == 0)
        {
            Debug.LogWarning($"[SteamService] Leaderboard not ready for {levelID} ({difficulty}), will upload after it's found");
            // The upload will happen automatically via CheckAndSyncLocalBestTime after leaderboard is found
            return;
        }

        // Pass the levelID and high score
        leaderboardManager.UploadTimeToLeaderboard(
            levelTimeMs,
            leaderboard,
            levelID,
            difficulty,
            highScore,
            friendID
        );
    }

    /// <summary>
    /// Downloads global leaderboard entries for the specified level.
    /// </summary>
    // public void DownloadLeaderboardEntriesGlobal(int nLevel)
    // {
    //     SteamLeaderboards leaderboardManager = GetLeaderboardManager();
    //     if (leaderboardManager == null)
    //         return;

    //     if (nLevel == (int)requestedLevel)
    //     {
    //         leaderboardManager.DownloadLeaderboardEntriesForCurrentLeaderboard(1, 5);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("You are trying to download leaderboard stats before it was found.");
    //     }
    // }

    /// <summary>
    /// Requests leaderboard data and downloads entries for the specified level.
    /// </summary>
    public void RequestLeaderboardAndDownloadEntries(LEVEL_ID levelID, DIFFICULTY difficulty)
    {
        requestedLevel = levelID;
        LoadLeaderboardForCurrentLevel(levelID, difficulty);
    }

    /// <summary>
    /// Gets all leaderboard entries for the specified level.
    /// </summary>
    public List<LeaderboardEntry> GetLeaderboardEntries(LEVEL_ID levelID, DIFFICULTY difficulty)
    {
        SteamLeaderboards leaderboardManager = GetLeaderboardManager();
        if (leaderboardManager == null)
            return null;

        return leaderboardManager.GetLeaderboardEntriesForLevel(levelID, difficulty);
    }

    #endregion

    #region Stats API

    /// <summary>
    /// Gets an integer stat value.
    /// </summary>
    public int GetStat(STATS_ID_STEAMWORKS stat)
    {
        SteamStats statsManager = GetStatsManager();
        if (statsManager != null)
        {
            return statsManager.GetStat(stat);
        }

        Debug.LogError("Steam Stats is null!");
        return 0;
    }

    /// <summary>
    /// Gets a float stat value.
    /// </summary>
    public float GetStatFloat(STATS_ID_STEAMWORKS stat)
    {
        SteamStats statsManager = GetStatsManager();
        if (statsManager != null)
        {
            return statsManager.GetStatFloat(stat);
        }

        Debug.LogError("Steam Stats is null!");
        return 0;
    }

    /// <summary>
    /// Updates a stat with a specific value. (Reserved for future use)
    /// </summary>
    public void UpdateSteamworksStat(STATS_ID_STEAMWORKS statsID, int amount)
    {
        // Implementation reserved for future use
    }

    /// <summary>
    /// Increments a stat by 1. (Reserved for future use)
    /// </summary>
    public void IncrementSteamworksStat(STATS_ID_STEAMWORKS statsID)
    {
        // Implementation reserved for future use
    }

    /// <summary>
    /// Updates the fail count for a specific level.
    /// </summary>
    public void UpdateFails(LEVEL_ID levelId, int failCount)
    {
        SteamStats statsManager = GetStatsManager();
        if (statsManager != null)
        {
            statsManager.UpdateFails(levelId, failCount);
        }
        else
        {
            Debug.LogError("Steam Stats is null!");
        }
    }

    /// <summary>
    /// Updates the win count for a specific level.
    /// </summary>
    public void UpdateWins(LEVEL_ID levelId, int winCount)
    {
        SteamStats statsManager = GetStatsManager();
        if (statsManager != null)
        {
            statsManager.UpdateWins(levelId, winCount);
        }
        else
        {
            Debug.LogError("Steam Stats is null!");
        }
    }

    /// <summary>
    /// Updates the total time played for a specific level.
    /// </summary>
    public void UpdateLevelTime(LEVEL_ID levelId, float levelTime)
    {
        SteamStats statsManager = GetStatsManager();
        if (statsManager != null)
        {
            statsManager.UpdateLevelTime(levelId, levelTime);
        }
        else
        {
            Debug.LogError("Steam Stats is null!");
        }
    }

    public void UpdateHighScore(LEVEL_ID levelId, DIFFICULTY difficulty, int highScore)
    {
        SteamStats statsManager = GetStatsManager();
        if (statsManager != null)
        {
            STATS_ID_STEAMWORKS statId = GetHighScoreStat(levelId, difficulty);
            int currentHighScore = statsManager.GetStat(statId);

            // Only update if new score is higher
            if (highScore > currentHighScore)
            {
                statsManager.UpdateStat(statId, highScore);
                SyncStatsToSteam();
            }
        }
    }

    private STATS_ID_STEAMWORKS GetHighScoreStat(LEVEL_ID levelID, DIFFICULTY difficulty)
    {
        return difficulty switch
        {
            DIFFICULTY.EASY => levelID switch
            {
                LEVEL_ID.LEVEL_0 => STATS_ID_STEAMWORKS.LEVEL_0_EASY_HIGHSCORE,
                LEVEL_ID.LEVEL_1 => STATS_ID_STEAMWORKS.LEVEL_1_EASY_HIGHSCORE,
                LEVEL_ID.LEVEL_2 => STATS_ID_STEAMWORKS.LEVEL_2_EASY_HIGHSCORE,
                LEVEL_ID.LEVEL_3 => STATS_ID_STEAMWORKS.LEVEL_3_EASY_HIGHSCORE,
                LEVEL_ID.LEVEL_4 => STATS_ID_STEAMWORKS.LEVEL_4_EASY_HIGHSCORE,
                _ => throw new System.ArgumentException($"Unsupported level ID: {levelID}")
            },
            DIFFICULTY.HARD => levelID switch
            {
                LEVEL_ID.LEVEL_0 => STATS_ID_STEAMWORKS.LEVEL_0_HARD_HIGHSCORE,
                LEVEL_ID.LEVEL_1 => STATS_ID_STEAMWORKS.LEVEL_1_HARD_HIGHSCORE,
                LEVEL_ID.LEVEL_2 => STATS_ID_STEAMWORKS.LEVEL_2_HARD_HIGHSCORE,
                LEVEL_ID.LEVEL_3 => STATS_ID_STEAMWORKS.LEVEL_3_HARD_HIGHSCORE,
                LEVEL_ID.LEVEL_4 => STATS_ID_STEAMWORKS.LEVEL_4_HARD_HIGHSCORE,
                _ => throw new System.ArgumentException($"Unsupported level ID: {levelID}")
            },
            DIFFICULTY.NORMAL => levelID switch
            {
                LEVEL_ID.LEVEL_0 => STATS_ID_STEAMWORKS.LEVEL_0_HIGHSCORE,
                LEVEL_ID.LEVEL_1 => STATS_ID_STEAMWORKS.LEVEL_1_HIGHSCORE,
                LEVEL_ID.LEVEL_2 => STATS_ID_STEAMWORKS.LEVEL_2_HIGHSCORE,
                LEVEL_ID.LEVEL_3 => STATS_ID_STEAMWORKS.LEVEL_3_HIGHSCORE,
                LEVEL_ID.LEVEL_4 => STATS_ID_STEAMWORKS.LEVEL_4_HIGHSCORE,
                _ => throw new System.ArgumentException($"Unsupported level ID: {levelID}")
            },
            _ => throw new System.ArgumentException($"Unsupported difficulty: {difficulty}")
        };

    }

    #endregion

    #region Helper Methods    
    private SteamLeaderboards GetLeaderboardManager()
    {
        SteamLeaderboards manager = GameLogic.Instance.GetSteamLeaderboardInstance();
        if (manager == null)
        {
            Debug.LogError("SteamworksLeaderboardsManager is null!");
            Debug.Log($"m_bInitialized: {m_bInitialized}");
        }
        return manager;
    }

    private SteamStats GetStatsManager()
    {
        return GameLogic.Instance.GetSteamStatsInstance();
    }

    /// <summary>
    /// For testing purposes only. Gets the Steam leaderboard instance.
    /// </summary>
    public SteamLeaderboards TESTONLY_GET_SteamStats()
    {
        SteamLeaderboards stats = GameLogic.Instance.GetSteamLeaderboardInstance();
        if (stats != null)
        {
            return stats;
        }

        Debug.LogError("[Steamworks] Steam Stats is not initialized.");
        return null;
    }

    /// <summary>
    /// Utility method to print array contents to console.
    /// </summary>
    public static void PrintArray(string name, System.Collections.IList arr)
    {
        System.Text.StringBuilder strBuilder = new System.Text.StringBuilder(name + '\n');

        for (int i = 0; i < arr.Count; ++i)
        {
            strBuilder.AppendLine(arr[i].ToString());
        }

        print(strBuilder);
    }

    #endregion

    #region Event Handlers

    private void Event_OnItemCollected(CollectibleType type)
    {
        if (!m_bInitialized)
        {
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log("Event_OnItemCollected: Steam not initialized");
            return;
        }

        SteamStats statsManager = GetStatsManager();
        if (statsManager == null)
        {
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log("Event_OnItemCollected: Stats manager is null");
            return;
        }

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"Event_OnItemCollected: Processing collectible of type {type}");

        if (type == CollectibleType.Cloth || type == CollectibleType.Nail || type == CollectibleType.Rope || type == CollectibleType.Wood)
        {
            bool success = statsManager.IncrementStat(STATS_ID_STEAMWORKS.TOTAL_COLLECTABLE_COUNT);
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"Event_OnItemCollected: Incrementing TOTAL_COLLECTABLE_COUNT stat");
            SyncStatsToSteam();
        }

        if (type == CollectibleType.Coin)
        {
            bool success = statsManager.IncrementStat(STATS_ID_STEAMWORKS.TOTAL_COINS);
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"Event_OnItemCollected: Incrementing TOTAL_COINS stat");
            SyncStatsToSteam();
        }
        if (type == CollectibleType.GoldCoin)
        {
            bool success = statsManager.IncrementStat(STATS_ID_STEAMWORKS.GOLD_COINS);
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"Event_OnItemCollected: Incrementing GOLD_COINS stat - Success: {success}");
            SyncStatsToSteam();
        }
    }

    private void Event_UpdateSteamworksStat(STATS_ID_STEAMWORKS statsID, int amount)
    {
        SteamStats statsManager = GetStatsManager();
        if (statsManager != null)
        {
            statsManager.UpdateStat(statsID, amount);
        }
    }

    private void Event_IncrementSteamworksStat(STATS_ID_STEAMWORKS statsID)
    {
        SteamStats statsManager = GetStatsManager();
        if (statsManager != null)
        {
            statsManager.IncrementStat(statsID);
        }
    }

    private void Event_RequestSaveLevelData(LevelData levelData)
    {
        // Update stats from level save data
    }

    private void Event_SteamOverlaySet(bool isActive, SteamService steamService)
    {
        // Handle Steam overlay state changes
    }

    private void Event_SteamworksConnected(bool connected)
    {
        // Handle Steamworks connection state
    }

    private void Event_OnLeaderboardLoaded(LeaderboardFindResult_t pCallback, LEVEL_ID levelID, DIFFICULTY difficulty, bool failed, bool coop = false)
    {
        if (failed)
            return;

        SteamLeaderboards leaderboardManager = GetLeaderboardManager();
        if (leaderboardManager != null)
        {
            leaderboardManager.DownloadLeaderboardEntriesForLeaderboard(1, leaderboardManager.maxEntries, levelID, difficulty, coop);
        }
    }

    private void Event_RequestLeaderboardForLevel(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        LoadLeaderboardForCurrentLevel(levelID, difficulty, coop);
    }

    private void Event_OnLevelComplete()
    {
        // Handle level completion
    }

    /// <summary>
    /// Updates Steam stats based on the level data from a completed level.
    /// </summary>
    private void Event_OnLevelCompleteSteamworks(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogWarning("LevelData is null, cannot update Steam stats");
            return;
        }

        SteamStats statsManager = GetStatsManager();
        if (statsManager == null)
        {
            Debug.LogError("Steam Stats Manager is null!");
            return;
        }

        LEVEL_ID levelID = (LEVEL_ID)levelData.level;

        // Update completion counts
        if (levelData.completed)
        {
            statsManager.UpdateWins(levelID, levelData.completedCount);
            TriggerLevelCompletionAchievements(levelData);
        }

        // Update failure counts
        statsManager.UpdateFails(levelID, levelData.restartCount);
        // Update level time
        statsManager.UpdateLevelTime(levelID, levelData.totalLevelTime);
        // Update game-wide stats

        // Increment total games played
        statsManager.IncrementStat(STATS_ID_STEAMWORKS.TOTAL_GAMES_PLAYED);

        // Update coins collected
        if (levelData.coinCount > 0)
        {
            float currentCoins = statsManager.GetStatFloat(STATS_ID_STEAMWORKS.TOTAL_COINS);
            statsManager.UpdateStat(STATS_ID_STEAMWORKS.TOTAL_COINS, currentCoins + levelData.coinCount);
        }

        // Update total drops
        if (levelData.totalDropCount > 0)
        {
            float currentDrops = statsManager.GetStatFloat(STATS_ID_STEAMWORKS.TOTAL_DROPS);
            statsManager.UpdateStat(STATS_ID_STEAMWORKS.TOTAL_DROPS, currentDrops + levelData.totalDropCount);
        }

        // Update body decay/damage
        if (levelData.bodyDecay > 0)
        {
            float currentDamage = statsManager.GetStatFloat(STATS_ID_STEAMWORKS.TOTAL_DAMAGE);
            statsManager.UpdateStat(STATS_ID_STEAMWORKS.TOTAL_DAMAGE, currentDamage + levelData.bodyDecay);
        }

        // Check for special achievements
        // No drop game achievement
        if (levelData.totalDropCount == 0 && levelData.completed)
        {
            statsManager.IncrementStat(STATS_ID_STEAMWORKS.NO_DROP_GAMES_COUNT);
        }

        // Perfect condition achievement
        if (levelData.bodyDecay == 0 && levelData.completed)
        {
            statsManager.IncrementStat(STATS_ID_STEAMWORKS.PERFECT_CONDITION_GAMES_COUNT);
        }

        // Store stats to Steam
        if (SteamManager.Initialized)
        {
            SteamUserStats.StoreStats();
        }

        if (HilltopConstants.DEBUG_STEAM_API)
        {
            Debug.Log($"Updated Steam stats for Level {levelData.level}:");
            Debug.Log($"  - Completed: {levelData.completed}");
            Debug.Log($"  - Completion Count: {levelData.completedCount}");
            Debug.Log($"  - Restart Count: {levelData.restartCount}");
            Debug.Log($"  - Coins: {levelData.coinCount}");
            Debug.Log($"  - Drops: {levelData.totalDropCount}");
            Debug.Log($"  - Body Decay: {levelData.bodyDecay}");
        }

    }

    /// <summary>
    /// Trigger achievements manually based on level completion
    /// </summary>
    private void TriggerLevelCompletionAchievements(LevelData levelData)
    {
        SteamStats steamStats = GetStatsManager();
        LEVEL_ID levelID = (LEVEL_ID)levelData.level;
        // Level-specific achievements
        switch (levelID)
        {
            case LEVEL_ID.LEVEL_0:
                if (levelData.completedCount > 0)
                    steamStats.UnlockAchievement("LEVEL_0_COMPLETE");
                break;

            case LEVEL_ID.LEVEL_1:
                if (levelData.completedCount > 0)
                    steamStats.UnlockAchievement("LEVEL_1_COMPLETE");
                break;
            case LEVEL_ID.LEVEL_2:
                if (levelData.completedCount > 0)
                    steamStats.UnlockAchievement("LEVEL_2_COMPLETE");
                break;
            case LEVEL_ID.LEVEL_3:
                if (levelData.completedCount > 0)
                    steamStats.UnlockAchievement("LEVEL_3_COMPLETE");
                break;
            case LEVEL_ID.LEVEL_4:
                if (levelData.completedCount > 0)
                    steamStats.UnlockAchievement("LEVEL_4_COMPLETE");
                break;
        }

        // Check for "complete all levels" achievement
        if (FirstFiveLevelHigherThanS())
        {
            steamStats.UnlockAchievement("Five_Steps_To_Glory");
        }

        if (levelData.completed == false)
            return;

        if (ObjectFinder.Instance != null)
        {
            // No Drop Run
            if (ObjectFinder.Instance.levelState.levelDropCount == 0)
            {
                steamStats.UnlockAchievement("Professional_Palbearers");
            }
            if (ObjectFinder.Instance.repairController != null)
            {
                // Perfect Coffin Condition
                if (ObjectFinder.Instance.repairController.DoesCoffinNeedRepairs() == false)
                {
                    steamStats.UnlockAchievement("Not_Even_A_Scratch");
                }
                // Perfect Corpse Condition
                if (ObjectFinder.Instance.repairController.DoesCorpseNeedRepairs() == false)
                {
                    steamStats.UnlockAchievement("Man_Of_Steel");
                }
            }
            // No Mud Kill In Level_2
            if (ObjectFinder.Instance.levelState.levelSpecificDeath == 0 && levelID == LEVEL_ID.LEVEL_2)
            {
                steamStats.UnlockAchievement("I_Prefer_Showers");
            }
        }
    }

    //check if first 5 levels are higher than S
    private bool FirstFiveLevelHigherThanS()
    {
        int levelCount = 0;
        // Check each level to see if it's been completed
        for (int i = 0; i < 5; i++)
        {
            LEVEL_ID levelID = (LEVEL_ID)i;

            // Get the level data for this level (check NORMAL difficulty for achievements)
            LevelData levelData = GameLogic.Instance.GetLevelData((LEVEL_ID)i, DIFFICULTY.NORMAL);
            if (levelData == null || !levelData.completed)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"Level {levelID} not completed yet");
                return false; // This level hasn't been completed yet                
            }
            else if (levelData != null && levelData.highScore >= 3)
            {
                levelCount++;
            }
        }

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("All levels have been completed and got A!");

        return levelCount >= 5;
    }

    /// <summary>
    /// Check if all levels have been completed at least once
    /// </summary>
    private bool AreAllLevelsCompleted()
    {
        // Check each level to see if it's been completed
        for (int i = 0; i < GameLogic.Instance.TOTAL_LEVELS_COUNT + 1; i++)
        {
            LEVEL_ID levelID = (LEVEL_ID)i;

            // Get the level data for this level (check NORMAL difficulty for achievements)
            LevelData levelData = GameLogic.Instance.GetLevelData((LEVEL_ID)i, DIFFICULTY.NORMAL);
            if (levelData == null || !levelData.completed || levelData.completedCount == 0)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"Level {levelID} not completed yet");
                return false; // This level hasn't been completed yet                
            }
            else if (levelData != null && levelData.highScore != 3)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"Level {levelID} completed but did not achieve A rank");
                return false; // This level hasn't achieved A rank yet
            }

        }

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("All levels have been completed and got A!");

        return true;
    }

    private STATS_ID_STEAMWORKS GetCompletedCountStat(LEVEL_ID levelID)
    {
        return levelID switch
        {
            LEVEL_ID.LEVEL_0 => STATS_ID_STEAMWORKS.LEVEL_0_COMPLETED_COUNT,
            LEVEL_ID.LEVEL_1 => STATS_ID_STEAMWORKS.LEVEL_1_COMPLETED_COUNT,
            LEVEL_ID.LEVEL_2 => STATS_ID_STEAMWORKS.LEVEL_2_COMPLETED_COUNT,
            LEVEL_ID.LEVEL_3 => STATS_ID_STEAMWORKS.LEVEL_3_COMPLETED_COUNT,
            LEVEL_ID.LEVEL_4 => STATS_ID_STEAMWORKS.LEVEL_4_COMPLETED_COUNT,
            _ => STATS_ID_STEAMWORKS.LEVEL_0_COMPLETED_COUNT
        };
    }

    public void RequestStatsRefreshFromSteam()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Cannot refresh stats - Steam is not initialized");
            return;
        }

        SteamStats statsManager = GetStatsManager();
        if (statsManager != null)
        {
            statsManager.RequestStatsForCurrentUser();

            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log("Requested stats refresh from Steam server");
        }
        else
        {
            Debug.LogError("Steam Stats Manager is null! Cannot refresh stats.");
        }
    }

    /// <summary>
    /// Forces a sync of local stats to the Steam server.
    /// Call this after making changes to ensure they're uploaded.
    /// </summary>
    public void SyncStatsToSteam()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Cannot sync stats - Steam is not initialized");
            return;
        }

        bool result = SteamUserStats.StoreStats();

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"Synced stats to Steam server: {result}");
    }

    private void Event_RequestSteamworksStats()
    {
        RequestStatsRefreshFromSteam();
    }

    #endregion
}