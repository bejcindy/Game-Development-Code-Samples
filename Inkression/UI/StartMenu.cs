using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;
//using FMOD;
using DG.Tweening;
using UnityEngine.UI;
using Beautify.Universal;

public class StartMenu : MonoBehaviour
{
    public int startType;
    public bool startAllowed;
    public bool fadeSound;
    public Transform startCam;
    public Animator cinemachineAnim;
    public Animator startButtonAnim;
    public Animator quitButtonAnim;
    public EventSystem eventSystem;
    public CanvasGroup warningPanel;
    Beautify.Universal.Beautify beautify;
    Sequence startSequence;
    Animator canvasAnim;
    float fadeVal;
    bool finishLoadMain;

    [SerializeField] private EventReference SelectAudio;
    private EventInstance Audio;

    FileDataHandler dataHandler;
    GameData gameData;
    public GameObject continueButton;
    bool noSaveFound;
    [SerializeField] GameObject creditPanel;
    [SerializeField] GameObject languageDropDown;

    public GameObject newGameButton, prologueButton, mainSceneButton;
    public static bool playedThroughOnce;    
    
    private void Awake()
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, SaveSystemManager.fileName);
        gameData = dataHandler.Load();
        //continueButton.SetActive(false);
        //noSaveFound = true;

        //set default frame rate to screen refresh rate
        //Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;

        if (gameData == null)
        {
            continueButton.SetActive(false);
            noSaveFound = true;
        }
        creditPanel.SetActive(true);
        //languageDropDown.SetActive(false);

        creditPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        creditPanel.transform.GetChild(0).GetComponent<CanvasGroup>().alpha = 0;
        //creditPanel.GetComponent<ScrollRect>().enabled = false;
        creditPanel.transform.GetComponent<CanvasGroup>().interactable = false;
        LayoutRebuilder.ForceRebuildLayoutImmediate(creditPanel.transform.GetChild(0).GetComponent<RectTransform>());

        BeautifySettings.UnloadBeautify();
        beautify = BeautifySettings.settings;
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        fadeVal = 0f;
        canvasAnim = GetComponent<Animator>();
        Audio = RuntimeManager.CreateInstance(SelectAudio);
        RuntimeManager.AttachInstanceToGameObject(Audio, GetComponent<Transform>(), GetComponent<Rigidbody>());
        warningPanel.DOFade(1, 3f).SetDelay(1);
        if (playedThroughOnce)
        {
            newGameButton.SetActive(false);
            prologueButton.SetActive(true);
            mainSceneButton.SetActive(true);
        }
        else
        {
            newGameButton.SetActive(true);
            prologueButton.SetActive(false);
            mainSceneButton.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {        
        Cursor.visible = false;

        if (Input.GetKeyDown(KeyCode.N))
            playedThroughOnce = false;
        if (Input.GetKeyDown(KeyCode.M))
            playedThroughOnce = true;

        if(Input.GetKey(KeyCode.LeftAlt))
        {
            if(Input.GetKeyDown(KeyCode.L))
                mainSceneButton.SetActive(true);
        }
        if (cinemachineAnim.isActiveAndEnabled)
        {
            if (cinemachineAnim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
            {
                if (noSaveFound || SaveSystemManager.forceNewGame)
                {
                    if (!finishLoadMain)
                        LoadPrologueScene();
                    else
                        LoadIzakayaScene();
                }

                else
                {
                    LoadIzakayaScene();
                }

                //LoadIzakayaScene();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape) && creditPanel.transform.GetChild(0).GetComponent<CanvasGroup>().alpha != 0)
        {
            CreditButton();
        }

        //if (startAllowed && Input.anyKeyDown && !Input.GetKeyDown(KeyCode.LeftControl) && !Input.GetKeyDown(KeyCode.LeftAlt) && !Input.GetKeyDown(KeyCode.RightAlt) && !Input.GetKeyDown(KeyCode.RightControl))
        //{
        //    fadeSound = true;
        //    RuntimeManager.PlayOneShot("event:/Sound Effects/UI/StartGame");
        //    StartGame();
        //}

        if (fadeSound)
        {
            if (fadeVal < 1)
                fadeVal += Time.deltaTime * 0.1f;
            else
            {
                Audio.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }

            Audio.setParameterByName("FadeOut", fadeVal);
        }


    }

    void EnterScene()
    {
        if (!finishLoadMain)
            LoadPrologueScene();
        else
            LoadIzakayaScene();
    }

    public void WarningUnderstood()
    {
        warningPanel.DOFade(0, 2f).OnComplete(()=>
        {
            warningPanel.gameObject.SetActive(false);
            canvasAnim.SetTrigger("WarningDone");
        });
        Audio.start();
        Audio.release();
    }

    public void WishlistButton()
    {
        Application.OpenURL("steam://store/2965930/#game_area_purchase");
    }

    public void DiscordButton()
    {
        Application.OpenURL("https://discord.com/invite/QcdEzHmFTu");
    }

    public void CreditButton()
    {
        if (creditPanel.transform.GetChild(0).GetComponent<CanvasGroup>().alpha == 0)
        {
            creditPanel.GetComponent<CanvasGroup>().DOKill();
            LayoutRebuilder.ForceRebuildLayoutImmediate(creditPanel.transform.GetChild(0).GetComponent<RectTransform>());
            //creditPanel.SetActive(false);
            creditPanel.GetComponent<Image>().DOFade(0.8f, .5f);
            //creditPanel.transform.GetChild(0).GetComponent<CanvasGroup>().DOFade(1, .5f).OnComplete(() => creditPanel.GetComponent<ScrollRect>().enabled = true);
            creditPanel.transform.GetChild(0).GetComponent<CanvasGroup>().DOFade(1, .5f).OnComplete(() => creditPanel.transform.GetComponent<CanvasGroup>().interactable = true);
            //creditPanel.transform.GetComponent<CanvasGroup>().interactable = false;
        }
        else
        {
            //creditPanel.SetActive(true);
            creditPanel.GetComponent<CanvasGroup>().DOKill();
            creditPanel.GetComponent<Image>().DOFade(0f, .5f);
            //creditPanel.transform.GetChild(0).GetComponent<CanvasGroup>().DOFade(0, .5f).OnComplete(() => creditPanel.GetComponent<ScrollRect>().enabled = false);
            creditPanel.transform.GetChild(0).GetComponent<CanvasGroup>().DOFade(0, .2f).OnComplete(() => creditPanel.transform.GetComponent<CanvasGroup>().interactable = false);
        }
        //if (creditPanel.activeSelf)        
        //    creditPanel.SetActive(false);                    
        //else
        //    creditPanel.SetActive(true);            

    }

    public void LanguageButton()
    {
        if (languageDropDown.activeSelf)
            languageDropDown.SetActive(false);
        else
            languageDropDown.SetActive(true);

    }

    public void AllowStartGame()
    {
        startAllowed = true;
    }

    public void LoadPrologueScene()
    {
        //SceneManager.LoadScene(1);
        SingleBell.bellRingCount = 0;
        GameProgress.progress = 0;
        GameProgress.isDemoTest = false;
        GameProgress.freeTest = false;
        SceneManager.LoadScene("Prologue");
    }

    public void LoadIzakayaScene()
    {
        //SceneManager.LoadScene(2);
        SingleBell.bellRingCount = 0;
        GameProgress.progress = 0;
        GameProgress.isDemoTest = false;
        GameProgress.freeTest = false;
        SceneManager.LoadScene("Izakaya");
    }

    public void StartGame()
    {
        switch (startType)
        {
            case 0:
                canvasAnim.SetTrigger("Start");
                break;
            case 1:
                canvasAnim.SetTrigger("Start");
                startSequence = DOTween.Sequence();
                startSequence.Append(startCam.DOMoveZ(82, 3.5f).SetEase(Ease.InQuart));

                float fadeVal = 0;
                startSequence.Join(DOTween.To(() => fadeVal, x => fadeVal = x, 1, 0.5f).SetDelay(3f).
                    OnUpdate(() => beautify.vignettingFade.Override(fadeVal))).
                    onComplete = EnterScene;

                float cAVal = 0;
                startSequence.Insert(2.5f, DOTween.To(() => cAVal, x => cAVal = x, 0.1f, startSequence.Duration()-1.5f).
                    OnUpdate(() => beautify.chromaticAberrationIntensity.Override(cAVal)));

                float blurVal = 0;
                startSequence.Insert(2.5f, DOTween.To(() => blurVal, x => blurVal = x, 1, startSequence.Duration()-1.5f).
                    OnUpdate(() => beautify.blurIntensity.Override(blurVal)));


                break;
        }

    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StartCameraLerp()
    {
        cinemachineAnim.enabled = true;
    }

    public void HoverSound()
    {
        RuntimeManager.PlayOneShot("event:/Sound Effects/UI/PauseMenuHover");
    }

    public void StartNewGame()
    {
        fadeSound = true;
        RuntimeManager.PlayOneShot("event:/Sound Effects/UI/StartGame");
        StartGame();
        SaveSystemManager.forceNewGame = true;
    }

    public void FinishLoadScene(bool loadMain)
    {
        fadeSound = true;
        RuntimeManager.PlayOneShot("event:/Sound Effects/UI/StartGame");
        StartGame();
        SaveSystemManager.forceNewGame = true;
        finishLoadMain = loadMain;
    }

    public void LoadSave()
    {
        fadeSound = true;
        RuntimeManager.PlayOneShot("event:/Sound Effects/UI/StartGame");
        StartGame();
        SaveSystemManager.forceNewGame = false;
    }
}
