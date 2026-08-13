using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelSave : Singleton<LevelSave>
{
    #region Private Fields
    private bool m_bInitialized = false;
    private ES3Settings es3Settings;
    private const string ENCRYPTION_KEY = "TheUndertaker-04-28-2025";
    private const string MIGRATION_FLAG_KEY = "DIFFICULTY_MIGRATION_COMPLETE_V1";
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        try
        {
            m_bInitialized = InitializeLevelSaves();
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError($"Error loading or creating level save: {e.Message}", this);
            Application.Quit();
            return;
        }

        if (!m_bInitialized)
        {
            Debug.LogError("Level save initialization failed", this);
            Application.Quit();
        }
    }
    #endregion

    #region Initialization
    public bool InitializeLevelSaves()
    {
        es3Settings = new ES3Settings(true);

        if (!HilltopConstants.DEBUG_GAMESAVE)
        {
            es3Settings.encryptionType = ES3.EncryptionType.AES;
            es3Settings.encryptionPassword = ENCRYPTION_KEY;
        }

        // Only run migration once per save data
        if (!ES3.KeyExists(MIGRATION_FLAG_KEY, es3Settings))
        {
            MigrateAllLegacySaves();
            // Mark migration as complete
            ES3.Save(MIGRATION_FLAG_KEY, true, es3Settings);
            Debug.Log("[LevelSave] Migration complete and flagged");
        }
        else
        {
            Debug.Log("[LevelSave] Migration already completed, skipping");
        }

        GameLogic.Instance.NotifyLevelSaveLoadedLoaded();
        return true;
    }

    void OnEnable()
    {
        EventsMaster.OnGameSaveLoaded += Event_OnGameSaveLoaded;
        EventsMaster.OnLevelSaveLoaded += Event_OnLevelSaveLoaded;
        EventsMaster.RequestSaveLevelData += Event_RequestSaveLevelData;
    }

    void OnDisable()
    {
        EventsMaster.OnGameSaveLoaded -= Event_OnGameSaveLoaded;
        EventsMaster.OnLevelSaveLoaded -= Event_OnLevelSaveLoaded;
        EventsMaster.RequestSaveGameSettings -= Event_RequestSaveGameSettings;
    }


    private void Event_RequestSaveGameSettings()
    {
        //throw new NotImplementedException();
    }

    private void Event_RequestSaveLevelData(LevelData levelData)
    {
        if (m_bInitialized)
            SaveLevelData(levelData);
        else
            Debug.LogError("Level save not initialized");
    }

    private void Event_OnLevelSaveLoaded(LevelSave levelSave)
    {
        //throw new NotImplementedException();
    }

    private void Event_OnGameSaveLoaded(GameSave gameSave)
    {
        //throw new NotImplementedException();
    }


    public bool InitLevelSaves()
    {
        es3Settings = new ES3Settings(true);

        if (!HilltopConstants.DEBUG_GAMESAVE)
        {
            es3Settings.encryptionType = ES3.EncryptionType.AES;
            es3Settings.encryptionPassword = ENCRYPTION_KEY;
        }

        GameLogic.Instance.NotifyLevelSaveLoadedLoaded();
        return true;
    }

    public void InitESSettings(ES3Settings settings)
    {
        es3Settings = settings;
    }
    #endregion

    #region Public API - Load Operations   

    public LevelData GetLevelData(LEVEL_ID levelId, DIFFICULTY difficulty, ulong friendID = 0)
    {
        // return GetLevelData((int)levelId, difficulty);
        if (GameLogic.Instance.GameState == GameState.Loading)
        {
            Debug.LogWarning("Request made while game is loading");
            return null;
        }

        return LoadLevelDataFromDisk((int)levelId, difficulty, friendID);
    }
    #endregion

    #region Public API - Save Operations
    /// <summary>
    /// Saves level data, merging with existing data to preserve best scores and statistics.
    /// </summary>
    /// <param name="newLevelData">The new level data to save</param>
    /// <param name="friendID">The ID of the friend associated with this level data</param>
    /// <returns>True if save was successful</returns>
    public bool SaveLevelData(LevelData newLevelData)
    {
        if (newLevelData == null)
        {
            Debug.LogWarning("Cannot save null level data");
            return false;
        }
        string levelKey;
        if (newLevelData.friendID != 0)
            levelKey = GenerateLevelKey(newLevelData.level, newLevelData.difficulty, newLevelData.friendID);
        else
            levelKey = GenerateLevelKey(newLevelData.level, newLevelData.difficulty);
        Debug.Log($"Saving level data for level {newLevelData.level} on {newLevelData.difficulty} difficulty with friend {newLevelData.friendID}");

        try
        {
            LevelData mergedData;

            if (ES3.KeyExists(levelKey, es3Settings))
            {
                // Load existing data and merge with new data
                LevelData existingData = LoadLevelDataFromDisk(newLevelData.level, newLevelData.difficulty);
                mergedData = MergeLevelData(existingData, newLevelData);
            }
            else
            {
                // First time saving this level
                Debug.Log($"Creating new level data for level {newLevelData.level} on {newLevelData.difficulty} difficulty");
                mergedData = CreateNewLevelData(newLevelData);
            }

            if (mergedData == null)
            {
                Debug.LogError("Failed to create merged level data");
                return false;
            }

            // Save to disk and update user stats
            bool saveSuccess = SaveLevelDataToDisk(mergedData);
            //if (saveSuccess)
            //{
            //    GameLogic.Instance.UpdateUserStatsFromLevelData(mergedData);
            //}

            return saveSuccess;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving level data: {e.Message}");
            return false;
        }
    }
    #endregion

    #region Public API - Delete Operations
    public bool DeleteLevelData(int levelNumber, DIFFICULTY difficulty)
    {
        string levelKey = GenerateLevelKey(levelNumber, difficulty);

        if (ES3.KeyExists(levelKey, es3Settings))
        {
            try
            {
                ES3.DeleteKey(levelKey, es3Settings);
                Debug.Log($"Deleted level data for level {levelNumber}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete level {levelNumber}: {e.Message}");
                return false;
            }
        }

        Debug.LogWarning($"Cannot delete level {levelNumber} - data doesn't exist");
        return false;
    }

    public void DeleteAllLevelData()
    {
        Debug.LogWarning("Deleting all level data");

        for (int i = 1; i <= GameLogic.Instance.TOTAL_LEVELS_COUNT; i++)
        {
            // DeleteLevelData(i);
            for (int j = 0; j < Enum.GetNames(typeof(DIFFICULTY)).Length; j++)
            {
                DeleteLevelData(i, (DIFFICULTY)j);
            }
        }
    }
    #endregion

    #region Public API - Progress Tracking

    public int GetTotalCoinProgressForAllLevels(GameSave gameSave)
    {
        GameSettings gameSettings = gameSave?.GetGameSettings();

        if (gameSettings == null)
        {
            Debug.LogWarning("Error loading game settings, returning 0 coins");
            return 0;
        }

        int totalCoins = 0;

        for (int i = 0; i < GameLogic.Instance.TOTAL_LEVELS_COUNT; i++)
        {
            // Count coins from NORMAL difficulty (existing player records)
            LevelData levelData = GetLevelData((LEVEL_ID)i, DIFFICULTY.NORMAL);

            if (levelData?.highestTotalCoinCount > 0)
            {
                totalCoins += levelData.highestTotalCoinCount;
            }
        }

        return totalCoins;
    }
    #endregion

    #region Steam Sync Management

    /// <summary>
    /// Checks if the local best time for a level needs to be synced with Steam.
    /// </summary>
    /// <param name="levelID">The level to check</param>
    /// <param name="difficulty">The difficulty to check</param>
    /// <param name="steamBestTimeMs">The best time from Steam leaderboard in milliseconds (0 if no entry exists)</param>
    /// <param name="coop">If true, checks best time across all coop sessions; if false, checks solo time</param>
    /// <returns>True if local time should be uploaded to Steam</returns>
    public bool ShouldSyncWithSteam(LEVEL_ID levelID, DIFFICULTY difficulty, int steamBestTimeMs, bool coop = false)
    {
        if (!coop)
        {
            // Solo mode - check single save
            LevelData localData = GetLevelData(levelID, difficulty);

            if (localData == null)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[LevelSave] No local data for {levelID} ({difficulty}), no sync needed");
                return false;
            }

            // Check if we have a valid local best time
            if (localData.bestLevelTime <= 0)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[LevelSave] No valid local best time for {levelID} ({difficulty}) - bestLevelTime={localData.bestLevelTime}");
                return false;
            }

            int localBestTimeMs = (int)(localData.bestLevelTime * 1000);

            // Case 1: No Steam entry exists (steamBestTimeMs == 0)
            if (steamBestTimeMs == 0)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[LevelSave] No Steam entry for {levelID} ({difficulty}), should upload local: {localBestTimeMs}ms");
                return true;
            }

            // Case 2: Local time is better than Steam time
            if (localBestTimeMs < steamBestTimeMs)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[LevelSave] Local time ({localBestTimeMs}ms) is better than Steam ({steamBestTimeMs}ms) for {levelID} ({difficulty})");
                return true;
            }

            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[LevelSave] Steam time ({steamBestTimeMs}ms) is better or equal to local ({localBestTimeMs}ms)");
            return false;
        }
        else
        {
            // Coop mode - find best time across all coop sessions
            var (bestTimeMs, bestFriendID) = GetLocalBestCoopTimeWithFriend(levelID, difficulty);

            if (bestTimeMs == 0)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[LevelSave] No local coop data for {levelID} ({difficulty}), no sync needed");
                return false;
            }

            // Case 1: No Steam entry exists
            if (steamBestTimeMs == 0)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[LevelSave] No Steam coop entry for {levelID} ({difficulty}), should upload local: {bestTimeMs}ms (friend: {bestFriendID})");
                return true;
            }

            // Case 2: Local time is better than Steam time
            if (bestTimeMs < steamBestTimeMs)
            {
                if (HilltopConstants.DEBUG_STEAM_API)
                    Debug.Log($"[LevelSave] Local coop time ({bestTimeMs}ms) is better than Steam ({steamBestTimeMs}ms) for {levelID} ({difficulty}) (friend: {bestFriendID})");
                return true;
            }

            if (HilltopConstants.DEBUG_STEAM_API)
                Debug.Log($"[LevelSave] Steam coop time ({steamBestTimeMs}ms) is better or equal to local ({bestTimeMs}ms)");
            return false;
        }
    }

    /// <summary>
    /// Marks a level as synced with Steam.
    /// </summary>
    /// <param name="levelID">The level to mark as synced</param>
    /// <param name="difficulty">The difficulty to mark as synced</param>
    /// <returns>True if successfully marked as synced</returns>
    public bool MarkAsSyncedWithSteam(LEVEL_ID levelID, DIFFICULTY difficulty)
    {
        LevelData levelData = GetLevelData(levelID, difficulty);

        if (levelData == null)
        {
            Debug.LogError($"[LevelSave] Cannot mark {levelID}{difficulty} as synced - no data exists");
            return false;
        }

        levelData.didSyncWithSteam = true;
        bool success = SaveLevelDataToDisk(levelData);

        if (success && HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[LevelSave] ✓ Marked {levelID}{difficulty} as synced with Steam");

        return success;
    }

    /// <summary>
    /// Gets the local best time for a level in milliseconds.
    /// </summary>
    /// <param name="levelID">The level to get the best time for</param>
    /// <param name="difficulty">The difficulty to get the best time for</param>
    /// <param name="coop">If true, finds the best time among all coop sessions; if false, gets solo best time</param>
    /// <returns>Best time in milliseconds, or 0 if no valid time exists</returns>
    public int GetLocalBestTimeMs(LEVEL_ID levelID, DIFFICULTY difficulty, bool coop = false)
    {
        if (!coop)
        {
            // Get solo best time
            LevelData levelData = GetLevelData(levelID, difficulty);
            if (levelData == null || levelData.bestLevelTime <= 0)
                return 0;
            return (int)(levelData.bestLevelTime * 1000);
        }
        else
        {
            // Get best time across all coop sessions
            var (bestTimeMs, _) = GetLocalBestCoopTimeWithFriend(levelID, difficulty);
            return bestTimeMs;
        }
    }

    /// <summary>
    /// Gets the local best coop time and the friend ID associated with that time.
    /// </summary>
    /// <param name="levelID">The level to get the best time for</param>
    /// <param name="difficulty">The difficulty to get the best time for</param>
    /// <returns>Tuple of (best time in milliseconds, friendID). Returns (0, 0) if no valid time exists</returns>
    public (int timeMs, ulong friendID) GetLocalBestCoopTimeWithFriend(LEVEL_ID levelID, DIFFICULTY difficulty)
    {
        float bestTime = 0f;
        ulong bestFriendID = 0;
        string keyPrefix = $"{GameSaveIDs.LEVEL_DICTIONARY_ID}{(int)levelID}_{difficulty}_";

        // Get all keys from ES3
        string[] allKeys = ES3.GetKeys(es3Settings);

        foreach (string key in allKeys)
        {
            // Check if this key matches a coop session for this level/difficulty
            if (key.StartsWith(keyPrefix))
            {
                try
                {
                    LevelData coopData = ES3.Load<LevelData>(key, es3Settings);
                    if (coopData != null && coopData.bestLevelTime > 0 && coopData.friendID != 0)
                    {
                        if (bestTime == 0f || coopData.bestLevelTime < bestTime)
                        {
                            bestTime = coopData.bestLevelTime;
                            bestFriendID = coopData.friendID;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[LevelSave] Error loading coop data from key {key}: {e.Message}");
                }
            }
        }

        return bestTime > 0 ? ((int)(bestTime * 1000), bestFriendID) : (0, 0);
    }



    /// <summary>
    /// Resets the Steam sync flag for a level, forcing it to re-sync on next check.
    /// </summary>
    /// <param name="levelID">The level to reset sync flag for</param>
    /// <param name="difficulty">The difficulty to reset sync flag for</param>
    /// <returns>True if successfully reset</returns>
    public bool ResetSteamSyncFlag(LEVEL_ID levelID, DIFFICULTY difficulty)
    {
        LevelData levelData = GetLevelData(levelID, difficulty);

        if (levelData == null)
        {
            Debug.LogError($"[LevelSave] Cannot reset sync flag for {levelID} - no data exists");
            return false;
        }

        levelData.didSyncWithSteam = false;
        bool success = SaveLevelDataToDisk(levelData);

        if (success && HilltopConstants.DEBUG_STEAM_API)
            Debug.Log($"[LevelSave] ✓ Reset sync flag for {levelID}");

        return success;
    }

    #endregion

    #region Private Helper Methods
    /// <summary>
    /// Migrates all legacy saves (without difficulty suffix) to NORMAL difficulty format.
    /// Called once during initialization.
    /// </summary>
    private void MigrateAllLegacySaves()
    {
        Debug.Log($"[LevelSave] Starting migration check for {GameLogic.Instance.TOTAL_LEVELS_COUNT} levels...");
        int migratedCount = 0;

        for (int i = 0; i <= GameLogic.Instance.TOTAL_LEVELS_COUNT; i++)
        {
            string legacyKey = GenerateLegacyLevelKey(i);
            string normalKey = GenerateLevelKey(i, DIFFICULTY.NORMAL);

            bool legacyExists = ES3.KeyExists(legacyKey, es3Settings);
            bool normalExists = ES3.KeyExists(normalKey, es3Settings);

            Debug.Log($"[LevelSave] Level {i}: legacyKey='{legacyKey}' exists={legacyExists}, normalKey='{normalKey}' exists={normalExists}");

            // Only migrate if legacy exists and new format doesn't
            if (legacyExists && !normalExists)
            {
                try
                {
                    LevelData legacyData = ES3.Load<LevelData>(legacyKey, es3Settings);

                    Debug.Log($"[LevelSave] Found legacy data for level {i}:");
                    Debug.Log($"  - bestLevelTime: {legacyData.bestLevelTime}");
                    Debug.Log($"  - levelTime: {legacyData.levelTime}");
                    Debug.Log($"  - totalLevelTime: {legacyData.totalLevelTime}");
                    Debug.Log($"  - completed: {legacyData.completed}");
                    Debug.Log($"  - completedCount: {legacyData.completedCount}");
                    Debug.Log($"  - highScore: {legacyData.highScore}");

                    legacyData.difficulty = DIFFICULTY.NORMAL;

                    ES3.Save(normalKey, legacyData, es3Settings);

                    // Verify the save worked
                    LevelData verifyData = ES3.Load<LevelData>(normalKey, es3Settings);
                    Debug.Log($"[LevelSave] Migrated level {i} to NORMAL difficulty - verified bestTime={verifyData.bestLevelTime}");

                    migratedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LevelSave] Failed to migrate level {i}: {e.Message}");
                }
            }

            // Reset didSyncWithSteam for all existing saves (all difficulties)
            // This is needed because old saves were uploaded to the wrong leaderboards before difficulty separation
            foreach (DIFFICULTY diff in System.Enum.GetValues(typeof(DIFFICULTY)))
            {
                string diffKey = GenerateLevelKey(i, diff);
                if (ES3.KeyExists(diffKey, es3Settings))
                {
                    try
                    {
                        LevelData data = ES3.Load<LevelData>(diffKey, es3Settings);
                        if (data.didSyncWithSteam)
                        {
                            Debug.Log($"[LevelSave] Resetting sync flag for level {i} ({diff}) to allow re-upload to correct difficulty leaderboard");
                            data.didSyncWithSteam = false;
                            data.difficulty = diff; // Ensure difficulty is correct
                            ES3.Save(diffKey, data, es3Settings);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[LevelSave] Failed to reset sync flag for level {i} ({diff}): {e.Message}");
                    }
                }
            }
        }

        if (migratedCount > 0)
        {
            Debug.Log($"[LevelSave] ✓ Successfully migrated {migratedCount} legacy saves to NORMAL difficulty");
        }
        else
        {
            Debug.Log($"[LevelSave] No legacy saves found to migrate");
        }
    }

    private string GenerateLevelKey(int levelNumber, DIFFICULTY difficulty, ulong friendID = 0)
    {
        if (friendID == 0)
            return $"{GameSaveIDs.LEVEL_DICTIONARY_ID}{levelNumber}_{difficulty}";
        else
            return $"{GameSaveIDs.LEVEL_DICTIONARY_ID}{levelNumber}_{difficulty}_{friendID}";
    }

    private string GenerateLegacyLevelKey(int levelNumber)
    {
        return GameSaveIDs.LEVEL_DICTIONARY_ID + levelNumber.ToString();
    }

    private LevelData LoadLevelDataFromDisk(int levelNumber, DIFFICULTY difficulty, ulong friendID = 0)
    {
        string levelKey;

        if (friendID == 0)
            levelKey = GenerateLevelKey(levelNumber, difficulty);
        else
            levelKey = GenerateLevelKey(levelNumber, difficulty, friendID);

        // Check if new format exists
        if (ES3.KeyExists(levelKey, es3Settings))
        {
            try
            {
                LevelData levelData = ES3.Load<LevelData>(levelKey, es3Settings);

                // Fix for legacy saves without difficulty field - ensure it matches the requested difficulty
                if (levelData.difficulty != difficulty)
                {
                    Debug.Log($"Correcting difficulty mismatch: loaded {levelData.difficulty}, expected {difficulty}");
                    levelData.difficulty = difficulty;
                    // Reset sync flag since this is for a different difficulty/leaderboard
                    levelData.didSyncWithSteam = false;
                }

                Debug.Log($"Loaded level data for level {levelNumber} on {difficulty} difficulty - bestTime={levelData.bestLevelTime}");
                return levelData;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading level {levelNumber} on {difficulty} difficulty: {e.Message}");
                return null;
            }
        }

        // Try to migrate from legacy format (only for NORMAL difficulty)
        if (difficulty == DIFFICULTY.NORMAL)
        {
            string legacyKey = GenerateLegacyLevelKey(levelNumber);
            if (ES3.KeyExists(legacyKey, es3Settings))
            {
                Debug.Log($"Found legacy save for level {levelNumber}. Migrating to NORMAL difficulty...");
                try
                {
                    LevelData legacyData = ES3.Load<LevelData>(legacyKey, es3Settings);

                    // Set difficulty to NORMAL
                    legacyData.difficulty = DIFFICULTY.NORMAL;

                    // Save in new format
                    ES3.Save(levelKey, legacyData, es3Settings);

                    Debug.Log($"Successfully migrated level {levelNumber} to NORMAL difficulty");
                    return legacyData;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error migrating legacy save for level {levelNumber}: {e.Message}");
                    return null;
                }
            }
        }

        Debug.LogWarning($"Level data doesn't exist for level {levelNumber} on {difficulty} difficulty with friend {friendID}");
        return null;
    }

    private LevelData LoadLevelDataFromDiskLegacy(int levelNumber)
    {
        string levelKey = GenerateLegacyLevelKey(levelNumber);

        if (!ES3.KeyExists(levelKey, es3Settings))
        {
            Debug.LogWarning($"Level data doesn't exist for level {levelNumber}");
            return null;
        }

        try
        {
            LevelData levelData = ES3.Load<LevelData>(levelKey, es3Settings);
            Debug.Log($"Loaded level data for level {levelNumber}");
            return levelData;
        }
        catch (System.IO.IOException)
        {
            Debug.LogError("File is open elsewhere or insufficient storage space");
        }
        catch (System.Security.SecurityException)
        {
            Debug.LogError("Insufficient permissions to load level data");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading level {levelNumber}: {e.Message}");
        }

        return null;
    }

    private bool SaveLevelDataToDisk(LevelData levelData)
    {
        string levelKey;
        if (levelData.friendID == 0)
            levelKey = GenerateLevelKey(levelData.level, levelData.difficulty);
        else
            levelKey = GenerateLevelKey(levelData.level, levelData.difficulty, levelData.friendID);

        try
        {
            if (levelData.friendID == 0)
                Debug.Log($"[LevelSave] Saving level {levelData.level} ({levelData.difficulty}) to disk: bestLevelTime={levelData.bestLevelTime}, levelTime={levelData.levelTime}");
            else
                Debug.Log($"[LevelSave] Saving Coop level {levelData.level} ({levelData.difficulty}) with {levelData.friendID} to disk: bestLevelTime={levelData.bestLevelTime}, levelTime={levelData.levelTime}");
            ES3.Save(levelKey, levelData, es3Settings);

            // Verify the save
            LevelData verifyData = ES3.Load<LevelData>(levelKey, es3Settings);
            Debug.Log($"[LevelSave] Verified save: bestLevelTime={verifyData.bestLevelTime}");

            return true;
        }
        catch (System.IO.IOException)
        {
            Debug.LogError("File is open elsewhere or insufficient storage space");
        }
        catch (System.Security.SecurityException)
        {
            Debug.LogError("Insufficient permissions to save level data");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving level data: {e.Message}");
        }

        return false;
    }

    private LevelData CreateNewLevelData(LevelData sourceData)
    {
        return new LevelData
        {
            level = sourceData.level,
            difficulty = sourceData.difficulty,
            friendID = sourceData.friendID,
            levelTime = sourceData.levelTime,
            bestLevelTime = sourceData.levelTime,
            totalLevelTime = sourceData.levelTime,
            coinCount = sourceData.coinCount,
            highestTotalCoinCount = sourceData.coinCount,
            bodyDecay = sourceData.bodyDecay,
            lowestBodyDecay = sourceData.bodyDecay,
            highScore = sourceData.highScore,
            completed = sourceData.completed,
            completedCount = sourceData.completed ? 1 : 0,
            timeStamp = sourceData.timeStamp != default(DateTime) ? sourceData.timeStamp : DateTime.Now,
            totalDropCount = sourceData.totalDropCount,
            restartCount = sourceData.restartCount,
            gameVersionID = sourceData.gameVersionID
        };
    }

    private LevelData MergeLevelData(LevelData existingData, LevelData newData)
    {
        if (existingData == null || newData == null)
        {
            Debug.LogError("Cannot merge null level data");
            return null;
        }

        // Calculate the merged best time
        float mergedBestTime = GetBestTime(existingData.bestLevelTime, newData.levelTime);

        Debug.Log($"[LevelSave] Merging level {existingData.level} ({existingData.difficulty})({newData.difficulty}): existing bestTime={existingData.bestLevelTime}, new levelTime={newData.levelTime}, merged={mergedBestTime}");

        return new LevelData
        {
            level = existingData.level,
            difficulty = newData.difficulty, // Use the current run's difficulty!

            // Use new data if valid, otherwise keep existing
            levelTime = newData.levelTime > 0 ? newData.levelTime : existingData.levelTime,
            coinCount = newData.coinCount > 0 ? newData.coinCount : existingData.coinCount,
            bodyDecay = newData.bodyDecay > 0 ? newData.bodyDecay : existingData.bodyDecay,
            completed = newData.completed || existingData.completed,
            timeStamp = newData.timeStamp != default(DateTime) ? newData.timeStamp : existingData.timeStamp,
            totalDropCount = newData.totalDropCount > 0 ? newData.totalDropCount : existingData.totalDropCount,
            restartCount = newData.restartCount > 0 ? newData.restartCount : existingData.restartCount,
            gameVersionID = !string.IsNullOrEmpty(newData.gameVersionID) ? newData.gameVersionID : existingData.gameVersionID,

            // Track best times and scores
            bestLevelTime = mergedBestTime,
            highScore = Math.Max(existingData.highScore, newData.highScore),
            lowestBodyDecay = GetLowestBodyDecay(existingData.lowestBodyDecay, newData.bodyDecay),
            highestTotalCoinCount = Math.Max(existingData.highestTotalCoinCount, newData.coinCount),

            // Accumulate totals
            completedCount = existingData.completedCount + (newData.completed ? 1 : 0),
            totalLevelTime = existingData.totalLevelTime + newData.levelTime
        };
    }

    private float GetBestTime(float existingBest, float newTime)
    {
        if (existingBest <= 0) return newTime;
        if (newTime <= 0) return existingBest;
        return Math.Min(existingBest, newTime);
    }

    private float GetLowestBodyDecay(float existingLowest, float newDecay)
    {
        if (existingLowest <= 0) return newDecay;
        if (newDecay <= 0) return existingLowest;
        return Math.Min(existingLowest, newDecay);
    }
    #endregion

    #region Utility Methods (Kept for compatibility)
    public string MakeLevelID(int level, DIFFICULTY difficulty)
    {
        return GenerateLevelKey(level, difficulty);
    }
    #endregion

    #region Debug Methods (Testing Only)

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.M))
        // {
        //     DEBUG_ResetAllSyncFlags();
        // }
    }

    /// <summary>
    /// Resets sync flags for all levels and all difficulties.
    /// FOR TESTING ONLY!
    /// </summary>
    public void DEBUG_ResetAllSyncFlags()
    {
        Debug.Log("[DEBUG] Resetting all Steam sync flags...");

        for (int i = 0; i <= GameLogic.Instance.TOTAL_LEVELS_COUNT; i++)
        {
            foreach (DIFFICULTY diff in System.Enum.GetValues(typeof(DIFFICULTY)))
            {
                LevelData levelData = GetLevelData((LEVEL_ID)i, diff);
                if (levelData != null && levelData.didSyncWithSteam)
                {
                    levelData.didSyncWithSteam = false;
                    SaveLevelDataToDisk(levelData);
                    Debug.Log($"✓ Reset sync flag for Level {i} ({diff})");
                }
            }
        }

        Debug.Log("[DEBUG] Finished resetting all sync flags!");
    }

    /// <summary>
    /// Resets the best time for a specific level (local save only).
    /// FOR TESTING ONLY!
    /// </summary>
    public void DEBUG_ResetBestTimeForLevel(LEVEL_ID levelId)
    {
        // Reset for NORMAL difficulty
        LevelData levelData = GetLevelData(levelId, DIFFICULTY.NORMAL);

        if (levelData == null)
        {
            Debug.LogWarning($"No level data found for {levelId}");
            return;
        }

        // Reset best time and related stats
        levelData.bestLevelTime = 0f;

        // Save the modified data
        bool success = SaveLevelDataToDisk(levelData);

        if (success)
        {
            Debug.Log($"✓ Reset best time for {levelId}");
        }
        else
        {
            Debug.LogError($"✗ Failed to reset best time for {levelId}");
        }
    }

    /// <summary>
    /// Completely deletes all data for a level (local save only).
    /// FOR TESTING ONLY!
    /// </summary>
    public void DEBUG_DeleteLevelData(LEVEL_ID levelId, DIFFICULTY difficulty)
    {
        bool success = DeleteLevelData((int)levelId, difficulty);

        if (success)
        {
            Debug.Log($"✓ Deleted all data for {levelId} ({difficulty})");
        }
        else
        {
            Debug.LogError($"✗ Failed to delete data for {levelId}");
        }
    }
    #endregion
}
