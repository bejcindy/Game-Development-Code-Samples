using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using FMOD.Studio;
using FMODUnity;
using TMPro;

public class OptionsMenuController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Apply/Revert Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button revertButton;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Game Settings")]
    [SerializeField] private Toggle skipFailDiaToggle;

    [Header("Timer Settings")]
    [SerializeField] private Toggle timerOnToggle;
    [SerializeField] private Toggle timerTempOnToggle;
    [SerializeField] private Toggle timerOffToggle;

    [Header("Language Settings")]
    [SerializeField] private Toggle englishToggle;
    [SerializeField] private Toggle chineseToggle;
    [SerializeField] private Toggle jpToggle;

    [Header("Graphics Settings")]
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Toggle aspectRatio43Toggle;  // NEW: Aspect ratio toggle
    [SerializeField] private Toggle aspectRatio169Toggle; // NEW: Aspect ratio toggle
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Vector2Int[] resolutions;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString[] qualityLevelTexts;

    #endregion

    #region Private Fields

    // Audio buses
    private Bus masterBus, musicBus, sfxBus;

    // Settings cache structure
    private struct SettingsCache
    {
        // Audio settings
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;

        // Game settings
        public bool skipFailureDialogues;
        public int timerSettings;
        public string language;

        // Graphics settings
        public bool fullScreen;
        public bool vSync;
        public bool aspectRatio43;  // NEW: True for 4:3, false for 16:9
        public int selectedResolutionIndex;
        public int selectedQualityIndex;

        public bool hasChanges;
    }

    private SettingsCache cachedSettings;
    private SettingsCache originalSettings;

    // Initialization tracking
    private bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeAudioBuses();
        InitializeButtons();
        InitializeGraphicsSettings();
        // Load settings immediately in Awake so they're available even when inactive
        LoadCurrentSettings();

        // Apply the loaded settings immediately, even when inactive
        ApplyLoadedSettings();

        isInitialized = true;
    }

    /// <summary>
    /// Apply the currently loaded settings to the game systems without requiring UI interaction
    /// This ensures settings are applied even when the options panel is inactive
    /// </summary>
    private void ApplyLoadedSettings()
    {
        // Apply audio settings immediately
        if (masterBus.hasHandle()) masterBus.setVolume(cachedSettings.masterVolume);
        if (musicBus.hasHandle()) musicBus.setVolume(cachedSettings.musicVolume);
        if (sfxBus.hasHandle()) sfxBus.setVolume(cachedSettings.sfxVolume);

        // Update static values for immediate audio feedback
        PauseMenuCanvas.masterVolume = cachedSettings.masterVolume;
        PauseMenuCanvas.musicVolume = cachedSettings.musicVolume;
        PauseMenuCanvas.sfxVolume = cachedSettings.sfxVolume;

        // Apply graphics settings immediately
        ApplyGraphicsSettings();

        // Apply localization settings immediately
        ApplyLanguageSettings();

        Debug.Log("Loaded settings applied to game systems");
    }

    /// <summary>
    /// Apply language settings without UI rebuilding
    /// </summary>
    private void ApplyLanguageSettings()
    {
        if (!string.IsNullOrEmpty(cachedSettings.language))
        {
            SwitchLocale(cachedSettings.language);
        }
    }

    private void Start()
    {
        StartCoroutine(LoadLocalizedQualityOptions());
        // Only update UI if we haven't done it yet
        UpdateUIFromCache();
        UpdateApplyButtonState();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;

        // Refresh settings when the panel is activated, but don't reload unnecessarily
        if (isInitialized)
        {
            LoadCurrentSettings();
            UpdateUIFromCache();
        }
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    #endregion

    #region Initialization

    private void InitializeAudioBuses()
    {
        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music Bus");
        sfxBus = RuntimeManager.GetBus("bus:/SFX Bus");
    }

    private void InitializeButtons()
    {
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplyAllSettings);
            applyButton.gameObject.SetActive(false); // Start hidden since no changes initially
        }

        if (revertButton != null)
        {
            revertButton.onClick.AddListener(RevertToDefaults);
            // Revert button stays always visible
        }
    }

    private void InitializeGraphicsSettings()
    {
        // Initialize resolution dropdown
        PopulateResolutionDropdown();

        // Initialize quality dropdown - only start coroutine if active
        // if (gameObject.activeInHierarchy)
        // {
        //     StartCoroutine(LoadLocalizedQualityOptions());
        // }
        // StartCoroutine(LoadLocalizedQualityOptions());
    }

    #endregion

    #region Graphics Settings Methods

    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null || resolutions == null) return;

        resolutionDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (Vector2Int resolution in resolutions)
        {
            options.Add(new TMP_Dropdown.OptionData(resolution.x + " x " + resolution.y));
        }

        resolutionDropdown.AddOptions(options);
    }

    // NEW: Select the best resolution based on monitor's native resolution
    private void SelectBestResolution()
    {
        if (resolutions == null || resolutions.Length == 0) return;

        // Get the monitor's native resolution
        Resolution nativeResolution = Screen.currentResolution;
        int nativeWidth = nativeResolution.width;
        int nativeHeight = nativeResolution.height;


        int bestIndex = 0;
        int bestScore = int.MaxValue;

        for (int i = 0; i < resolutions.Length; i++)
        {
            int resWidth = resolutions[i].x;
            int resHeight = resolutions[i].y;

            // Check for exact match first (highest priority)
            if (resWidth == nativeWidth && resHeight == nativeHeight)
            {
                cachedSettings.selectedResolutionIndex = i;
                return;
            }

            // Calculate a score based on how close the resolution is to native
            // Prioritize resolution that's equal or smaller than native (to avoid upscaling)
            int widthDiff = Mathf.Abs(resWidth - nativeWidth);
            int heightDiff = Mathf.Abs(resHeight - nativeHeight);

            // Prefer resolutions that don't exceed native resolution
            int penalty = 0;
            if (resWidth > nativeWidth || resHeight > nativeHeight)
            {
                penalty = 100000; // Heavy penalty for exceeding native resolution
            }

            // Score calculation: prioritize height match, then width match
            int score = heightDiff * 1000 + widthDiff + penalty;

            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        cachedSettings.selectedResolutionIndex = bestIndex;
    }

    // Keep the existing method for when current screen resolution is needed
    private void SelectClosestResolution()
    {
        if (resolutions == null || resolutions.Length == 0) return;

        int screenW = Screen.width;
        int screenH = Screen.height;
        int bestIndex = 0;
        int bestScore = int.MaxValue;

        for (int i = 0; i < resolutions.Length; i++)
        {
            int dx = Mathf.Abs(resolutions[i].x - screenW);
            int dy = Mathf.Abs(resolutions[i].y - screenH);

            // Prefer exact match
            if (dx == 0 && dy == 0)
            {
                cachedSettings.selectedResolutionIndex = i;
                return;
            }

            // Score: prioritize height, then width
            int score = dy * 10000 + dx;
            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        cachedSettings.selectedResolutionIndex = bestIndex;
    }

    bool loadingQualityOptions;
    private IEnumerator LoadLocalizedQualityOptions()
    {
        if (loadingQualityOptions) yield break;

        loadingQualityOptions = true;

        if (qualityLevelTexts == null || qualityDropdown == null) yield break;

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (var localizedQuality in qualityLevelTexts)
        {
            var operation = localizedQuality.GetLocalizedStringAsync();
            yield return operation;

            if (operation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                options.Add(new TMP_Dropdown.OptionData(operation.Result));
            }
            else
            {
                Debug.LogError($"Failed to load localized quality text");
            }
        }

        // Store current selection
        int currentSelection = qualityDropdown.value;

        // Update options
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(options);

        // Restore selection if valid
        if (currentSelection < options.Count)
            qualityDropdown.value = currentSelection;

        loadingQualityOptions = false;
        yield break;
    }

    #endregion

    #region Settings Management

    /// <summary>
    /// Load current settings from GameLogic and Unity settings into cache
    /// </summary>
    public void LoadCurrentSettings()
    {
        // Audio settings
        cachedSettings.masterVolume = GameLogic.Instance.GetMasterVolume();
        cachedSettings.musicVolume = GameLogic.Instance.GetBgMusicVolume();
        cachedSettings.sfxVolume = GameLogic.Instance.GetSfxVolume();

        // Game settings
        cachedSettings.skipFailureDialogues = GameLogic.Instance.GetSkipFailureDialogues();
        cachedSettings.timerSettings = GameLogic.Instance.GetTimerSettings();
        cachedSettings.language = GameLogic.Instance.GetLanguage();

        // Graphics settings - load from saved settings, not current Unity state
        cachedSettings.fullScreen = GameLogic.Instance.GetFullScreen();
        cachedSettings.vSync = GameLogic.Instance.GetVSync();
        cachedSettings.aspectRatio43 = GameLogic.Instance.GetAspectRatio43();
        cachedSettings.selectedResolutionIndex = GameLogic.Instance.GetSelectedResolutionIndex();
        cachedSettings.selectedQualityIndex = GameLogic.Instance.GetSelectedQualityIndex();


        // Auto-detect best resolution if not set or invalid
        if (resolutions != null)
        {
            if (cachedSettings.selectedResolutionIndex < 0 || cachedSettings.selectedResolutionIndex >= resolutions.Length)
            {
                SelectBestResolution();

                // Save the auto-detected resolution immediately
                GameLogic.Instance.SetSelectedResolutionIndex(cachedSettings.selectedResolutionIndex);

                // Update the dropdown immediately if it exists and is populated
                if (resolutionDropdown != null && resolutionDropdown.options.Count > 0)
                {
                    resolutionDropdown.value = cachedSettings.selectedResolutionIndex;
                }
            }
        }

        cachedSettings.hasChanges = false;

        // Store original settings for comparison
        originalSettings = cachedSettings;
    }

    /// <summary>
    /// Update UI elements to reflect cached settings
    /// </summary>
    public void UpdateUIFromCache()
    {
        // Audio settings
        if (masterSlider != null) masterSlider.value = cachedSettings.masterVolume;
        if (musicSlider != null) musicSlider.value = cachedSettings.musicVolume;
        if (sfxSlider != null) sfxSlider.value = cachedSettings.sfxVolume;

        // Game settings
        if (skipFailDiaToggle != null) skipFailDiaToggle.isOn = cachedSettings.skipFailureDialogues;

        // Timer settings
        if (timerOnToggle != null) timerOnToggle.isOn = cachedSettings.timerSettings == 1;
        if (timerTempOnToggle != null) timerTempOnToggle.isOn = cachedSettings.timerSettings == 0;
        if (timerOffToggle != null) timerOffToggle.isOn = cachedSettings.timerSettings == -1;

        // Language toggles
        if (englishToggle != null) englishToggle.isOn = cachedSettings.language == "en";
        if (chineseToggle != null) chineseToggle.isOn = cachedSettings.language == "zh";
        if (jpToggle != null) jpToggle.isOn = cachedSettings.language == "ja";

        // Graphics settings
        if (fullScreenToggle != null) fullScreenToggle.isOn = cachedSettings.fullScreen;
        if (vSyncToggle != null) vSyncToggle.isOn = cachedSettings.vSync;

        // Aspect ratio toggles with debugging
        if (aspectRatio43Toggle != null)
        {
            aspectRatio43Toggle.isOn = cachedSettings.aspectRatio43;
        }
        if (aspectRatio169Toggle != null)
        {
            aspectRatio169Toggle.isOn = !cachedSettings.aspectRatio43;
        }

        if (qualityDropdown != null) qualityDropdown.value = cachedSettings.selectedQualityIndex;
        if (resolutionDropdown != null) resolutionDropdown.value = cachedSettings.selectedResolutionIndex;

        // Apply audio changes temporarily for preview
        if (masterBus.hasHandle()) masterBus.setVolume(cachedSettings.masterVolume);
        if (musicBus.hasHandle()) musicBus.setVolume(cachedSettings.musicVolume);
        if (sfxBus.hasHandle()) sfxBus.setVolume(cachedSettings.sfxVolume);

        // Update static values for immediate audio feedback
        PauseMenuCanvas.masterVolume = cachedSettings.masterVolume;
        PauseMenuCanvas.musicVolume = cachedSettings.musicVolume;
        PauseMenuCanvas.sfxVolume = cachedSettings.sfxVolume;
    }

    /// <summary>
    /// Check if settings have changed and update apply button state
    /// </summary>
    private void UpdateApplyButtonState()
    {
        bool hasChanges =
            cachedSettings.masterVolume != originalSettings.masterVolume ||
            cachedSettings.musicVolume != originalSettings.musicVolume ||
            cachedSettings.sfxVolume != originalSettings.sfxVolume ||
            cachedSettings.skipFailureDialogues != originalSettings.skipFailureDialogues ||
            cachedSettings.timerSettings != originalSettings.timerSettings ||
            cachedSettings.language != originalSettings.language ||
            cachedSettings.fullScreen != originalSettings.fullScreen ||
            cachedSettings.vSync != originalSettings.vSync ||
            cachedSettings.aspectRatio43 != originalSettings.aspectRatio43 || // NEW: Check aspect ratio changes
            cachedSettings.selectedResolutionIndex != originalSettings.selectedResolutionIndex ||
            cachedSettings.selectedQualityIndex != originalSettings.selectedQualityIndex;

        cachedSettings.hasChanges = hasChanges;

        if (applyButton != null)
        {
            // Hide/show the apply button instead of just disabling it
            applyButton.gameObject.SetActive(hasChanges);
        }
    }

    /// <summary>
    /// Apply all cached settings and save them
    /// </summary>
    public void ApplyAllSettings()
    {
        if (!cachedSettings.hasChanges) return;
        // Apply audio settings
        GameLogic.Instance.SetMasterVolume(cachedSettings.masterVolume);
        GameLogic.Instance.SetBgMusicVolume(cachedSettings.musicVolume);
        GameLogic.Instance.SetSfxVolume(cachedSettings.sfxVolume);

        // Apply game settings
        GameLogic.Instance.SetSkipFailureDialogues(cachedSettings.skipFailureDialogues);
        GameLogic.Instance.SetTimerSettings(cachedSettings.timerSettings);
        GameLogic.Instance.SetLanguage(cachedSettings.language);

        // Apply graphics settings to both GameLogic (for saving) and Unity (for immediate effect)
        GameLogic.Instance.SetFullScreen(cachedSettings.fullScreen);
        GameLogic.Instance.SetVSync(cachedSettings.vSync);
        GameLogic.Instance.SetAspectRatio43(cachedSettings.aspectRatio43); // Save aspect ratio setting
        GameLogic.Instance.SetSelectedResolutionIndex(cachedSettings.selectedResolutionIndex);
        GameLogic.Instance.SetSelectedQualityIndex(cachedSettings.selectedQualityIndex);

        // Apply graphics settings immediately
        ApplyGraphicsSettings();

        // Update original settings to match current
        originalSettings = cachedSettings;
        cachedSettings.hasChanges = false;

        // Update apply button state
        UpdateApplyButtonState();

        // Play audio feedback
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIButtonPress);
    }

    private void ApplyGraphicsSettings()
    {
        // Apply VSync setting
        QualitySettings.vSyncCount = cachedSettings.vSync ? 1 : 0;

        // Apply resolution setting with aspect ratio consideration
        if (cachedSettings.selectedResolutionIndex >= 0 &&
            cachedSettings.selectedResolutionIndex < resolutions.Length)
        {
            int width = resolutions[cachedSettings.selectedResolutionIndex].x;
            int height = resolutions[cachedSettings.selectedResolutionIndex].y;

            // NEW: Apply aspect ratio adjustment based on setting
            if (cachedSettings.aspectRatio43)
            {
                // Force 4:3 aspect ratio (original behavior)
                if (width >= height * 4 / 3)
                {
                    Screen.SetResolution(height * 4 / 3, height, cachedSettings.fullScreen);
                }
                else
                {
                    Screen.SetResolution(width, width * 3 / 4, cachedSettings.fullScreen);
                }
            }
            else
            {
                // Use 16:9 aspect ratio
                if (width >= height * 16 / 9)
                {
                    Screen.SetResolution(height * 16 / 9, height, cachedSettings.fullScreen);
                }
                else
                {
                    Screen.SetResolution(width, width * 9 / 16, cachedSettings.fullScreen);
                }
            }
        }

        // Apply quality setting
        QualitySettings.SetQualityLevel(cachedSettings.selectedQualityIndex, false);
    }

    /// <summary>
    /// Revert all settings to default values
    /// </summary>
    public void RevertToDefaults()
    {
        // Set default audio values
        cachedSettings.masterVolume = 0.5f;
        cachedSettings.musicVolume = 0.5f;
        cachedSettings.sfxVolume = 0.5f;

        // Set default game settings
        cachedSettings.skipFailureDialogues = false;
        cachedSettings.timerSettings = 1; // Default to timer on
        cachedSettings.language = "en";

        // Set default graphics settings
        cachedSettings.fullScreen = true;
        cachedSettings.vSync = true;
        cachedSettings.aspectRatio43 = true; // Default to 4:3 aspect ratio
        cachedSettings.selectedQualityIndex = 2; // Assuming index 2 is high quality

        // NEW: Use best resolution detection instead of closest
        SelectBestResolution();

        cachedSettings.hasChanges = true;

        // Update UI to reflect defaults
        UpdateUIFromCache();

        // Update apply button state
        UpdateApplyButtonState();

        // Play audio feedback
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIButtonPress);

        Debug.Log("Settings reverted to defaults");
    }

    /// <summary>
    /// Discard changes and restore original settings
    /// </summary>
    public void DiscardChanges()
    {
        cachedSettings = originalSettings;
        UpdateUIFromCache();
        UpdateApplyButtonState();
    }

    /// <summary>
    /// Check if there are unsaved changes
    /// </summary>
    public bool HasUnsavedChanges()
    {
        return cachedSettings.hasChanges;
    }

    #endregion

    #region Dropdown Helpers
    /// <summary>
    /// Checks if any dropdown in the options menu is currently open
    /// </summary>
    /// <returns>True if any dropdown is open, false otherwise</returns>
    public bool IsAnyDropdownOpen()
    {
        bool resolutionOpen = resolutionDropdown != null && resolutionDropdown.IsExpanded;
        bool qualityOpen = qualityDropdown != null && qualityDropdown.IsExpanded;

        return resolutionOpen || qualityOpen;
    }

    /// <summary>
    /// Closes any open dropdowns in the options menu
    /// </summary>
    public void CloseAllDropdowns()
    {
        if (resolutionDropdown != null && resolutionDropdown.IsExpanded)
        {
            resolutionDropdown.Hide();
        }

        if (qualityDropdown != null && qualityDropdown.IsExpanded)
        {
            qualityDropdown.Hide();
        }
    }
    #endregion

    #region UI Event Handlers

    // Audio Events
    public void OnMasterVolumeChanged(float value)
    {
        cachedSettings.masterVolume = value;
        PauseMenuCanvas.masterVolume = value;
        if (masterBus.hasHandle()) masterBus.setVolume(value);
        UpdateApplyButtonState();
    }

    public void OnMusicVolumeChanged(float value)
    {
        cachedSettings.musicVolume = value;
        PauseMenuCanvas.musicVolume = value;
        if (musicBus.hasHandle()) musicBus.setVolume(value);
        UpdateApplyButtonState();
    }

    public void OnSFXVolumeChanged(float value)
    {
        cachedSettings.sfxVolume = value;
        PauseMenuCanvas.sfxVolume = value;
        if (sfxBus.hasHandle()) sfxBus.setVolume(value);
        UpdateApplyButtonState();
    }

    // Game Settings Events
    public void OnSkipFailureDialogueChanged(bool value)
    {
        cachedSettings.skipFailureDialogues = value;
        UpdateApplyButtonState();
    }

    public void OnTimerSettingSelected(int i)
    {
        cachedSettings.timerSettings = i;
        UpdateApplyButtonState();
    }

    public void OnEnglishSelected(bool value)
    {
        if (value)
        {
            cachedSettings.language = "en";
            // Only start coroutine if the GameObject is active
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(SwitchLocaleAndRebuild("en"));
            }
            else
            {
                // Just switch locale without rebuilding if inactive
                SwitchLocale("en");
            }
            UpdateApplyButtonState();
        }
    }

    public void OnChineseSelected(bool value)
    {
        if (value)
        {
            cachedSettings.language = "zh";
            // Only start coroutine if the GameObject is active
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(SwitchLocaleAndRebuild("zh"));
            }
            else
            {
                // Just switch locale without rebuilding if inactive
                SwitchLocale("zh");
            }
            UpdateApplyButtonState();
        }
    }

    public void OnJapaneseSelected(bool value)
    {
        if (value)
        {
            cachedSettings.language = "ja";
            // Only start coroutine if the GameObject is active
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(SwitchLocaleAndRebuild("ja"));
            }
            else
            {
                // Just switch locale without rebuilding if inactive
                SwitchLocale("ja");
            }
            UpdateApplyButtonState();
        }
    }

    // Graphics Settings Events
    public void OnFullScreenChanged(bool value)
    {
        cachedSettings.fullScreen = value;
        UpdateApplyButtonState();
    }

    public void OnVSyncChanged(bool value)
    {
        cachedSettings.vSync = value;
        UpdateApplyButtonState();
    }

    // NEW: Aspect ratio event handlers
    public void OnAspectRatio43Selected(bool value)
    {
        if (value)
        {
            cachedSettings.aspectRatio43 = true;
            UpdateApplyButtonState();
        }
    }

    public void OnAspectRatio169Selected(bool value)
    {
        if (value)
        {
            cachedSettings.aspectRatio43 = false;
            UpdateApplyButtonState();
        }
    }

    public void OnResolutionChanged(int index)
    {
        cachedSettings.selectedResolutionIndex = index;
        UpdateApplyButtonState();
    }

    public void OnQualityChanged(int index)
    {
        cachedSettings.selectedQualityIndex = index;
        UpdateApplyButtonState();
    }

    #endregion

    #region Localization

    private void OnLanguageChanged(Locale newLocale)
    {
        // Only start coroutines if the GameObject is active
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(LoadLocalizedQualityOptions());
            // No longer need to rebuild layouts
        }
    }

    /// <summary>
    /// Switches locale and properly rebuilds layout after localization is complete
    /// </summary>
    /// <param name="localeCode">Language code to switch to</param>
    /// <returns></returns>
    private IEnumerator SwitchLocaleAndRebuild(string localeCode)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        Locale targetLocale = null;

        foreach (var locale in locales)
        {
            if (locale.Identifier.Code == localeCode)
            {
                targetLocale = locale;
                break;
            }
        }

        if (targetLocale == null)
        {
            Debug.LogWarning("Locale code not found: " + localeCode);
            yield break;
        }

        // Switch the locale
        LocalizationSettings.SelectedLocale = targetLocale;

        // Wait for localization to complete
        var operation = LocalizationSettings.SelectedLocaleAsync;
        yield return operation;

        // Wait an additional frame to ensure all localized text components have updated
        yield return null;

        // Reload localized quality options if needed (only if still active)
        if (gameObject.activeInHierarchy && qualityLevelTexts != null && qualityLevelTexts.Length > 0)
        {
            yield return StartCoroutine(LoadLocalizedQualityOptions());
        }

        // Wait another frame after quality options are loaded
        yield return null;

    }

    public void SwitchLocale(string localeCode)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        foreach (var locale in locales)
        {
            if (locale.Identifier.Code == localeCode)
            {
                LocalizationSettings.SelectedLocale = locale;
                return;
            }
        }
        Debug.LogWarning("Locale code not found: " + localeCode);
    }

    #endregion

    #region Audio Feedback

    public void PlayHoverSound()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIButtonHover);
    }

    public void PlaySelectSound()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIButtonPress);
    }
}

#endregion