using System;
using UnityEngine;
using System.Collections.Generic;
using Steamworks;

// Used Steamworks Template from https://steamworks.github.io/

public enum StatType
{
    Integer,
    Float
}

[ES3Serializable]
public class StatsMap
{
    public Dictionary<STATS_ID_STEAMWORKS, float> _statsMap;
    public Dictionary<STATS_ID_STEAMWORKS, StatType> _statTypes;

    public void InitStatsMap()
    {
        _statsMap = new Dictionary<STATS_ID_STEAMWORKS, float>();
        _statTypes = new Dictionary<STATS_ID_STEAMWORKS, StatType>();

        // Initialize stats with their types
        InitStatWithType(STATS_ID_STEAMWORKS.TOTAL_GAMES_PLAYED, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.TOTAL_DROPS, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.TOTAL_DAMAGE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.TOTAL_COINS, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.TOTAL_DISTANCE, StatType.Float);
        InitStatWithType(STATS_ID_STEAMWORKS.HEAL_BODY_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.CASKET_REPAIR_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.CHAOS_COINS, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.HAUNTED_GRAVES_GRABS_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.SKULL_KNOCK_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.MUD_SINK_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.TOTAL_COLLECTABLE_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.GOLD_COINS, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.BRIDGE_FALL_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.BREAK_ITEMS_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.SAME_RESPAWN_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.NO_DROP_GAMES_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.PERFECT_CONDITION_GAMES_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_0_COMPLETED_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_0_FAILED_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_0_TOTAL_TIME, StatType.Float);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_1_TOTAL_TIME, StatType.Float);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_1_FAILED_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_1_COMPLETED_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_2_TOTAL_TIME, StatType.Float);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_2_COMPLETED_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_2_FAILED_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_3_TOTAL_TIME, StatType.Float);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_3_COMPLETED_COUNT, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_3_FAILED_COUNT, StatType.Integer);

        InitStatWithType(STATS_ID_STEAMWORKS.FALL_COUNT, StatType.Integer);

        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_0_EASY_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_1_EASY_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_2_EASY_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_3_EASY_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_4_EASY_HIGHSCORE, StatType.Integer);

        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_0_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_1_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_2_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_3_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_4_HIGHSCORE, StatType.Integer);

        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_0_HARD_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_1_HARD_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_2_HARD_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_3_HARD_HIGHSCORE, StatType.Integer);
        InitStatWithType(STATS_ID_STEAMWORKS.LEVEL_4_HARD_HIGHSCORE, StatType.Integer);
    }

    private void InitStatWithType(STATS_ID_STEAMWORKS stat, StatType type)
    {
        _statsMap[stat] = 0f;
        _statTypes[stat] = type;
    }

    public float GetFloatValue(STATS_ID_STEAMWORKS id) => _statsMap[id];
    public int GetIntValue(STATS_ID_STEAMWORKS id) => (int)_statsMap[id];
    public void SetValue(STATS_ID_STEAMWORKS id, float value) => _statsMap[id] = value;
    public void SetValueInt(STATS_ID_STEAMWORKS id, float value) => _statsMap[id] = (int)value;
}

public class StatsData
{
    //TODO: Remove from ES
}

public class SteamStats : MonoBehaviour
{
    private StatsMap _steamworksStats;
    private Texture2D m_Icon;

    // Steam callbacks
    protected Callback<UserStatsReceived_t> m_UserStatsReceived;
    protected Callback<UserStatsStored_t> m_UserStatsStored;
    protected Callback<UserAchievementStored_t> m_UserAchievementStored;
    protected Callback<UserStatsUnloaded_t> m_UserStatsUnloaded;
    protected Callback<UserAchievementIconFetched_t> m_UserAchievementIconFetched;

    // Steam call results
    private CallResult<UserStatsReceived_t> OnUserStatsReceivedCallResult;
    private CallResult<NumberOfCurrentPlayers_t> OnNumberOfCurrentPlayersCallResult;
    private CallResult<GlobalAchievementPercentagesReady_t> OnGlobalAchievementPercentagesReadyCallResult;
    private CallResult<GlobalStatsReceived_t> OnGlobalStatsReceivedCallResult;

    #region Unity Lifecycle

    public void Awake()
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("Steam Stats Awake");

        _steamworksStats = new StatsMap();
        _steamworksStats.InitStatsMap();
    }

    public void Start()
    {
        if (HilltopConstants.DEBUG_STEAM_API)
        {
            IncrementStat(STATS_ID_STEAMWORKS.TOTAL_GAMES_PLAYED);
        }
    }

    public void OnEnable()
    {
        m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
        m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
        m_UserAchievementStored = Callback<UserAchievementStored_t>.Create(OnUserAchievementStored);
        m_UserStatsUnloaded = Callback<UserStatsUnloaded_t>.Create(OnUserStatsUnloaded);
        m_UserAchievementIconFetched = Callback<UserAchievementIconFetched_t>.Create(OnUserAchievementIconFetched);

        OnUserStatsReceivedCallResult = CallResult<UserStatsReceived_t>.Create(OnUserStatsReceived);
        OnNumberOfCurrentPlayersCallResult = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);
        OnGlobalAchievementPercentagesReadyCallResult = CallResult<GlobalAchievementPercentagesReady_t>.Create(OnGlobalAchievementPercentagesReady);
        OnGlobalStatsReceivedCallResult = CallResult<GlobalStatsReceived_t>.Create(OnGlobalStatsReceived);
    }

    #endregion

    #region Public API - Read

    public int GetStat(STATS_ID_STEAMWORKS stat)
    {
        if (!_steamworksStats._statsMap.ContainsKey(stat))
        {
            SteamEvents.Event_OnSteamworksError("Stats not found... Did you forget to add it to the stats map?");
            SteamEvents.Event_OnStatResponse(stat, 0);
            return 0;
        }

        int value = _steamworksStats.GetIntValue(stat);
        SteamEvents.Event_OnStatResponse(stat, value);
        return value;
    }

    public float GetStatFloat(STATS_ID_STEAMWORKS stat)
    {
        if (!_steamworksStats._statsMap.ContainsKey(stat))
        {
            SteamEvents.Event_OnSteamworksError("Stats not found... Did you forget to add it to the stats map?");
            SteamEvents.Event_OnStatResponse(stat, 0);
            return 0;
        }

        float value = _steamworksStats.GetFloatValue(stat);
        SteamEvents.Event_OnStatResponse(stat, value);
        return value;
    }

    #endregion

    #region Public API - Write

    public void UpdateStat(STATS_ID_STEAMWORKS stat, float value)
    {
        if (!EnsureStatExists(stat)) return;

        if (_steamworksStats._statTypes[stat] == StatType.Integer)
        {
            int intValue = (int)value;
            _steamworksStats.SetValueInt(stat, intValue);
            SteamUserStats.SetStat(stat.ToString(), intValue);
        }
        else
        {
            _steamworksStats.SetValue(stat, value);
            SteamUserStats.SetStat(stat.ToString(), value);
        }
    }

    public bool IncrementStat(STATS_ID_STEAMWORKS stat)
    {
        if (!EnsureStatExists(stat)) return false;

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"Incrementing stat {stat} from current value: {_steamworksStats.GetFloatValue(stat)}");

        bool result;
        if (_steamworksStats._statTypes[stat] == StatType.Integer)
        {
            int v = _steamworksStats.GetIntValue(stat) + 1;
            _steamworksStats.SetValueInt(stat, v);
            result = SteamUserStats.SetStat(stat.ToString(), v);
        }
        else
        {
            float v = _steamworksStats.GetFloatValue(stat) + 1f;
            _steamworksStats.SetValue(stat, v);
            result = SteamUserStats.SetStat(stat.ToString(), v);
        }
        // added this to make stuff like body drops to trigger achievement at the correct time
        if (result)
        {
            bool stored = SteamUserStats.StoreStats();
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"StoreStats result for {stat}: {stored}");
        }

        if (HilltopConstants.DEBUG_STEAM_API)
        {
            Debug.Log($"SetStat result for {stat}: {result}");
            Debug.Log($"New value for {stat}: {_steamworksStats.GetFloatValue(stat)}");
        }

        return result;
    }

    public void ResetSamePositionRespawnedTimes()
    {
        _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.SAME_RESPAWN_COUNT, 0);
        SteamUserStats.SetStat("SAME_RESPAWN_COUNT", 0);
    }

    public void UpdateFails(LEVEL_ID levelId, int failCount)
    {
        string id = $"LEVEL_{(int)levelId}_FAILED_COUNT";
        bool ret = false;

        switch (levelId)
        {
            case LEVEL_ID.LEVEL_0:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_0_FAILED_COUNT, failCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_0_FAILED_COUNT));
                break;
            case LEVEL_ID.LEVEL_1:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_1_FAILED_COUNT, failCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_1_FAILED_COUNT));
                break;
            case LEVEL_ID.LEVEL_2:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_2_FAILED_COUNT, failCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_2_FAILED_COUNT));
                break;
            case LEVEL_ID.LEVEL_3:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_3_FAILED_COUNT, failCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_3_FAILED_COUNT));
                break;
            case LEVEL_ID.LEVEL_4:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_4_FAILED_COUNT, failCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_4_FAILED_COUNT));
                break;
        }

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"UpdateFails: {ret} | L0:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_0_FAILED_COUNT)} " +
                      $"L1:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_1_FAILED_COUNT)} " +
                      $"L2:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_2_FAILED_COUNT)} " +
                      $"L3:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_3_FAILED_COUNT)}");
    }

    public void UpdateWins(LEVEL_ID levelId, int winCount)
    {
        string id = $"LEVEL_{(int)levelId}_COMPLETED_COUNT";
        bool ret = false;

        switch (levelId)
        {
            case LEVEL_ID.LEVEL_0:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_0_COMPLETED_COUNT, winCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_0_COMPLETED_COUNT));
                break;
            case LEVEL_ID.LEVEL_1:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_1_COMPLETED_COUNT, winCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_1_COMPLETED_COUNT));
                break;
            case LEVEL_ID.LEVEL_2:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_2_COMPLETED_COUNT, winCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_2_COMPLETED_COUNT));
                break;
            case LEVEL_ID.LEVEL_3:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_3_COMPLETED_COUNT, winCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_3_COMPLETED_COUNT));
                break;
            case LEVEL_ID.LEVEL_4:
                _steamworksStats.SetValueInt(STATS_ID_STEAMWORKS.LEVEL_4_COMPLETED_COUNT, winCount);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_4_COMPLETED_COUNT));
                break;
        }

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"UpdateWins: {ret} | L0:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_0_COMPLETED_COUNT)} " +
                      $"L1:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_1_COMPLETED_COUNT)} " +
                      $"L2:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_2_COMPLETED_COUNT)} " +
                      $"L3:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_3_COMPLETED_COUNT)}");
    }

    public void UpdateLevelTime(LEVEL_ID levelId, float levelTime)
    {
        string id = $"LEVEL_{(int)levelId}_TOTAL_TIME";
        bool ret = false;

        switch (levelId)
        {
            case LEVEL_ID.LEVEL_0:
                _steamworksStats.SetValue(STATS_ID_STEAMWORKS.LEVEL_0_TOTAL_TIME, levelTime);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_0_TOTAL_TIME));
                break;
            case LEVEL_ID.LEVEL_1:
                _steamworksStats.SetValue(STATS_ID_STEAMWORKS.LEVEL_1_TOTAL_TIME, levelTime);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_1_TOTAL_TIME));
                break;
            case LEVEL_ID.LEVEL_2:
                _steamworksStats.SetValue(STATS_ID_STEAMWORKS.LEVEL_2_TOTAL_TIME, levelTime);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_2_TOTAL_TIME));
                break;
            case LEVEL_ID.LEVEL_3:
                _steamworksStats.SetValue(STATS_ID_STEAMWORKS.LEVEL_3_TOTAL_TIME, levelTime);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_3_TOTAL_TIME));
                break;
            case LEVEL_ID.LEVEL_4:
                _steamworksStats.SetValue(STATS_ID_STEAMWORKS.LEVEL_4_TOTAL_TIME, levelTime);
                ret = SteamUserStats.SetStat(id, _steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_4_TOTAL_TIME));
                break;
        }

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"UpdateLevelTime: {ret} | L0:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_0_TOTAL_TIME)} " +
                      $"L1:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_1_TOTAL_TIME)} " +
                      $"L2:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_2_TOTAL_TIME)} " +
                      $"L3:{_steamworksStats.GetIntValue(STATS_ID_STEAMWORKS.LEVEL_3_TOTAL_TIME)}");
    }

    #endregion

    #region Steam Sync

    public void RequestStatsForCurrentUser()
    {
        bool ret = SteamUserStats.RequestCurrentStats();
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("SteamUserStats.RequestCurrentStats() : " + ret);

        PullStatToMap(STATS_ID_STEAMWORKS.TOTAL_GAMES_PLAYED);
        PullStatToMap(STATS_ID_STEAMWORKS.TOTAL_COINS);
        PullStatToMap(STATS_ID_STEAMWORKS.TOTAL_DAMAGE);
        PullStatToMap(STATS_ID_STEAMWORKS.TOTAL_DISTANCE);
        PullStatToMap(STATS_ID_STEAMWORKS.TOTAL_DROPS);
        PullStatToMap(STATS_ID_STEAMWORKS.CASKET_REPAIR_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.CHAOS_COINS);
        PullStatToMap(STATS_ID_STEAMWORKS.HAUNTED_GRAVES_GRABS_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.SKULL_KNOCK_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.MUD_SINK_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.TOTAL_COLLECTABLE_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.GOLD_COINS);
        PullStatToMap(STATS_ID_STEAMWORKS.SAME_RESPAWN_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.BRIDGE_FALL_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.BREAK_ITEMS_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.NO_DROP_GAMES_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.PERFECT_CONDITION_GAMES_COUNT);

        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_0_COMPLETED_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_1_COMPLETED_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_2_COMPLETED_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_3_COMPLETED_COUNT);

        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_0_FAILED_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_1_FAILED_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_2_FAILED_COUNT);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_3_FAILED_COUNT);

        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_0_TOTAL_TIME);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_1_TOTAL_TIME);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_2_TOTAL_TIME);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_3_TOTAL_TIME);

        PullStatToMap(STATS_ID_STEAMWORKS.FALL_COUNT);

        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_0_EASY_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_1_EASY_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_2_EASY_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_3_EASY_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_4_EASY_HIGHSCORE);

        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_0_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_1_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_2_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_3_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_4_HIGHSCORE);

        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_0_HARD_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_1_HARD_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_2_HARD_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_3_HARD_HIGHSCORE);
        PullStatToMap(STATS_ID_STEAMWORKS.LEVEL_4_HARD_HIGHSCORE);
    }

    public void GetAllStatsForUser(CSteamID steamID)
    {
        SteamAPICall_t handle = SteamUserStats.RequestUserStats(steamID);
        OnUserStatsReceivedCallResult.Set(handle);
        Debug.Log("SteamUserStats.RequestUserStats(" + steamID + ") : " + handle);
    }

    private void StoreStats()
    {
        bool ret = SteamUserStats.StoreStats();
        Debug.Log("SteamUserStats.StoreStats() : " + ret);
    }

    public void PrintCurrentStats()
    {
        if (HilltopConstants.DEBUG_STEAM_API)
        {
            Debug.Log("Current Steam Stats:");
            foreach (var stat in _steamworksStats._statsMap)
            {
                Debug.Log($"{stat.Key}: {stat.Value}");
            }
        }
    }

    #endregion

    #region Steam Callbacks

    private void OnUserStatsReceived(UserStatsReceived_t pCallback)
    {
        if (pCallback.m_eResult == EResult.k_EResultOK)
        {
            if (HilltopConstants.DEBUG_STEAM_API)
            {
                Debug.Log("[" + UserStatsReceived_t.k_iCallback + " - UserStatsReceived] - Stats successfully loaded from Steam!");
                Debug.Log("Game ID: " + pCallback.m_nGameID + " -- Result: " + pCallback.m_eResult + " -- Steam ID: " + pCallback.m_steamIDUser);
            }

            // Notify that stats have been successfully loaded
            SteamEvents.Event_OnStatsResults();

            // if (HilltopConstants.DEBUG_STEAM_API)
            //     PrintCurrentStats();
        }
        else
        {
            Debug.LogWarning($"Failed to receive stats from Steam. Result: {pCallback.m_eResult}");
        }
    }

    private void OnUserStatsReceived(UserStatsReceived_t pCallback, bool bIOFailure)
    {
        if (bIOFailure)
        {
            Debug.LogError("IO Failure when receiving user stats from Steam");
            return;
        }

        OnUserStatsReceived(pCallback);
    }

    private void OnUserStatsStored(UserStatsStored_t pCallback)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("[" + UserStatsStored_t.k_iCallback + " - UserStatsStored] - " + pCallback.m_nGameID + " -- " + pCallback.m_eResult);
    }

    private void OnUserAchievementStored(UserAchievementStored_t pCallback)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("[" + UserAchievementStored_t.k_iCallback + " - UserAchievementStored] - " +
                      pCallback.m_nGameID + " -- " + pCallback.m_bGroupAchievement + " -- " +
                      pCallback.m_rgchAchievementName + " -- " + pCallback.m_nCurProgress + " -- " + pCallback.m_nMaxProgress);
    }

    private void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t pCallback, bool bIOFailure)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("[" + NumberOfCurrentPlayers_t.k_iCallback + " - NumberOfCurrentPlayers] - " +
                      pCallback.m_bSuccess + " -- " + pCallback.m_cPlayers);
    }

    private void OnUserStatsUnloaded(UserStatsUnloaded_t pCallback)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("[" + UserStatsUnloaded_t.k_iCallback + " - UserStatsUnloaded] - " + pCallback.m_steamIDUser);
    }

    private void OnUserAchievementIconFetched(UserAchievementIconFetched_t pCallback)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("[" + UserAchievementIconFetched_t.k_iCallback + " - UserAchievementIconFetched] - " +
                      pCallback.m_nGameID + " -- " + pCallback.m_rgchAchievementName + " -- " +
                      pCallback.m_bAchieved + " -- " + pCallback.m_nIconHandle);

        m_Icon = SteamUtilities.GetSteamImageAsTexture2D(pCallback.m_nIconHandle);
    }

    private void OnGlobalAchievementPercentagesReady(GlobalAchievementPercentagesReady_t pCallback, bool bIOFailure)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("[" + GlobalAchievementPercentagesReady_t.k_iCallback + " - GlobalAchievementPercentagesReady] - " +
                      pCallback.m_nGameID + " -- " + pCallback.m_eResult);
    }

    private void OnLeaderboardUGCSet(LeaderboardUGCSet_t pCallback, bool bIOFailure)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("[" + LeaderboardUGCSet_t.k_iCallback + " - LeaderboardUGCSet] - " +
                      pCallback.m_eResult + " -- " + pCallback.m_hSteamLeaderboard);
    }

    private void OnGlobalStatsReceived(GlobalStatsReceived_t pCallback, bool bIOFailure)
    {
        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log("[" + GlobalStatsReceived_t.k_iCallback + " - GlobalStatsReceived] - " +
                      pCallback.m_nGameID + " -- " + pCallback.m_eResult);
    }

    #endregion

    #region Helpers

    private bool EnsureStatExists(STATS_ID_STEAMWORKS stat)
    {
        if (_steamworksStats._statsMap.ContainsKey(stat)) return true;

        SteamEvents.Event_OnSteamworksError("Stats not found... Did you forget to add it to the stats map?");
        return false;
    }

    private void PullStatToMap(STATS_ID_STEAMWORKS stat)
    {
        if (_steamworksStats._statTypes[stat] == StatType.Integer)
        {
            int value;
            bool ret = SteamUserStats.GetStat(stat.ToString(), out value);
            if (ret) _steamworksStats.SetValue(stat, value);
        }
        else
        {
            float value;
            bool ret = SteamUserStats.GetStat(stat.ToString(), out value);
            if (ret) _steamworksStats.SetValue(stat, value);
        }
    }

    #endregion

    #region Manual Achievement Triggering

    /// <summary>
    /// Manually unlock a Steam achievement
    /// </summary>
    public bool UnlockAchievement(string achievementID)
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam not initialized! Cannot unlock achievement.");
            return false;
        }

        // Check if achievement is already unlocked
        if (IsAchievementUnlocked(achievementID))
        {
            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"Achievement '{achievementID}' is already unlocked. Skipping.");
            return true; // Return true since the achievement is already in the desired state
        }

        bool success = SteamUserStats.SetAchievement(achievementID);
        if (success)
        {
            // Store the achievement (this actually commits it to Steam)
            bool stored = SteamUserStats.StoreStats();

            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"Achievement '{achievementID}' unlocked: {success}, stored: {stored}");

            return stored;
        }
        else
        {
            Debug.LogError($"Failed to unlock achievement '{achievementID}'");
            return false;
        }
    }

    /// <summary>
    /// Check if an achievement is already unlocked
    /// </summary>
    public bool IsAchievementUnlocked(string achievementID)
    {
        if (!SteamManager.Initialized) return false;

        bool achieved = false;
        bool success = SteamUserStats.GetAchievement(achievementID, out achieved);

        if (HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"Achievement '{achievementID}' status: success={success}, achieved={achieved}");

        return success && achieved;
    }
    #endregion
}