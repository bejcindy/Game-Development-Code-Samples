using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;



[ES3Serializable]
public class LevelData
{
    public int level;
    public LEVEL_ID LevelID;
    public DIFFICULTY difficulty;
    public ulong friendID;
    public bool completed;
    public int highScore;
    public int totalDropCount;
    public int restartCount;
    public float levelTime; // previous level time
    public float totalLevelTime;
    public float bestLevelTime;
    public int coinCount;
    public int highestTotalCoinCount;
    public float bodyDecay;
    public float lowestBodyDecay;
    public DateTime timeStamp; // last time the level was completed
    public String gameVersionID;
    public int completedCount;
    public bool didSyncWithSteam; // Track if this level's best time has been synced with Steam
}




[ES3Serializable]
public class GameSettings
{
    public GamePlatform platform;
    public RELEASE_TYPE releaseType;
    public bool soundFX;
    public float soundFXVolume;
    public bool bgMusic;
    public float bgMusicVolume;
    public bool mute;
    public float volume;

    public Dictionary<string, bool> tutorialProgress;
    public bool tutorialComplete;
    public int cutsceneProgress;
    public Dictionary<string, int> dialogueIndexes;
    public int furthestLevelID;
    public int lastPlayedLevelID = 0; // New field for tracking last played level
    public int totalGamesPlayed;
    public bool skipFailureDialogues;
    public Dictionary<string, bool> wikiProgress;
    // Player Analytics
    public DateTime lastPlayed;
    public float totalTimePlayed;

    // Player IDs
    public string playerID;

    //Timer Settings
    public int timerSetting = 0; // -1: off, 0: on during respawn, 1: always on

    //Localization
    public string language = "en"; // default to English

    // Graphics settings
    public bool fullScreen = true;
    public bool vSync = true;
    public int selectedResolutionIndex = 0;
    public int selectedQualityIndex = 2; // Default to medium/high quality
    public bool aspectRatio43 = true; // Default to 4:3 aspect ratio
}


public static class GameSaveIDs
{
    public const string GAME_SETTINGS_ID = "GAME_SETTINGS";
    public const string LEVEL_DICTIONARY_ID = "LEVEL_DICTIONARY";
}


public class GameSave : Singleton<GameSave>
{
    private bool m_bInitialized = false;
    // public static GameSave m_GameSave = null;
    private ES3Settings es3_settings;
    private string SECRET_KEY_TEST_donotship = "TheUndertaker-04-28-2025";
    private GameSave() { }

    void Awake()
    {
        // // Only one instance of gamesave at a time!
        // if (m_GameSave != null)
        // {
        //     Destroy(gameObject);
        //     return;
        // }
        //
        // m_GameSave = this;
        //
        // // Persist across scenes
        // DontDestroyOnLoad(gameObject);


        try
        {
            m_bInitialized = InitGameSaves();
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError("Error loading or creating game save.\n" + e, this);
            Application.Quit();
            return;
        }

        if (!m_bInitialized)
        {
            Debug.LogWarning("Game save failed", this);
            Application.Quit();
            return;
        }
    }

    public bool InitGameSaves()
    {
        InitializeEncryptionSettings();

        GameSettings _gs = InitializeGameSettings();

        GameLogic.Instance.NotifyGameSaveLoadedLoaded();
        return true;
    }

    private void InitializeEncryptionSettings()
    {
        es3_settings = new ES3Settings(true);
        if (!HilltopConstants.DEBUG_GAMESAVE)
        {
            es3_settings.encryptionType = ES3.EncryptionType.AES;
            es3_settings.encryptionPassword = SECRET_KEY_TEST_donotship;
        }
    }


    private GameSettings InitializeGameSettings()
    {
        GameSettings _gs = GetGameSettingsDisk();
        if (_gs == null)
        {
            _gs = new GameSettings();
            Debug.Log("Failed to load Game Settings Data, creating new default Game Settings");
            SetDefaultGameSettings(_gs);
            ConfigureReleaseType(_gs);
            SaveNewGameSettings(_gs);
        }
        else
        {
            if (HilltopConstants.DEBUG)
                Debug.Log("Loaded Game Settings Data");
        }
        return _gs;
    }

    private void ConfigureReleaseType(GameSettings _gs)
    {
        if (HilltopConstants.DEBUG || HilltopConstants.DEBUG_MOVEMENT ||
            HilltopConstants.DEBUG_OBSTACLES || HilltopConstants.DEBUG_STEAM_API)
        {
            Debug.LogWarning("DEBUG set to true, running in demo mode by default");
            _gs.releaseType = RELEASE_TYPE.PRODUCTION;
        }

        if (HilltopConstants.STEAM_DEMO)
        {
            _gs.releaseType = RELEASE_TYPE.DEMO_STEAM;
        }
    }

    private void SaveNewGameSettings(GameSettings _gs)
    {
        if (SaveGameData(_gs))
        {
            Debug.Log("New Game Data Saved");
        }
        else
        {
            Debug.Log("New Game Data Not Saved");
        }
    }

    public void InitESSettings(ES3Settings settings)
    {
        es3_settings = settings;
    }


    public GameSettings SetDefaultGameSettings(GameSettings _gs)
    {
        _gs.platform = GamePlatform.PC;
        _gs.releaseType = RELEASE_TYPE.PRODUCTION;
        _gs.soundFX = true;
        _gs.bgMusic = true;
        _gs.mute = false;
        _gs.volume = 1f;
        _gs.soundFXVolume = 1f;
        _gs.bgMusicVolume = 1f;
        _gs.lastPlayed = DateTime.Now;
        _gs.totalTimePlayed = 0f;
        _gs.tutorialProgress = new Dictionary<string, bool>();
        _gs.tutorialComplete = false;
        _gs.cutsceneProgress = 0;
        _gs.playerID = SystemInfo.deviceUniqueIdentifier; //TODO: check if we can use this
        _gs.timerSetting = 1; // Default to on during respawn
        _gs.language = "en"; // Default to English
        _gs.lastPlayedLevelID = 0; // Initialize last played level to 0

        // Graphics settings defaults
        _gs.fullScreen = true;
        _gs.vSync = true;
        _gs.aspectRatio43 = true; // Default to 4:3 aspect ratio
        _gs.selectedResolutionIndex = -1; // NEW: Use -1 to indicate "not set" so we can auto-detect
        _gs.selectedQualityIndex = 2; // Default to medium/high quality

        return _gs;
    }

    public bool SaveGameData(GameSettings _gS)
    {
        try
        {
            // ES3.Save(GameSaveIDs.LEVEL_DICTIONARY_ID, levelStatusDictionary);
            ES3.Save(GameSaveIDs.GAME_SETTINGS_ID, _gS, es3_settings);
        }
        catch (System.IO.IOException)
        {
            Debug.Log("The file is open elsewhere or there was not enough storage space");
            return false;
        }
        catch (System.Security.SecurityException)
        {
            Debug.Log("You do not have the required permissions");
            return false;
        }
        catch (System.NullReferenceException)
        {
            Debug.Log("You are trying to save a game settings that doesn't exist!");
            return false;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return false;
        }

        return true;
    }

    public GameSettings GetGameSettingsDisk()
    {
        if (ES3.KeyExists(GameSaveIDs.GAME_SETTINGS_ID, es3_settings))
        {
            GameSettings gS;
            try
            {
                gS = ES3.Load<GameSettings>(GameSaveIDs.GAME_SETTINGS_ID, es3_settings);
                return gS;
            }
            catch (System.IO.IOException)
            {
                Debug.LogWarning("The file is open elsewhere or there was not enough storage space");
                return null;
            }
            catch (System.Security.SecurityException)
            {
                Debug.LogWarning("You do not have the required permissions");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                return null;
            }
        }
        else
        {
            return null;
        }
    }


    public GameSettings GetGameSettings()
    {
        if (GameLogic.Instance.GameState.Equals(GameState.Loading))
        {
            Debug.LogWarning("Request make while game is loading");
            return null;
        }

        else
        {
            return GetGameSettingsDisk();
        }
    }

    public void SetFurthestLevelID(LEVEL_ID id)
    {
        GameSettings _gs = GetGameSettings();
        _gs.furthestLevelID = (int)id;
        SaveGameData(_gs);
    }

    public LEVEL_ID GetFurthestLevelID()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, defaulting furthest level to 0");
            return LEVEL_ID.LEVEL_0;
        }
        return (LEVEL_ID)_gs.furthestLevelID;
    }

    /// <summary>
    /// Sets the last played level ID
    /// </summary>
    /// <param name="levelId">The level ID that was last played</param>
    public void SetLastPlayedLevelID(int levelId)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.lastPlayedLevelID = levelId;
        SaveGameData(_gs);
        Debug.Log($"Last played level set to: {levelId}");
    }

    /// <summary>
    /// Gets the last played level ID
    /// </summary>
    /// <returns>The last played level ID, defaults to 0 if not set</returns>
    public int GetLastPlayedLevelID()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, defaulting last played level to 0");
            return 0;
        }
        return _gs.lastPlayedLevelID;
    }

    public void SetMasterVolume(float volume)
    {
        GameSettings _gs = GetGameSettings();
        if (volume < 0 || volume > 1)
        {
            Debug.LogError("Volume must be between 0 and 1");
        }
        else
        {
            _gs.volume = volume;
        }
        SaveGameData(_gs);
    }
    public float GetMasterVolume()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return 0.5f;
        else
            return _gs.volume;
    }
    public void SetBgMusicVolume(float v)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.bgMusicVolume = v;
        SaveGameData(_gs);
    }
    public float GetBgMusicVolume()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return 0.5f;
        else
            return _gs.bgMusicVolume;
    }
    public void SetBgMusic(bool b)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.bgMusic = b;
        SaveGameData(_gs);
    }

    public bool BgMusicEnabled()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return false;
        return _gs.bgMusic;
    }

    public void SetSfxVolume(float v)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.soundFXVolume = v;
        SaveGameData(_gs);
    }
    public float GetSfxVolume()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return 0.5f;
        else
            return _gs.soundFXVolume;
    }
    public void SetSoundFx(bool b)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.soundFX = b;
        SaveGameData(_gs);
    }
    public bool SoundFxEnabled()
    {
        return GetGameSettings().soundFX;
    }
    public void SetMute(bool b)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.mute = b;
        SaveGameData(_gs);
    }

    public bool IsMute()
    {
        return GetGameSettings().mute;
    }

    public void SetTutorialProgress(Dictionary<string, bool> progress)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;

        if (_gs.tutorialProgress == null)
            _gs.tutorialProgress = new Dictionary<string, bool>();

        // Remove keys not in the new progress
        var keysToRemove = new List<string>();
        foreach (var key in _gs.tutorialProgress.Keys)
        {
            if (!progress.ContainsKey(key))
                keysToRemove.Add(key);
        }
        foreach (var key in keysToRemove)
        {
            _gs.tutorialProgress.Remove(key);
        }

        // Update and add keys from the new progress
        foreach (var kvp in progress)
        {
            _gs.tutorialProgress[kvp.Key] = kvp.Value;
        }

        SaveGameData(_gs);
    }

    public Dictionary<string, bool> GetTutorialProgress()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            return new Dictionary<string, bool>();
        }
        else
        {
            if (_gs.tutorialProgress == null)
                _gs.tutorialProgress = new Dictionary<string, bool>();
            return _gs.tutorialProgress;
        }
    }

    public void SetWikiProgress(Dictionary<string, bool> progress)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        if (_gs.wikiProgress == null)
            _gs.wikiProgress = new Dictionary<string, bool>();
        // Remove keys not in the new progress
        var keysToRemove = new List<string>();
        foreach (var key in _gs.wikiProgress.Keys)
        {
            if (!progress.ContainsKey(key))
                keysToRemove.Add(key);
        }
        foreach (var key in keysToRemove)
        {
            _gs.wikiProgress.Remove(key);
        }
        // Update and add keys from the new progress
        foreach (var kvp in progress)
        {
            _gs.wikiProgress[kvp.Key] = kvp.Value;
        }
        SaveGameData(_gs);
    }

    public Dictionary<string, bool> GetWikiProgress()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            return new Dictionary<string, bool>();
        }
        else
        {
            if (_gs.wikiProgress == null)
                _gs.wikiProgress = new Dictionary<string, bool>();
            return _gs.wikiProgress;
        }
    }

    public void SetTutorialComplete(bool b)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.tutorialComplete = b;
        SaveGameData(_gs);
    }

    public void SetCutsceneProgress(int progress)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.cutsceneProgress = progress;
        SaveGameData(_gs);
    }


    public void SetSkipFailureDialogues(bool b)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.skipFailureDialogues = b;
        SaveGameData(_gs);
    }

    public bool GetSkipFailureDialogues()
    {
        return GetGameSettings().skipFailureDialogues;
    }

    public void SetTimerSetting(int setting)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.timerSetting = setting;
        SaveGameData(_gs);
    }

    public int GetTimerSetting()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, default to timer on during respawn");
            return 1; // Default to on during respawn
        }
        else
        {
            return _gs.timerSetting;
        }
    }

    public void SetLanguage(string language)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.language = language;
        SaveGameData(_gs);
    }

    public string GetLanguage()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            string systemLanguageCode = GetSystemLanguageCode();
            Debug.Log($"System Language detected: {systemLanguageCode}");

            return systemLanguageCode;
        }
        else
        {
            if (string.IsNullOrEmpty(_gs.language))
            {
                return GetSystemLanguageCode();
            }
            return _gs.language;
        }
    }
    private string GetSystemLanguageCode()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional:
                return "zh";

            case SystemLanguage.Japanese:
                return "ja";

            case SystemLanguage.English:
                return "en";

            default:
                // Fallback to English if system language is not supported
                return "en";
        }
    }

    public void SetDialogueIndexes(Dictionary<string, int> indexes)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        if (_gs.dialogueIndexes == null)
            _gs.dialogueIndexes = new Dictionary<string, int>();
        // Remove keys not in the new indexes
        var keysToRemove = new List<string>();
        foreach (var key in _gs.dialogueIndexes.Keys)
        {
            if (!indexes.ContainsKey(key))
                keysToRemove.Add(key);
        }
        foreach (var key in keysToRemove)
        {
            _gs.dialogueIndexes.Remove(key);
        }
        // Update and add keys from the new indexes
        foreach (var kvp in indexes)
        {
            _gs.dialogueIndexes[kvp.Key] = kvp.Value;
        }
        SaveGameData(_gs);
    }

    public Dictionary<string, int> GetDialogueIndexes()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            return new Dictionary<string, int>();
        }
        else
        {
            if (_gs.dialogueIndexes == null)
                _gs.dialogueIndexes = new Dictionary<string, int>();
            return _gs.dialogueIndexes;
        }
    }

    // Graphics Settings Methods

    /// <summary>
    /// Sets the fullscreen mode setting
    /// </summary>
    /// <param name="fullScreen">True for fullscreen, false for windowed</param>
    public void SetFullScreen(bool fullScreen)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.fullScreen = fullScreen;
        SaveGameData(_gs);
    }

    /// <summary>
    /// Gets the fullscreen mode setting
    /// </summary>
    /// <returns>True if fullscreen, false if windowed. Default is true.</returns>
    public bool GetFullScreen()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, default to fullscreen");
            return true;
        }
        else
        {
            return _gs.fullScreen;
        }
    }

    /// <summary>
    /// Sets the VSync setting
    /// </summary>
    /// <param name="vSync">True to enable VSync, false to disable</param>
    public void SetVSync(bool vSync)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.vSync = vSync;
        SaveGameData(_gs);
    }

    /// <summary>
    /// Gets the VSync setting
    /// </summary>
    /// <returns>True if VSync is enabled, false if disabled. Default is true.</returns>
    public bool GetVSync()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, default to VSync enabled");
            return true;
        }
        else
        {
            return _gs.vSync;
        }
    }

    /// <summary>
    /// Sets the selected resolution index
    /// </summary>
    /// <param name="resolutionIndex">Index of the resolution in the resolutions array</param>
    public void SetSelectedResolutionIndex(int resolutionIndex)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;

        if (resolutionIndex < 0)
        {
            Debug.LogWarning("Resolution index must be >= 0, setting to 0");
            resolutionIndex = 0;
        }

        _gs.selectedResolutionIndex = resolutionIndex;
        SaveGameData(_gs);
    }

    /// <summary>
    /// Gets the selected resolution index
    /// </summary>
    /// <returns>Index of the selected resolution. Default is 0.</returns>
    public int GetSelectedResolutionIndex()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, default to resolution index 0");
            return 0;
        }
        else
        {
            return _gs.selectedResolutionIndex;
        }
    }

    /// <summary>
    /// Sets the selected quality level index
    /// </summary>
    /// <param name="qualityIndex">Index of the quality level</param>
    public void SetSelectedQualityIndex(int qualityIndex)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;

        if (qualityIndex < 0)
        {
            Debug.LogWarning("Quality index must be >= 0, setting to 0");
            qualityIndex = 0;
        }

        _gs.selectedQualityIndex = qualityIndex;
        SaveGameData(_gs);
    }

    /// <summary>
    /// Gets the selected quality level index
    /// </summary>
    /// <returns>Index of the selected quality level. Default is 2 (medium/high).</returns>
    public int GetSelectedQualityIndex()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            //auto detect quality level based on Unity's logic
            int autoDetectedQuality = QualitySettings.GetQualityLevel();
            Debug.Log($"Unity auto-detected quality level: {autoDetectedQuality} ({QualitySettings.names[autoDetectedQuality]})");

            return autoDetectedQuality;
        }
        else
        {
            return _gs.selectedQualityIndex;
        }
    }

    /// <summary>
    /// Sets the aspect ratio setting (true for 4:3, false for 16:9)
    /// </summary>
    /// <param name="aspectRatio43">True for 4:3, false for 16:9</param>
    public void SetAspectRatio43(bool aspectRatio43)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.aspectRatio43 = aspectRatio43;
        SaveGameData(_gs);
    }

    /// <summary>
    /// Gets the aspect ratio setting (true for 4:3, false for 16:9)
    /// </summary>
    /// <returns>True if 4:3, false if 16:9. Default is true (4:3).</returns>
    public bool GetAspectRatio43()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, default to 4:3 aspect ratio");
            Resolution nativeResolution = Screen.currentResolution;
            int nativeWidth = nativeResolution.width;
            int nativeHeight = nativeResolution.height;
            if (nativeWidth <= 1280 || nativeHeight <= 800)
            {
                return false; // 16:9 for Steam Deck and smaller resolutions
            }
            else
            {
                return true; // 4:3 for larger resolutions
            }
        }
        else
        {
            return _gs.aspectRatio43;
        }
    }

    public void UpdateTotalGameTime(float timeToAdd)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.totalTimePlayed = _gs.totalTimePlayed + timeToAdd;
        SaveGameData(_gs);
    }

    public float GetTotalGameTime()
    {
        return GetGameSettings().totalTimePlayed;
    }

    public void SetPlatform(GamePlatform platform)
    {
        GameSettings _gs = GetGameSettings();
        _gs.platform = platform;
        SaveGameData(_gs);
    }
    public GamePlatform GetPlatform()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, default to PC");
            return GamePlatform.PC;
        }
        else
        {
            return _gs.platform;
        }
    }


    public RELEASE_TYPE GetReleaseType()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            if (DemoManager.Instance.isLocalDemo)
            {
                return RELEASE_TYPE.PRODUCTION_LOCAL;
            }
            return RELEASE_TYPE.PRODUCTION;
        }
        else
        {
            return _gs.releaseType;
        }
    }

    public void SetReleaseType(RELEASE_TYPE releaseType)
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
            return;
        _gs.releaseType = releaseType;
        SaveGameData(_gs);
    }

    public void IncrementTotalGamesPlayed()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, default to false");
        }
        else
        {
            _gs.totalGamesPlayed++;
        }
    }

    public int GetTotalGamesPlayed()
    {
        GameSettings _gs = GetGameSettings();
        if (_gs == null)
        {
            Debug.LogWarning("Error loading game settings, default to false");
            return -1;
        }
        else
        {
            return _gs.totalGamesPlayed;
        }
    }


}