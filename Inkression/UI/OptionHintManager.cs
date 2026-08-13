using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VInspector;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEngine.Localization.Settings;


public class OptionHintManager : MonoBehaviour
{
    public enum HintType
    {
        CIG_INHALE,
        CIG_EXHALE,
        CIG_THROW,
        SMOKEURGE,
        EAT_NOFOOD,
        EAT_AIMINGFOOD,
        EAT_HASFOOD,
        DRINK,
        STANDUP,
        STANDUP_80,
        INSPECTION,
        VINYLSTAND,
        CATTOY_MOUSE_DEFAULT,
        CATTOY_MOUSE_WINDED,
        CATTOY_BELL,
        PROLOGUE_TATTOO,
        KICK,
        RECORDPLAYER_PLAY,
        RECORDPLAYER_PAUSE,
        RECORDPLAYER_PUT,
        RECORDPLAYER_SWITCH,
        CATCAN_OPEN,
        CATCAN_FEED
    }

    Animator hint1Anim, hint2Anim;
    RectTransform hint1Rect, hint2Rect;
    [SerializeField] Image hint1Img, hint2Img;
    [SerializeField] TextMeshProUGUI hint1Text, hint2Text;

    [SerializeField] Animator hint3Anim;
    [SerializeField] RectTransform hint3Rect;
    [SerializeField] Image hint3Img;
    [SerializeField] TextMeshProUGUI hint3Text;

    [Foldout("Key Sprites")]
    [SerializeField] Sprite key_1;
    [SerializeField] Sprite key_2;
    [SerializeField] Sprite key_3;
    [SerializeField] Sprite key_4;
    [SerializeField] Sprite key_F;
    [SerializeField] Sprite key_TAB;
    [SerializeField] Sprite key_SPACE;
    [SerializeField] Sprite key_ESC;
    [SerializeField] Sprite key_Z;
    [SerializeField] Sprite key_X;
    [SerializeField] Sprite key_C;
    [SerializeField] Sprite key_V;
    [SerializeField] Sprite key_Q;
    [SerializeField] Sprite key_R;
    [SerializeField] Sprite LMB;
    [SerializeField] Sprite RMB;
    [SerializeField] Sprite scroll_UP;
    [SerializeField] Sprite scroll_DOWN;
    [SerializeField] Sprite scroll_UPDOWN;
    [EndFoldout]

    public TextTable hintTextTable;
    public float yOffset = 150f;

    //Renderer trackingRenderer;
    RectTransform rectTransform;
    RectTransform CanvasRect;
    bool stopDeactivate;
    CanvasGroup canvasGroup;
    float fadeSpeed = 8f;

    string currentLocale;
    HintType? currentHintType;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        hint1Anim = transform.GetChild(0).GetComponent<Animator>();
        hint2Anim = transform.GetChild(1).GetComponent<Animator>();

        hint1Rect = transform.GetChild(0).GetComponent<RectTransform>();
        hint2Rect = transform.GetChild(1).GetComponent<RectTransform>();

        hint1Anim.gameObject.SetActive(false);
        hint2Anim.gameObject.SetActive(false);

        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        CanvasRect = ReferenceTool.canvasRect;
    }

    private void Update()
    {
        //if(trackingRenderer)
        //    rectTransform.anchoredPosition = new Vector2(0, CalculatePosition(trackingRenderer).y + yOffset);
        if (MindPalace.tatMenuOn)
        {
            if (canvasGroup.alpha > 0)
                canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            else
                canvasGroup.alpha = 0;
        }
        else
        {
            if (canvasGroup.alpha < 1)
                canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            else
                canvasGroup.alpha = 1;
        }
        if (LocalizationSettings.SelectedLocale.Identifier.Code != currentLocale && currentHintType != null)
            ChangeLanguage();
    }

    public void HintSetUp(HintType hintType)
    {
        currentLocale = LocalizationSettings.SelectedLocale.Identifier.Code;
        //stopDeactivate = true;
        currentHintType = hintType;
        switch (hintType)
        {
            case HintType.CIG_INHALE:
                hint1Img.sprite = scroll_DOWN;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Inhale", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.CIG_EXHALE:
                hint1Img.sprite = scroll_UP;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Exhale", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.CIG_THROW:
                hint1Img.sprite = LMB;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Throw", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.SMOKEURGE:
                hint1Img.sprite = key_C;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Smoke?", Localization.language);
                hint2Img.sprite = key_SPACE;
                hint2Img.SetNativeSize();
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Nah", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint2Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                hint2Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.EAT_NOFOOD:
                //hint1Img.sprite = LMB;
                //hint1Img.SetNativeSize();
                //hint1Text.text = "Pick";
                hint2Img.sprite = RMB;
                hint2Img.SetNativeSize();
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Put Down", Localization.language);
                //hint1Anim.gameObject.SetActive(true);
                hint2Anim.gameObject.SetActive(true);
                //hint1Anim.SetBool("hintOn", true);
                hint1Anim.SetBool("hintOn", false);
                hint2Anim.SetBool("hintOn", true);
                //LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.EAT_AIMINGFOOD:
                hint1Img.sprite = LMB;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Pick", Localization.language);
                hint2Img.sprite = RMB;
                hint2Img.SetNativeSize();
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Put Down", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint2Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                hint2Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.EAT_HASFOOD:
                hint1Img.sprite = scroll_DOWN;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Eat", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint2Anim.SetBool("hintOn", false);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.DRINK:
                hint1Img.sprite = scroll_DOWN;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Drink", Localization.language);
                hint2Img.sprite = RMB;
                hint2Img.SetNativeSize();
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Put Down", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint2Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                hint2Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.STANDUP:
                hint1Img.sprite = key_SPACE;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Finish", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.STANDUP_80:
                hint3Img.sprite = key_R;
                hint3Img.SetNativeSize();
                hint3Text.text = hintTextTable.GetFieldTextForLanguage("Finish", Localization.language);
                hint3Anim.gameObject.SetActive(true);
                hint3Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint3Rect);
                break;
            case HintType.INSPECTION:
                hint1Img.sprite = key_ESC;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Exit View", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.VINYLSTAND:
                hint1Img.sprite = scroll_UPDOWN;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Choose", Localization.language);
                hint2Img.sprite = LMB;
                hint2Img.SetNativeSize();
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Pick Up", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint2Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                hint2Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.CATTOY_MOUSE_DEFAULT:
                hint1Img.sprite = scroll_UPDOWN;
                hint1Img.SetNativeSize();
                hint1Text.text = "Wind Up";
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.CATTOY_MOUSE_WINDED:
                hint1Img.sprite = LMB;
                hint1Img.SetNativeSize();
                hint1Text.text = "Put Down";
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.CATTOY_BELL:
                hint1Img.sprite = scroll_UPDOWN;
                hint1Img.SetNativeSize();
                hint1Text.text = "Ring";
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.PROLOGUE_TATTOO:
                hint1Img.sprite = LMB;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Tattoo", Localization.language);
                hint2Img.sprite = RMB;
                hint2Img.SetNativeSize();
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Refill Ink", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint2Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                hint2Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.KICK:
                hint1Img.sprite = key_Q;
                hint1Img.SetNativeSize();
                hint1Text.text = "Kick";
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.RECORDPLAYER_PLAY:
                hint1Img.sprite = RMB;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Play", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.RECORDPLAYER_PAUSE:
                hint1Img.sprite = RMB;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Pause", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                hint2Anim.SetBool("hintOn", false);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.RECORDPLAYER_PUT:
                hint1Img.sprite = LMB;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Put", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.RECORDPLAYER_SWITCH:
                hint1Img.sprite = RMB;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Play", Localization.language);
                hint2Img.sprite = LMB;
                hint2Img.SetNativeSize();
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Switch", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint2Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                hint2Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.CATCAN_OPEN:
                hint1Img.sprite = RMB;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Open", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.CATCAN_FEED:
                hint1Img.sprite = RMB;
                hint1Img.SetNativeSize();
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Feed", Localization.language);
                hint1Anim.gameObject.SetActive(true);
                hint1Anim.SetBool("hintOn", true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
        }
        //trackingRenderer = rend;

        //rectTransform.anchoredPosition = new Vector2(0, CalculatePosition(rend).y + yOffset);
        if (hintType == HintType.DRINK)
            rectTransform.anchoredPosition = new Vector2(0, 80f);
        else
            rectTransform.anchoredPosition = new Vector2(0, -120f);

    }

    void ChangeLanguage()
    {
        currentLocale = LocalizationSettings.SelectedLocale.Identifier.Code;
        switch (currentHintType)
        {
            case HintType.CIG_INHALE:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Inhale", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.CIG_EXHALE:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Exhale", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.CIG_THROW:               
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Throw", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.SMOKEURGE:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Smoke?", Localization.language);
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Nah", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.EAT_NOFOOD:
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Put Down", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.EAT_AIMINGFOOD:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Pick", Localization.language);
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Put Down", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.EAT_HASFOOD:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Eat", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.DRINK:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Drink", Localization.language);
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Put Down", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.STANDUP:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Finish", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.STANDUP_80:
                hint3Text.text = hintTextTable.GetFieldTextForLanguage("Finish", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint3Rect);
                break;
            case HintType.INSPECTION:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Exit View", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.VINYLSTAND:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Choose", Localization.language);
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Pick Up", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;            
            case HintType.PROLOGUE_TATTOO:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Tattoo", Localization.language);
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Refill Ink", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;            
            case HintType.RECORDPLAYER_PLAY:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Play", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.RECORDPLAYER_PAUSE:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Pause", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.RECORDPLAYER_PUT:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Put", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.RECORDPLAYER_SWITCH:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Play", Localization.language);
                hint2Text.text = hintTextTable.GetFieldTextForLanguage("Switch", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint2Rect);
                break;
            case HintType.CATCAN_OPEN:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Open", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
            case HintType.CATCAN_FEED:
                hint1Text.text = hintTextTable.GetFieldTextForLanguage("Feed", Localization.language);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hint1Rect);
                break;
        }
    }

    public void HintOff()
    {
        //stopDeactivate = false;
        if (hint1Anim.gameObject.activeSelf)
            hint1Anim.SetBool("hintOn", false);
        if (hint2Anim.gameObject.activeSelf)
            hint2Anim.SetBool("hintOn", false);
        currentHintType = null;
        Invoke("DeactivateChildren", 0.1f);
    }

    public void TatHintOff()
    {
        //stopDeactivate = false;
        if (hint3Anim.gameObject.activeSelf)
            hint3Anim.SetBool("hintOn", false);

        Invoke("DeactivateChildren", 0.1f);
    }

    void DeactivateChildren()
    {
        //if (!stopDeactivate)
        //{
        hint1Anim.gameObject.SetActive(false);
        hint2Anim.gameObject.SetActive(false);
        yOffset = 150f;
        //}
    }
    void DeactivateChild3()
    {
        hint3Anim.gameObject.SetActive(false);
        yOffset = 150f;
    }

    Vector2 CalculatePosition(Renderer rend)
    {
        //Renderer rend = trackingRenderer;
        //Renderer rend = _transform.GetComponent<Renderer>();
        Vector3[] points = new Vector3[8];
        points[0] = rend.bounds.min;
        points[1] = rend.bounds.min + new Vector3(rend.bounds.size.x, 0, 0);
        points[2] = rend.bounds.min + new Vector3(0, rend.bounds.size.y, 0);
        points[3] = rend.bounds.min + new Vector3(0, 0, rend.bounds.size.z);
        points[4] = rend.bounds.max - new Vector3(rend.bounds.size.x, 0, 0);
        points[5] = rend.bounds.max - new Vector3(0, rend.bounds.size.y, 0);
        points[6] = rend.bounds.max - new Vector3(0, 0, rend.bounds.size.z);
        points[7] = rend.bounds.max;

        Vector3 highestPoint = Vector3.zero;
        for (int i = 0; i < 8; i++)
        {
            if (highestPoint == Vector3.zero)
                highestPoint = points[i];
            else if (highestPoint.y < points[i].y)
                highestPoint = points[i];
        }
        Vector2 ViewportPosition = Camera.main.WorldToViewportPoint(highestPoint);
        Vector2 WorldObject_ScreenPosition = new Vector2(
        ((ViewportPosition.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f)),
        ((ViewportPosition.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f)));

        Vector2 clampedY = new Vector2(WorldObject_ScreenPosition.x, Mathf.Clamp(WorldObject_ScreenPosition.y, -Screen.height / 2 + 100f, Screen.height / 2 - 100f));
        return clampedY;
    }
}
