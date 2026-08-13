using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

// Used Steamworks Template from https://steamworks.github.io/

public class LeaderboardEntry
{
    public int rank;
    public CSteamID userID;
    public string personalName;
    public float time;
    public int highScore;
    public bool mine;
    public ulong friendID;
    public string friendName;
}

public class LeaderboardLevel
{
    public SteamLeaderboard_t m_SteamLeaderboard;
    public SteamLeaderboardEntries_t m_SteamLeaderboardEntries;
    public List<LeaderboardEntry> m_LeaderboardEntries;
    public bool refresh = true;
    public DateTime lastUpdated = DateTime.Now;

    public LeaderboardLevel()
    {
        m_LeaderboardEntries = new List<LeaderboardEntry>();
    }
}

public class SteamLeaderboards : MonoBehaviour
{
    public int maxEntries = 500; // TEST: See if we can get past 100 with a smaller number
    private const int BATCH_SIZE = 100;

    // Batch download state
    private bool m_IsDownloadingBatches = false;
    private int m_CurrentBatch = 0;
    private int m_TotalBatches = 0;
    private (LEVEL_ID, DIFFICULTY, bool) m_BatchingKey;

    // Debug: track last requested batch range/handle
    private int m_LastRequestedRangeStart = -1;
    private int m_LastRequestedRangeEnd = -1;
    private ulong m_LastRequestedHandle = 0;

    // Current state tracking
    private SteamLeaderboard_t m_Current_SteamLeaderboard;
    private SteamLeaderboardEntries_t m_Current_SteamLeaderboardEntries;
    private LEVEL_ID m_Current_SteamLeaderboardID;
    private LEVEL_ID m_UploadingLevelID;
    private DIFFICULTY m_Current_SteamLeaderboardDifficulty;
    private DIFFICULTY m_UploadingLevelDifficulty;
    bool m_Current_IsCoop;
    bool m_UploadingIsCoop;


    // Leaderboard data storage
    private Dictionary<(LEVEL_ID, DIFFICULTY, bool), LeaderboardLevel> m_LeaderboardLevels;

    // Track which leaderboards have been auto-synced this session
    private HashSet<(LEVEL_ID, DIFFICULTY, bool)> m_SyncedThisSession;

    // Unused legacy fields
    private Vector2 m_ScrollPos;
    private int m_NumGamesStat;
    private float m_LEVEL_0_TIME_Stat;
    private bool m_AchievedWinOneGame;
    private Texture2D m_Icon;

    // Callbacks
    protected Callback<UserStatsReceived_t> m_UserStatsReceived;
    protected Callback<UserStatsStored_t> m_UserStatsStored;
    protected Callback<UserAchievementStored_t> m_UserAchievementStored;
    protected Callback<UserStatsUnloaded_t> m_UserStatsUnloaded;
    protected Callback<UserAchievementIconFetched_t> m_UserAchievementIconFetched;

    // Call results
    private CallResult<UserStatsReceived_t> OnUserStatsReceivedCallResult;
    private CallResult<LeaderboardFindResult_t> OnLeaderboardFindResultCallResult;
    private CallResult<LeaderboardScoresDownloaded_t> OnLeaderboardScoresDownloadedCallResult;
    private CallResult<LeaderboardScoreUploaded_t> OnLeaderboardScoreUploadedCallResult;
    private CallResult<NumberOfCurrentPlayers_t> OnNumberOfCurrentPlayersCallResult;
    private CallResult<GlobalAchievementPercentagesReady_t> OnGlobalAchievementPercentagesReadyCallResult;
    private CallResult<LeaderboardUGCSet_t> OnLeaderboardUGCSetCallResult;
    private CallResult<GlobalStatsReceived_t> OnGlobalStatsReceivedCallResult;
    private CallResult<LeaderboardScoresDownloaded_t> OnPersonalRankCallResult;

    #region Unity Lifecycle

    private void Awake()
    {
        m_LeaderboardLevels = new Dictionary<(LEVEL_ID, DIFFICULTY, bool), LeaderboardLevel>();
        m_SyncedThisSession = new HashSet<(LEVEL_ID, DIFFICULTY, bool)>();
    }

    public void OnEnable()
    {
        OnLeaderboardFindResultCallResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFindResult);
        OnLeaderboardScoresDownloadedCallResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderboardScoresDownloaded);
        OnLeaderboardScoreUploadedCallResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnLeaderboardScoreUploaded);
        OnNumberOfCurrentPlayersCallResult = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);
        OnLeaderboardUGCSetCallResult = CallResult<LeaderboardUGCSet_t>.Create(OnLeaderboardUGCSet);
        OnPersonalRankCallResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnPersonalRankDownloaded);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets cached leaderboard entries for the specified level.
    /// Will refresh if data is stale or not available.
    /// Automatically syncs local best time with Steam if needed.
    /// </summary>
    public List<LeaderboardEntry> GetLeaderboardEntriesForLevel(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        m_Current_SteamLeaderboardID = levelID;
        m_Current_SteamLeaderboardDifficulty = difficulty;
        m_Current_IsCoop = coop;

        var key = (levelID, difficulty, coop);

        if (m_LeaderboardLevels.ContainsKey(key))
        {
            if (m_LeaderboardLevels[key] == null || m_LeaderboardLevels[key].refresh)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[SteamLeaderboards] Cache invalid for {levelID} ({difficulty}), refreshing...");

                InitializeLeaderboardLevel(levelID, difficulty, coop);
                FindLeaderboardWithID(levelID, difficulty, coop);

                // NEW: Auto-sync local best time after leaderboard is found
                // CheckAndSyncLocalBestTime(levelID, difficulty);

                return null;
            }

            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[SteamLeaderboards] Returning {m_LeaderboardLevels[key].m_LeaderboardEntries.Count} cached entries for {levelID} ({difficulty})");

            return m_LeaderboardLevels[key].m_LeaderboardEntries;
        }
        else
        {
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[SteamLeaderboards] First request for {levelID} ({difficulty}), initializing...");

            m_LeaderboardLevels.Add(key, new LeaderboardLevel());
            m_LeaderboardLevels[key].m_LeaderboardEntries = new List<LeaderboardEntry>();
            FindLeaderboardWithID(levelID, difficulty, coop);
            // NEW: Auto-sync after finding leaderboard
            // CheckAndSyncLocalBestTime(levelID, difficulty);
            return null;
        }
    }

    /// <summary>
    /// Checks if local best time needs to be synced with Steam and uploads if necessary.
    /// Call this after leaderboard is found and entries are downloaded.
    /// Only syncs once per session to avoid infinite loops.
    /// </summary>
    private void CheckAndSyncLocalBestTime(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        if (!SteamManager.Initialized)
        {
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.LogWarning("[SteamLeaderboards] Cannot sync - Steam not initialized");
            return;
        }

        var key = (levelID, difficulty, coop);

        // Only sync once per session to avoid infinite loops
        if (m_SyncedThisSession.Contains(key))
        {
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[SteamLeaderboards] Already synced {levelID} {difficulty} {coop} this session, skipping");
            return;
        }

        m_SyncedThisSession.Add(key);

        // Get Steam leaderboard entry for this user
        CSteamID myID = SteamUser.GetSteamID();
        int steamBestTimeMs = 0;

        if (m_LeaderboardLevels.ContainsKey(key) && m_LeaderboardLevels[key].m_LeaderboardEntries != null)
        {
            LeaderboardEntry mySteamEntry = m_LeaderboardLevels[key].m_LeaderboardEntries.Find(
                entry => entry.userID.m_SteamID == myID.m_SteamID
            );

            if (mySteamEntry != null)
            {
                steamBestTimeMs = (int)(mySteamEntry.time * 1000);
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[SteamLeaderboards] Found Steam entry for {levelID} {difficulty} {coop}: {steamBestTimeMs}ms");
            }
            else if (HilltopConstants.DEBUG_STEAM_API)
            {
                Debug.Log($"[SteamLeaderboards] No Steam entry found for {levelID} {difficulty} {coop}, will upload if local exists");
            }
        }

        LevelSave levelSave = LevelSave.Instance;
        int localBestTimeMs = levelSave.GetLocalBestTimeMs(levelID, difficulty, coop);

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] Sync check for {levelID} {difficulty} {coop}: Local={localBestTimeMs}ms, Steam={steamBestTimeMs}ms");

        if (levelSave.ShouldSyncWithSteam(levelID, difficulty, steamBestTimeMs, coop))
        {
            if (localBestTimeMs > 0)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[SteamLeaderboards] ✓ Uploading local best time for {levelID}: {localBestTimeMs}ms");

                // For coop, get the friendID associated with the best time
                ulong friendID = 0;
                if (coop)
                {
                    var (bestTimeMs, bestFriendID) = levelSave.GetLocalBestCoopTimeWithFriend(levelID, difficulty);
                    friendID = bestFriendID;
                    if (HilltopConstants.DEBUG_STEAM_API)
                        Debug.Log($"[SteamLeaderboards] Best coop time achieved with friend: {friendID}");
                }

                UploadAndRefresh(levelID, difficulty, localBestTimeMs, friendID);
            }
            else if (HilltopConstants.DEBUG_STEAM_API)
            {
                Debug.LogWarning($"[SteamLeaderboards] ✗ ShouldSync=true but localBestTimeMs is {localBestTimeMs}, skipping upload");
            }
        }
        else if (HilltopConstants.DEBUG_STEAM_API)
        {
            Debug.Log($"[SteamLeaderboards] ✗ No sync needed for {levelID} ({difficulty}) - ShouldSyncWithSteam returned false");
        }
    }

    /// <summary>
    /// Uploads a time and refreshes the leaderboard.
    /// </summary>
    private void UploadAndRefresh(LEVEL_ID levelID, DIFFICULTY difficulty, int timeMs, ulong friendID = 0)
    {
        bool isCoop;
        if (friendID == 0)
            isCoop = false;
        else
            isCoop = true;
        var key = (levelID, difficulty, isCoop);

        if (!m_LeaderboardLevels.ContainsKey(key))
        {
            Debug.LogError($"[SteamLeaderboards] Cannot upload - leaderboard not initialized for {levelID} ({difficulty})");
            return;
        }

        SteamLeaderboard_t leaderboard = m_LeaderboardLevels[key].m_SteamLeaderboard;

        if (leaderboard.m_SteamLeaderboard == 0)
        {
            Debug.LogError($"[SteamLeaderboards] ✗ Invalid leaderboard handle for {levelID} ({difficulty}) - cannot upload");
            return;
        }

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] Leaderboard handle valid: {leaderboard.m_SteamLeaderboard}");

        // Get high score from level data
        LevelData levelData = GameLogic.Instance.GetLevelData(levelID, difficulty);
        int highScore = levelData?.highScore ?? 0;
        // int[] details = new int[] { highScore };
        int[] details;
        if (friendID == 0)
        {
            details = new int[] { highScore };
        }
        else
        {
            int upperBits = (int)(friendID >> 32);  // Upper 32 bits
            int lowerBits = (int)(friendID & 0xFFFFFFFF);  // Lower 32 bits

            details = new int[] { highScore, lowerBits, upperBits };
        }

        // Use KeepBest to avoid overwriting better times
        SteamAPICall_t handle = SteamUserStats.UploadLeaderboardScore(
            leaderboard,
            ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
            timeMs,
            details,
            details.Length
        );

        // // Use ForceUpdate to ensure upload        
        // SteamAPICall_t handle = SteamUserStats.UploadLeaderboardScore(
        //     leaderboard,
        //     ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodForceUpdate,
        //     timeMs,
        //     details,
        //     details.Length
        // );

        if (handle == SteamAPICall_t.Invalid)
        {
            Debug.LogError($"[SteamLeaderboards] ✗ UploadLeaderboardScore returned INVALID handle!");
            return;
        }

        if (HilltopConstants.DEBUG_STEAM_API)
        {
            if (friendID == 0)
                Debug.Log($"[SteamLeaderboards] ⬆️ Upload initiated: APICall handle={handle.m_SteamAPICall}, time={timeMs}ms, level={levelID}, difficulty={difficulty}, highScore={highScore}");
            else
                Debug.Log($"[SteamLeaderboards] ⬆️ Upload initiated for friend: APICall handle={handle.m_SteamAPICall}, time={timeMs}ms, level={levelID}, difficulty={difficulty}, highScore={highScore}, friendName={SteamFriends.GetFriendPersonaName(new CSteamID(friendID))}");
        }

        m_UploadingLevelID = levelID;
        m_UploadingLevelDifficulty = difficulty;
        m_UploadingIsCoop = isCoop;

        if (OnLeaderboardScoreUploadedCallResult == null)
        {
            Debug.LogError("[SteamLeaderboards] ✗ OnLeaderboardScoreUploadedCallResult is NULL! Callback won't fire!");
            return;
        }

        OnLeaderboardScoreUploadedCallResult.Set(handle);

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] ✓ Callback registered, waiting for Steam response...");
    }

    /// <summary>
    /// Gets the Steam leaderboard handle for the specified level.
    /// </summary>
    public SteamLeaderboard_t GetLeaderboardForLevel(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        var key = (levelID, difficulty, coop);

        if (!m_LeaderboardLevels.ContainsKey(key))
        {
            // Return invalid leaderboard handle
            return new SteamLeaderboard_t();
        }

        return m_LeaderboardLevels[key].m_SteamLeaderboard;
    }

    /// <summary>
    /// Finds and loads the leaderboard for the specified level from Steam.
    /// </summary>
    public void FindLeaderboardWithID(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        string leaderboardName = GetLeaderboardIDFromLevelID(levelID, difficulty, coop);

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] Finding leaderboard: {leaderboardName}");

        SteamAPICall_t handle = SteamUserStats.FindLeaderboard(leaderboardName);
        OnLeaderboardFindResultCallResult.Set(handle);
    }

    /// <summary>
    /// Uploads a time score to the specified leaderboard using force update method.
    /// </summary>
    public void UploadTimeToLeaderboard(int levelTimeMilliseconds, SteamLeaderboard_t leaderboard, LEVEL_ID levelID, DIFFICULTY difficulty, int highScore = 0, ulong friendID = 0)
    {
        m_UploadingLevelID = levelID;
        m_UploadingLevelDifficulty = difficulty;
        m_UploadingIsCoop = friendID == 0 ? false : true;
        // Include high score in details array
        int[] details;
        if (friendID == 0)
        {
            details = new int[] { highScore };
        }
        else
        {
            int upperBits = (int)(friendID >> 32);  // Upper 32 bits
            int lowerBits = (int)(friendID & 0xFFFFFFFF);  // Lower 32 bits

            details = new int[] { highScore, lowerBits, upperBits };
        }

        SteamAPICall_t handle = SteamUserStats.UploadLeaderboardScore(
            leaderboard,
            ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodForceUpdate,
            levelTimeMilliseconds,
            details,
            details.Length
        );

        OnLeaderboardScoreUploadedCallResult.Set(handle);

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] Uploading {levelTimeMilliseconds}ms for {levelID}{difficulty}");
    }

    /// <summary>
    /// Downloads leaderboard entries for the current leaderboard.
    /// </summary>


    /// <summary>
    /// Downloads leaderboard entries for a specific level using batching.
    /// </summary>
    public void DownloadLeaderboardEntriesForLeaderboard(int rangeStart, int rangeEnd, LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] DownloadLeaderboardEntriesForLeaderboard CALLED for {levelID}{difficulty}{coop}, range {rangeStart}-{rangeEnd}. Stack trace:\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");

        var key = (levelID, difficulty, coop);

        // Prevent multiple simultaneous batch downloads
        if (m_IsDownloadingBatches)
        {
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.LogWarning($"[SteamLeaderboards] Already downloading batches, ignoring new request for {levelID}{difficulty}{coop}");
            return;
        }

        m_Current_SteamLeaderboard = m_LeaderboardLevels[key].m_SteamLeaderboard;
        m_Current_SteamLeaderboardID = levelID;
        m_Current_SteamLeaderboardDifficulty = difficulty;
        m_Current_IsCoop = coop;

        StartCoroutine(DownloadBatchedEntriesCoroutine(rangeStart, rangeEnd, key));
    }

    private System.Collections.IEnumerator DownloadBatchedEntriesCoroutine(int rangeStart, int rangeEnd, (LEVEL_ID, DIFFICULTY, bool) key)
    {
        // Initialize batching
        m_IsDownloadingBatches = true;
        m_BatchingKey = key;
        m_CurrentBatch = 0;
        m_TotalBatches = Mathf.CeilToInt((float)(rangeEnd - rangeStart + 1) / BATCH_SIZE);
        m_LeaderboardLevels[key].m_LeaderboardEntries.Clear();

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] Starting batch download: {m_TotalBatches} batches of {BATCH_SIZE}");

        // Download each batch sequentially
        for (int batch = 0; batch < m_TotalBatches; batch++)
        {
            int batchStart = rangeStart + (batch * BATCH_SIZE);
            int batchEnd = Mathf.Min(batchStart + BATCH_SIZE - 1, rangeEnd);

            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[SteamLeaderboards] Requesting batch {batch + 1}/{m_TotalBatches}: ranks {batchStart}-{batchEnd}");

            // Make the API call
            SteamAPICall_t handle = SteamUserStats.DownloadLeaderboardEntries(
                m_Current_SteamLeaderboard,
                ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,
                batchStart,
                batchEnd
            );

            m_LastRequestedRangeStart = batchStart;
            m_LastRequestedRangeEnd = batchEnd;
            m_LastRequestedHandle = handle.m_SteamAPICall;

            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[SteamLeaderboards] API call handle: {m_LastRequestedHandle}, requested ranks {m_LastRequestedRangeStart}-{m_LastRequestedRangeEnd}");

            OnLeaderboardScoresDownloadedCallResult.Set(handle);

            // Wait for callback
            float timeout = 5f;
            float elapsed = 0f;
            int expectedBatch = batch + 1;

            while (m_CurrentBatch < expectedBatch && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (elapsed >= timeout)
            {
                Debug.LogError($"[SteamLeaderboards] Batch {batch + 1} timed out! Aborting download.");
                m_IsDownloadingBatches = false; // Clean up immediately
                yield break; // Exit coroutine completely
            }
        }

        // All batches complete
        m_IsDownloadingBatches = false;

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] ✓ Batch download complete. Total entries: {m_LeaderboardLevels[key].m_LeaderboardEntries.Count}");

        m_LeaderboardLevels[key].refresh = false;
        m_LeaderboardLevels[key].lastUpdated = DateTime.Now;

        // Download personal rank
        SteamAPICall_t personalHandle = SteamUserStats.DownloadLeaderboardEntries(
            m_Current_SteamLeaderboard,
            ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,
            0,
            0
        );
        OnPersonalRankCallResult.Set(personalHandle);
    }

    /// <summary>
    /// Invalidates the cache for a specific level, forcing a refresh on next request.
    /// </summary>
    public void InvalidateLeaderboardCache(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        var key = (levelID, difficulty, coop);

        if (m_LeaderboardLevels.ContainsKey(key))
        {
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[SteamLeaderboards] ✓ Invalidating cache for {levelID} ({difficulty})");

            m_LeaderboardLevels[key].refresh = true;
        }
    }

    #endregion

    #region Steam Callbacks

    private void OnLeaderboardFindResult(LeaderboardFindResult_t pCallback, bool bIOFailure)
    {
        if (pCallback.m_bLeaderboardFound != 0)
        {
            var key = (m_Current_SteamLeaderboardID, m_Current_SteamLeaderboardDifficulty, m_Current_IsCoop);
            m_Current_SteamLeaderboard = pCallback.m_hSteamLeaderboard;
            m_LeaderboardLevels[key].m_SteamLeaderboard = m_Current_SteamLeaderboard;

            if (HilltopConstants.DEBUG_STEAM_API)
            {
                string foundName = SteamUserStats.GetLeaderboardName(m_Current_SteamLeaderboard);
                int totalEntries = SteamUserStats.GetLeaderboardEntryCount(m_Current_SteamLeaderboard);
                AppId_t appId = SteamUtils.GetAppID();
                Debug.Log($"[SteamLeaderboards] ✓ Leaderboard found for {m_Current_SteamLeaderboardID} ({m_Current_SteamLeaderboardDifficulty}) | Name={foundName} | Entries={totalEntries} | AppID={appId}");
            }

            SteamEvents.Event_OnLeaderboardLoaded(pCallback, m_Current_SteamLeaderboardID, m_Current_SteamLeaderboardDifficulty, false, m_Current_IsCoop);
        }
        else
        {
            Debug.LogError($"[SteamLeaderboards] ✗ Leaderboard NOT found for {m_Current_SteamLeaderboardID}");
            SteamEvents.Event_OnLeaderboardLoaded(pCallback, m_Current_SteamLeaderboardID, m_Current_SteamLeaderboardDifficulty, true, m_Current_IsCoop);
        }
    }

    private void OnLeaderboardScoresDownloaded(LeaderboardScoresDownloaded_t pCallback, bool bIOFailure)
    {
        if (m_IsDownloadingBatches)
        {
            // Batch mode - append entries
            if (!bIOFailure)
            {
                int entriesToProcess = (int)pCallback.m_cEntryCount;
                int totalEntries = SteamUserStats.GetLeaderboardEntryCount(m_Current_SteamLeaderboard);

                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[SteamLeaderboards] Batch {m_CurrentBatch + 1}/{m_TotalBatches}: Received {entriesToProcess} entries (Total in leaderboard: {totalEntries})");

                // Append entries to list
                m_Current_SteamLeaderboardEntries = pCallback.m_hSteamLeaderboardEntries;
                m_LeaderboardLevels[m_BatchingKey].m_SteamLeaderboardEntries = m_Current_SteamLeaderboardEntries;

                // Log what rank range Steam actually returned (with scores/IDs)
                if (HilltopConstants.DEBUG_STEAM_API && entriesToProcess > 0)
                {
                    int[] firstDetails = new int[1];
                    SteamUserStats.GetDownloadedLeaderboardEntry(m_Current_SteamLeaderboardEntries, 0, out var firstEntry, firstDetails, 1);

                    int[] lastDetails = new int[1];
                    SteamUserStats.GetDownloadedLeaderboardEntry(m_Current_SteamLeaderboardEntries, entriesToProcess - 1, out var lastEntry, lastDetails, 1);

                    Debug.Log($"[SteamLeaderboards] Batch response for handle {m_LastRequestedHandle} (requested {m_LastRequestedRangeStart}-{m_LastRequestedRangeEnd}): " +
                              $"count={entriesToProcess}, first(rank={firstEntry.m_nGlobalRank}, score={firstEntry.m_nScore}, user={firstEntry.m_steamIDUser}), " +
                              $"last(rank={lastEntry.m_nGlobalRank}, score={lastEntry.m_nScore}, user={lastEntry.m_steamIDUser})");
                }

                for (int i = 0; i < entriesToProcess; i++)
                {
                    int[] details = new int[1];
                    SteamUserStats.GetDownloadedLeaderboardEntry(
                        m_Current_SteamLeaderboardEntries,
                        i,
                        out var userEntry,
                        details,
                        1
                    );

                    // Skip duplicates (check if this userID already exists)
                    bool isDuplicate = m_LeaderboardLevels[m_BatchingKey].m_LeaderboardEntries.Exists(
                        entry => entry.userID == userEntry.m_steamIDUser
                    );

                    if (isDuplicate)
                    {
                        if (HilltopConstants.DEBUG_STEAM_API)
                            Debug.Log($"[SteamLeaderboards] Skipping duplicate user {userEntry.m_steamIDUser}");
                        continue;
                    }

                    float timeToAdd = userEntry.m_steamIDUser.m_SteamID > 0
                        ? (float)userEntry.m_nScore / 1000f
                        : -1;

                    string personalName = SteamFriends.GetFriendPersonaName(userEntry.m_steamIDUser);
                    int highScore = details.Length > 0 ? details[0] : 0;

                    ulong friendID = 0;
                    string friendName = "";
                    if (details.Length > 1)
                    {
                        ulong lower = (uint)details[1];
                        ulong upper = (uint)details[2];
                        friendID = (upper << 32) | lower;
                        friendName = SteamFriends.GetFriendPersonaName((CSteamID)friendID);
                    }

                    LeaderboardEntry newEntry = new LeaderboardEntry
                    {
                        rank = userEntry.m_nGlobalRank,
                        userID = userEntry.m_steamIDUser,
                        personalName = personalName,
                        time = timeToAdd,
                        highScore = highScore,
                        friendID = friendID,
                        friendName = friendName
                    };

                    m_LeaderboardLevels[m_BatchingKey].m_LeaderboardEntries.Add(newEntry);
                }

                m_CurrentBatch++;

                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[SteamLeaderboards] Batch {m_CurrentBatch}/{m_TotalBatches} complete. Running total: {m_LeaderboardLevels[m_BatchingKey].m_LeaderboardEntries.Count}");
            }
            else
            {
                Debug.LogError($"[SteamLeaderboards] Batch download failed!");
                m_CurrentBatch++; // Increment to prevent hang
            }
            return;
        }

        // Legacy mode - single download
        if (!bIOFailure)
        {
            var key = (m_Current_SteamLeaderboardID, m_Current_SteamLeaderboardDifficulty, m_Current_IsCoop);
            m_Current_SteamLeaderboardEntries = pCallback.m_hSteamLeaderboardEntries;
            m_LeaderboardLevels[key].m_SteamLeaderboardEntries = m_Current_SteamLeaderboardEntries;

            int entriesToProcess = Math.Min((int)pCallback.m_cEntryCount, maxEntries);

            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[SteamLeaderboards] Processing {entriesToProcess} entries for {m_Current_SteamLeaderboardID} ({m_Current_SteamLeaderboardDifficulty})");

            m_LeaderboardLevels[key] = AddEntriesFromResult(
                m_LeaderboardLevels[key],
                entriesToProcess
            );

            m_LeaderboardLevels[key].refresh = false;
            m_LeaderboardLevels[key].lastUpdated = DateTime.Now;

            // Download personal rank
            SteamAPICall_t personalHandle = SteamUserStats.DownloadLeaderboardEntries(
                m_Current_SteamLeaderboard,
                ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,
                0,
                0
            );
            OnPersonalRankCallResult.Set(personalHandle);
        }
        else
        {
            Debug.LogError($"[SteamLeaderboards] ✗ Failed to download scores for {m_Current_SteamLeaderboardID}");
        }
    }

    private void OnPersonalRankDownloaded(LeaderboardScoresDownloaded_t pCallback, bool bIOFailure)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] OnPersonalRankDownloaded: entryCount={pCallback.m_cEntryCount}, IOFailure={bIOFailure}");

        if (!bIOFailure && pCallback.m_cEntryCount > 0)
        {
            int[] details = new int[1];
            SteamUserStats.GetDownloadedLeaderboardEntry(
                pCallback.m_hSteamLeaderboardEntries,
                0,
                out var userEntry,
                details,
                1
            );

            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[SteamLeaderboards] Personal rank entry: userID={userEntry.m_steamIDUser}, score={userEntry.m_nScore}, rank={userEntry.m_nGlobalRank}");

            var key = (m_Current_SteamLeaderboardID, m_Current_SteamLeaderboardDifficulty, m_Current_IsCoop);
            var currentLeaderboard = m_LeaderboardLevels[key];
            // bool alreadyInList = currentLeaderboard.m_LeaderboardEntries.Exists(entry =>
            //     entry.userID.m_SteamID == userEntry.m_steamIDUser.m_SteamID);

            // See if the personal entry is already present
            var existing = currentLeaderboard.m_LeaderboardEntries.Find(entry =>
                entry.userID.m_SteamID == userEntry.m_steamIDUser.m_SteamID);

            if (existing != null)
            {
                // mark the existing cached entry as belonging to the local player
                existing.mine = true;

                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[SteamLeaderboards] Personal entry already in list - marked mine. Rank #{existing.rank}");
            }
            else if (userEntry.m_steamIDUser.m_SteamID > 0)
            {
                float timeToAdd = (float)userEntry.m_nScore / 1000f;
                string personalName = SteamFriends.GetFriendPersonaName(userEntry.m_steamIDUser);
                int highScore = details.Length > 0 ? details[0] : 0;

                ulong friendID = 0;
                string friendName = "";
                if (details.Length > 1)
                {
                    ulong lower = (uint)details[1];
                    ulong upper = (uint)details[2];
                    friendID = (upper << 32) | lower;
                    friendName = SteamFriends.GetFriendPersonaName((CSteamID)friendID);
                }

                LeaderboardEntry personalEntry = new LeaderboardEntry
                {
                    rank = userEntry.m_nGlobalRank,
                    userID = userEntry.m_steamIDUser,
                    personalName = personalName,
                    time = timeToAdd,
                    highScore = highScore,
                    mine = true,
                    friendID = friendID,
                    friendName = friendName
                };

                currentLeaderboard.m_LeaderboardEntries.Add(personalEntry);

                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[SteamLeaderboards] ✓ Added personal entry: Rank #{userEntry.m_nGlobalRank}");
            }
        }
        // NEW: After downloading personal rank, check if we need to sync
        CheckAndSyncLocalBestTime(m_Current_SteamLeaderboardID, m_Current_SteamLeaderboardDifficulty, m_Current_IsCoop);

        // Send results to UI
        var resultKey = (m_Current_SteamLeaderboardID, m_Current_SteamLeaderboardDifficulty, m_Current_IsCoop);
        SteamEvents.Event_OnLeaderboardResult(
            m_LeaderboardLevels[resultKey].m_LeaderboardEntries,
            m_Current_SteamLeaderboardID
        );
    }

    private void OnLeaderboardScoreUploaded(LeaderboardScoreUploaded_t pCallback, bool bIOFailure)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] OnLeaderboardScoreUploaded callback fired: success={pCallback.m_bSuccess}, IOFailure={bIOFailure}, scoreChanged={pCallback.m_bScoreChanged}, newRank={pCallback.m_nGlobalRankNew}, prevRank={pCallback.m_nGlobalRankPrevious}");

        if (pCallback.m_bSuccess == 1 && !bIOFailure)
        {
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[SteamLeaderboards] ✓ Score uploaded for {m_UploadingLevelID} - Rank: #{pCallback.m_nGlobalRankNew}, Score changed: {pCallback.m_bScoreChanged}");

            // Invalidate cache and request fresh data
            InvalidateLeaderboardCache(m_UploadingLevelID, m_UploadingLevelDifficulty, m_UploadingIsCoop);
            SteamEvents.Event_RequestLeaderboardForLevel(m_UploadingLevelID, m_UploadingLevelDifficulty, m_UploadingIsCoop);
        }
        else
        {
            Debug.LogError($"[SteamLeaderboards] ✗ Score upload FAILED for {m_UploadingLevelID} - success={pCallback.m_bSuccess}, IOFailure={bIOFailure}");
        }
    }

    private void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t pCallback, bool bIOFailure)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] Current players: {pCallback.m_cPlayers}");
    }

    private void OnLeaderboardUGCSet(LeaderboardUGCSet_t pCallback, bool bIOFailure)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] UGC set result: {pCallback.m_eResult}");
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Converts a level ID to its corresponding Steam leaderboard ID string.
    /// </summary>
    private string GetLeaderboardIDFromLevelID(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        string coopText = coop ? "_COOP" : "";
        switch (difficulty)
        {
            case DIFFICULTY.EASY:
                return $"{levelID}{coopText}_TIME_EASY";
            case DIFFICULTY.NORMAL:
                return $"{levelID}{coopText}_TIME";
            case DIFFICULTY.HARD:
                return $"{levelID}{coopText}_TIME_HARD";
            default:
                Debug.LogWarning($"[SteamLeaderboards] Unknown difficulty {difficulty}, defaulting to NORMAL");
                return $"{levelID}{coopText}_TIME";
        }
    }

    /// <summary>
    /// Initializes a new leaderboard level entry.
    /// </summary>
    private void InitializeLeaderboardLevel(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        var key = (levelID, difficulty, coop);
        m_LeaderboardLevels[key] = new LeaderboardLevel();
        m_LeaderboardLevels[key].m_LeaderboardEntries = new List<LeaderboardEntry>();
    }

    /// <summary>
    /// Processes downloaded leaderboard entries and adds them to the level data.
    /// </summary>
    private LeaderboardLevel AddEntriesFromResult(LeaderboardLevel leaderboardLevel, int entryCount)
    {
        leaderboardLevel.m_LeaderboardEntries.Clear();

        for (int i = 0; i < entryCount; i++)
        {
            int[] details = new int[1];
            SteamUserStats.GetDownloadedLeaderboardEntry(
                leaderboardLevel.m_SteamLeaderboardEntries,
                i,
                out var userEntry,
                details,
                1
            );

            float timeToAdd = userEntry.m_steamIDUser.m_SteamID > 0
                ? (float)userEntry.m_nScore / 1000f
                : -1;

            string personalName = SteamFriends.GetFriendPersonaName(userEntry.m_steamIDUser);
            int highScore = details.Length > 0 ? details[0] : 0;

            ulong friendID = 0;
            string friendName = "";
            if (details.Length > 1)
            {
                ulong lower = (uint)details[1];
                ulong upper = (uint)details[2];
                friendID = (upper << 32) | lower;
                friendName = SteamFriends.GetFriendPersonaName((CSteamID)friendID);
            }

            LeaderboardEntry newEntry = new LeaderboardEntry
            {
                rank = userEntry.m_nGlobalRank,
                userID = userEntry.m_steamIDUser,
                personalName = personalName,
                time = timeToAdd,
                highScore = highScore,
                friendID = friendID,
                friendName = friendName
            };

            leaderboardLevel.m_LeaderboardEntries.Add(newEntry);
        }

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[SteamLeaderboards] ✓ Cached {leaderboardLevel.m_LeaderboardEntries.Count} entries");

        return leaderboardLevel;
    }
    #endregion
}