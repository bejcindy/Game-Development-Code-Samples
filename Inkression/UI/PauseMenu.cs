using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PixelCrushers.DialogueSystem;
using UnityEngine.UI;
using FMODUnity;
using DG.Tweening;
using Beautify.Universal;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused;
    public static bool saveComplete;
    public GameObject pauseMenu;
    public GameObject OptionsMenu;
    public GameObject QuitMenu;
    public GameObject SaveConfirmationWindow;
    public GameObject pauseMenuEndPanel;
    public Image fadeOutScreen;
    Animator pauseMenuAnim;
    public GameObject player;
    PlayerHolding playerHolding;
    PlayerMovement playerMovement;

    readonly string pauseMenuSFX = "event:/Sound Effects/UI/PauseMenuOn";
    string buttonHoverSFX = "event:/Sound Effects/UI/PauseMenuHover";
    string songBus = "bus:/Ambience/Songs";

    [SerializeField] GameObject languageDropDown;

    // Start is called before the first frame update
    void Start()
    {
        player = ReferenceTool.player.gameObject;
        playerHolding = ReferenceTool.playerHolding;
        playerMovement = ReferenceTool.playerMovement;
        ReferenceTool.pauseMenu = this;
        isPaused = false;
        if (SceneManager.GetActiveScene().name != "Prologue")
            Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.transform.SetAsLastSibling();
        pauseMenuAnim = pauseMenu.GetComponent<Animator>();
        
        OptionsMenu.SetActive(false);
        languageDropDown.SetActive(false);

        if (StartMenu.playedThroughOnce)
            SetDemoEndPanelOn();
    }

    // Update is called once per frame
    void Update()
    {
        //foreach (StandardUISubtitlePanel panel in DialogueManager.standardDialogueUI.conversationUIElements.subtitlePanels)
        //{
        //    if (panel.continueButton != null) panel.continueButton.interactable = !isPaused;
        //}
        Cursor.visible = false;
        if (saveComplete)
        {
            SaveConfirmationWindow.SetActive(true);
            saveComplete = false;
        }
        if (Input.GetKeyDown(KeyCode.Escape) && !MindPalace.tatMenuOn && !InspectionCanvasController.inspectCanvasOn && !playerHolding.inDialogue && !BookControler.bookOn && !DemoEndPanelController.endPanelOn)
        {
            if (OptionsMenu.activeSelf)
            {
                OptionsMenu.SetActive(false);
                //pauseMenu.SetActive(true);
                return;
            }

            if (QuitMenu.activeSelf)
            {
                QuitMenu.SetActive(false);
                //pauseMenu.SetActive(true);
                return;
            }

            if (SaveConfirmationWindow.activeSelf)
            {
                SaveConfirmationWindow.SetActive(false);
                //pauseMenu.SetActive(true);
                return;
            }

            if (languageDropDown.activeSelf)
            {
                languageDropDown.SetActive(false);
                return;
            }

            isPaused = !isPaused;
            foreach (StandardUISubtitlePanel panel in DialogueManager.standardDialogueUI.conversationUIElements.subtitlePanels)
            {
                if (panel.continueButton != null) panel.continueButton.interactable = !isPaused;
            }
            if (isPaused)
            {
                RuntimeManager.PlayOneShot("event:/Sound Effects/UI/PauseMenuOn", Camera.main.transform.position);
                PixelCrushers.UIPanel.monitorSelection = false; // Don't allow dialogue UI to steal back input focus.
                PixelCrushers.UIButtonKeyTrigger.monitorInput = false; // Disable hotkeys.
                PixelCrushers.DialogueSystem.DialogueManager.Pause(); // Stop DS timers (e.g., sequencer commands).
                Time.timeScale = 0.0f;
                Cursor.lockState = CursorLockMode.None;
                //Cursor.visible = true;
                playerMovement.StopWalkingSFX();
                pauseMenu.SetActive(true);
                OptionsMenu.SetActive(false);
                RuntimeManager.GetBus(songBus).setPaused(true);
            }
            else
            {
                Time.timeScale = 1.0f;
                RuntimeManager.GetBus(songBus).setPaused(false);
                pauseMenuAnim.SetTrigger("Off");
                PixelCrushers.UIPanel.monitorSelection = true; // Allow dialogue UI to steal back input focus again.
                PixelCrushers.UIButtonKeyTrigger.monitorInput = true; // Re-enable hotkeys.
                PixelCrushers.DialogueSystem.DialogueManager.Unpause(); // Resume DS timers (e.g., sequencer commands).

                if (!playerHolding.positionFixedWithMouse && !MindPalace.tatMenuOn && !InspectionCanvasController.inspectCanvasOn && !BookControler.bookOn && !DemoEndPanelController.endPanelOn)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    //Cursor.visible = false;
                }
                if (pauseMenuAnim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && pauseMenuAnim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "PauseMenuOff")
                {
                    pauseMenu.SetActive(false);
                }
            }
        }


        if (!isPaused && pauseMenuAnim.isActiveAndEnabled)
        {
            if (pauseMenuAnim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
            {
                pauseMenu.SetActive(false);
            }
            if (!playerHolding.positionFixedWithMouse && !MindPalace.tatMenuOn && !InspectionCanvasController.inspectCanvasOn && !BookControler.bookOn && !DemoEndPanelController.endPanelOn)
            {
                if (SceneManager.GetActiveScene().name == "Prologue")
                {
                    if (PrologueProgression.inTattoo)
                        Cursor.lockState = CursorLockMode.None;
                    else
                        Cursor.lockState = CursorLockMode.Locked;
                }
                else
                    Cursor.lockState = CursorLockMode.Locked;
                //Cursor.visible = false;                
            }
        }
    }

    public void PauseGame()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = 1.0f;
            pauseMenu.SetActive(false);
            OptionsMenu.SetActive(false);
            RuntimeManager.GetBus(songBus).setPaused(false);
            PixelCrushers.UIPanel.monitorSelection = true; // Allow dialogue UI to steal back input focus again.
            PixelCrushers.UIButtonKeyTrigger.monitorInput = true; // Re-enable hotkeys.
            PixelCrushers.DialogueSystem.DialogueManager.Unpause(); // Resume DS timers (e.g., sequencer commands).

            if (!playerHolding.positionFixedWithMouse && !MindPalace.tatMenuOn && !InspectionCanvasController.inspectCanvasOn && !BookControler.bookOn && !DemoEndPanelController.endPanelOn)
            {
                if (SceneManager.GetActiveScene().name == "Prologue")
                {
                    if (PrologueProgression.inTattoo)
                        Cursor.lockState = CursorLockMode.None;
                    else
                        Cursor.lockState = CursorLockMode.Locked;
                }
                else
                    Cursor.lockState = CursorLockMode.Locked;
                //Cursor.visible = false;
            }
        }
        else
        {
            isPaused = true;
            Time.timeScale = 0.0f;
            playerMovement.StopWalkingSFX();
            RuntimeManager.GetBus(songBus).setPaused(true);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ToTitleScene()
    {
        Time.timeScale = 1.0f;
        pauseMenu.SetActive(false);
        OptionsMenu.SetActive(false);
        RuntimeManager.GetBus(songBus).setPaused(false);
        PixelCrushers.UIPanel.monitorSelection = true; // Allow dialogue UI to steal back input focus again.
        PixelCrushers.UIButtonKeyTrigger.monitorInput = true; // Re-enable hotkeys.
        PixelCrushers.DialogueSystem.DialogueManager.Unpause(); // Resume DS timers (e.g., sequencer commands).

        fadeOutScreen.DOFade(1, 2f).OnComplete(() =>
        {
            SaveSystemManager.forceNewGame = true;
            SingleBell.bellRingCount = 0;
            GameProgress.progress = 0;
            GameProgress.isDemoTest = false;
            GameProgress.freeTest = false;
            SceneManager.LoadScene(0);
        });
    }

    public void SaveGame()
    {
        SaveSystemManager.instance.SaveGame();
        SaveSystemManager.manualSave = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1.0f;
        pauseMenu.SetActive(false);
        OptionsMenu.SetActive(false);
        RuntimeManager.GetBus(songBus).setPaused(false);
        PixelCrushers.UIPanel.monitorSelection = true; // Allow dialogue UI to steal back input focus again.
        PixelCrushers.UIButtonKeyTrigger.monitorInput = true; // Re-enable hotkeys.
        PixelCrushers.DialogueSystem.DialogueManager.Unpause(); // Resume DS timers (e.g., sequencer commands).

        fadeOutScreen.DOFade(1, 2f).OnComplete(() =>
        {
            SaveSystemManager.forceNewGame = true;
            SingleBell.bellRingCount = 0;
            GameProgress.progress = 0;
            GameProgress.isDemoTest = false;
            GameProgress.freeTest = false;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        });
    }

    public void LanguageButton()
    {        
        languageDropDown.SetActive(!languageDropDown.activeSelf);
        if (QuitMenu.activeSelf)
            QuitMenu.SetActive(false);
        if (OptionsMenu.activeSelf)
            OptionsMenu.SetActive(false);
    }

    public void PlayHoverSound()
    {
        RuntimeManager.PlayOneShot(buttonHoverSFX);
    }

    public void PlayMenuSound()
    {
        RuntimeManager.PlayOneShot(pauseMenuSFX);
    }


    public void ToggleOptionsMenu()
    {
        bool isActive = OptionsMenu.activeSelf;
        OptionsMenu.SetActive(!isActive);
        if (languageDropDown.activeSelf)
            languageDropDown.SetActive(false);
        if (QuitMenu.activeSelf)
            QuitMenu.SetActive(false);
    }

    public void ToggleQuitMenu()
    {
        bool isActive = QuitMenu.activeSelf;
        QuitMenu.SetActive(!isActive);
        if (languageDropDown.activeSelf)
            languageDropDown.SetActive(false);
        if (OptionsMenu.activeSelf)
            OptionsMenu.SetActive(false);
    }


    public void OpenOptionsMenu()
    {
        OptionsMenu.SetActive(true);
        pauseMenu.SetActive(false);
    }

    public void TurnOffSaveWindow()
    {
        SaveConfirmationWindow.SetActive(false);
    }


    public void BackToPauseMenu()
    {
        OptionsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void SetDemoEndPanelOn()
    {
        pauseMenuEndPanel.SetActive(true);
    }
}
