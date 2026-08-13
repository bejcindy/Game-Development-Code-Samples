using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;

public class SteamEvents
{
    /*
     *
     * Steam events
     *
     */

    public delegate void steamworksConnected(bool connected);
    public static event steamworksConnected SteamworksConnected;

    public delegate void steamworksStatUpdated(STATS_ID_STEAMWORKS statsID);
    public static event steamworksStatUpdated SteamworksStatUpdated;

    public delegate void updateSteamworksStat(STATS_ID_STEAMWORKS statsID, int amount);
    public static event updateSteamworksStat UpdateSteamworksStat;

    public delegate void incrementSteamworksStat(STATS_ID_STEAMWORKS statsID);
    public static event incrementSteamworksStat IncrementSteamworksStat;

    public delegate void levelCompleteSteamworks(LevelData levelData);
    public static event levelCompleteSteamworks LevelCompleteSteamworks;

    public delegate void steamworksAchievementUpdated(ACHIEVEMENTS_ID_STEAMWORKS achievementID);
    public static event steamworksAchievementUpdated SteamworksAchievementUpdated;

    public delegate void updateSteamworksAchievement(ACHIEVEMENTS_ID_STEAMWORKS achievementID, bool unlocked);
    public static event updateSteamworksAchievement UpdateSteamworksAchievement;

    public delegate void updateSteamworksLocalization(int localizationID);
    public static event updateSteamworksLocalization UpdateSteamworksLocalization;

    public delegate void steamOverlaySet(bool enabled, SteamService steamService);
    public static event steamOverlaySet SteamOverlaySet;

    public delegate void requestLeaderboardForLevel(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false);
    public static event requestLeaderboardForLevel RequestLeaderboardForLevel;

    public delegate void requestSteamworksStats();
    public static event requestSteamworksStats RequestSteamworksStats;

    public delegate void onLeaderboardLoaded(LeaderboardFindResult_t pCallback, LEVEL_ID levelID, DIFFICULTY difficulty, bool failed, bool coop = false);
    public static event onLeaderboardLoaded OnLeaderboardLoaded;

    public delegate void onLeaderboardResults(List<LeaderboardEntry> m_Leaderboard, LEVEL_ID levelID);
    public static event onLeaderboardResults OnLeaderboardResults;

    public delegate void onStatsResults();
    public static event onStatsResults OnStatsResults;

    public delegate void onStatResponse(STATS_ID_STEAMWORKS statsID, float value);
    public static event onStatResponse OnStatResponse;


    public delegate void onSteamworksError(string error);
    public static event onSteamworksError OnSteamworksError;

    public delegate void sendGameNotification(string text);
    public static event sendGameNotification SendGameNotification;

    public delegate void playerJoinedGame(PlayerNumber playerNumber);
    public static event playerJoinedGame PlayerJoinedGame;

    public static void Event_LevelCompleteSteamworks(LevelData levelData)
    {
        if (LevelCompleteSteamworks != null)
            LevelCompleteSteamworks(levelData);
    }

    public static void Event_SendGameNotification(string text)
    {
        if (SendGameNotification != null)
            SendGameNotification(text);
    }

    public static void Event_SteamworksConnected(bool connected)
    {
        if (SteamworksConnected != null)
            SteamworksConnected(connected);
    }

    public static void Event_UpdateSteamworksAchievement(ACHIEVEMENTS_ID_STEAMWORKS achievementID, bool unlocked)
    {
        if (UpdateSteamworksAchievement != null)
            UpdateSteamworksAchievement(achievementID, unlocked);
    }

    public static void Event_SteamworksAchievementUpdated(ACHIEVEMENTS_ID_STEAMWORKS achievementID)
    {
        if (SteamworksAchievementUpdated != null)
            SteamworksAchievementUpdated(achievementID);
    }

    public static void Event_SteamworksStatUpdated(STATS_ID_STEAMWORKS statsID)
    {
        if (SteamworksStatUpdated != null)
            SteamworksStatUpdated(statsID);
    }

    public static void Event_UpdateSteamworksStat(STATS_ID_STEAMWORKS statsID, int amount)
    {
        if (UpdateSteamworksStat != null)
            UpdateSteamworksStat(statsID, amount);
    }

    public static void Event_OnStatResponse(STATS_ID_STEAMWORKS statsID, float amount)
    {
        if (OnStatResponse != null)
            OnStatResponse(statsID, amount);
    }

    public static void Event_IncrementSteamworksStat(STATS_ID_STEAMWORKS statsID)
    {
        if (IncrementSteamworksStat != null)
            IncrementSteamworksStat(statsID);
    }

    public static void Event_UpdateSteamworksLocalization(int localizationID)
    {
        if (UpdateSteamworksLocalization != null)
            UpdateSteamworksLocalization(localizationID);
    }

    public static void Event_SteamOverlaySet(bool enabled, SteamService steamService)
    {
        if (SteamOverlaySet != null)
            SteamOverlaySet(enabled, steamService);
    }

    public static void Event_RequestLeaderboardForLevel(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        if (RequestLeaderboardForLevel != null)
            RequestLeaderboardForLevel(levelID, difficulty, coop);
    }

    public static void Event_RequestSteamworksStats()
    {
        if (RequestSteamworksStats != null)
            RequestSteamworksStats();
    }
    public static void Event_OnLeaderboardLoaded(LeaderboardFindResult_t pCallback, LEVEL_ID levelID, DIFFICULTY difficulty, bool failed, bool coop = false)
    {
        if (OnLeaderboardLoaded != null)
            OnLeaderboardLoaded(pCallback, levelID, difficulty, failed, coop);
    }

    public static void Event_OnLeaderboardResult(List<LeaderboardEntry> m_Leaderboard, LEVEL_ID levelID)
    {
        if (OnLeaderboardResults != null)
            OnLeaderboardResults(m_Leaderboard, levelID);
    }
    public static void Event_OnStatsResults()
    {
        if (OnStatsResults != null)
            OnStatsResults();
    }

    public static void Event_OnSteamworksError(string error)
    {
        if (OnSteamworksError != null)
            OnSteamworksError(error);
    }

    public static void Event_PlayerJoinedGame(PlayerNumber playerNumber)
    {
        if (PlayerJoinedGame != null)
            PlayerJoinedGame(playerNumber);
    }
}
