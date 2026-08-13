using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VInspector;
using DG.Tweening;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEngine.Localization.Settings;

public class ObjectHintManager : MonoBehaviour
{
    public enum HintType
    {
        LOOK,
        TALK,
        PICKFOOD,
        KICK,
        SIT,
        SIT_TOILET,
        OBSERVE,
        SWEEP,
        PICKUP,
        PICKUP_WITHDOT,
        CLICK,
        DOORBELL_CLICK,
        CAT,
        INSPECT,
        DOLL,
        GROCERYBOX,
        CONTAINER,
        CONTAINER_WITHDOT
    }

    public HintType hintType;
    public TextMeshProUGUI hintText;
    public TextTable hintTextTable;
    [SerializeField] Image keyImage;
    [SerializeField] Image dotImage;

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
    [SerializeField] Sprite LMB;
    [SerializeField] Sprite RMB;
    [SerializeField] Sprite scroll_UP;
    [SerializeField] Sprite scroll_DOWN;
    [EndFoldout]

    [SerializeField] Image iconImage;

    [Foldout("Hint Sprites")]
    [SerializeField] Sprite hint_LOOK;
    [SerializeField] Sprite hint_TALK;
    [SerializeField] Sprite hint_PICKFOOD;
    [SerializeField] Sprite hint_KICK;
    [SerializeField] Sprite hint_SIT;
    [SerializeField] Sprite hint_SWEEP;
    [SerializeField] Sprite hint_PICKUP;
    [SerializeField] Sprite hint_CLICK;
    [SerializeField] Sprite hint_CAT;
    [SerializeField] Sprite hint_INSPECT;
    [SerializeField] Sprite hint_GROCERYBOX;
    [SerializeField] Sprite hint_CONTAINER;
    [EndFoldout]

    public float hintTextOffset;
    [SerializeField] RectTransform hintPanel;

    public Transform objectToFollow;
    RectTransform CanvasRect;
    RectTransform rectTransform;

    Vector2 alternateHintPos;
    float minY;

    Animator anim;
    Animator panelAnim;
    bool showingIcon;
    bool showingHint;
    bool iconOnly;

    CanvasGroup canvasGroup;
    public bool vintage;

    string currentLocale;

    private void Awake()
    {
        CanvasRect = ReferenceTool.canvasRect;
        rectTransform = GetComponent<RectTransform>();
        alternateHintPos = new Vector2(0, (iconImage.GetComponent<RectTransform>().rect.height + hintPanel.rect.height) / 2);
        minY = -Screen.height / 2 + hintPanel.rect.height;
        anim = GetComponent<Animator>();
        panelAnim = hintPanel.GetComponent<Animator>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
    }
    private void Start()
    {
        rectTransform.anchoredPosition = CalculatePosition(objectToFollow);
        float delay;
        if (hintType == HintType.LOOK)
            delay = 0.5f;
        else
            delay = 0f;
        hintPanel.anchoredPosition = new Vector2(0, hintTextOffset);
        Invoke("HintSetUp", delay);
        //Invoke("StartFadeIn", delay);
        //if (iconOnly)
        //    StartCoroutine(LerpColor(1));
        //else
        //    canvasGroup.alpha = 1;
        //if (iconOnly)
        //    canvasGroup.DOFade(1, 0.1f);
        //else
        //    canvasGroup.alpha = 1;
        if (vintage)
            VintageLook();
    }

    void StartFadeIn()
    {
        if (iconOnly)
            canvasGroup.DOFade(1, 0.1f);
        else
            canvasGroup.alpha = 1;
    }

    void VintageLook()
    {
        Color whiteWithAlpha = new Color(1f, 1f, 1f, .68f);
        keyImage.color = Color.black;
        hintText.color = Color.black;
        hintPanel.GetComponent<Image>().color = whiteWithAlpha;
        minY = -Screen.height / 2 + hintPanel.rect.height * 2.5f;
    }

    private void Update()
    {
        //FOLLOW OBJECT POSITION        
        rectTransform.anchoredPosition = CalculatePosition(objectToFollow);

        if (hintType != HintType.TALK)
        {
            if (!vintage)
            {
                //MAKE SURE HINT DOESN'T GO OFF-SCREEN; USE ALTERNATE POS WHEN TOO CLOSE (Y DIFF < 150 OR ORIGINAL OFFSET IF IT'S LESS THAN 150)
                if ((rectTransform.anchoredPosition.y + hintTextOffset) < minY)
                {
                    if (hintTextOffset > -150)
                        hintPanel.anchoredPosition = alternateHintPos;
                    else
                    {
                        if (hintPanel.anchoredPosition.y < 0)
                        {
                            if (hintPanel.anchoredPosition.y > -150)
                                hintPanel.anchoredPosition = alternateHintPos;
                            else
                                hintPanel.anchoredPosition = new Vector2(0, minY - rectTransform.anchoredPosition.y);
                        }
                    }
                }
                else
                    hintPanel.anchoredPosition = new Vector2(0, hintTextOffset);
            }
            else
            {
                //MAKE SURE HINT DOESN'T GO OFF-SCREEN; USE ALTERNATE POS WHEN TOO CLOSE (Y DIFF < 150 OR ORIGINAL OFFSET IF IT'S LESS THAN 150)
                if ((rectTransform.anchoredPosition.y + hintTextOffset) < minY)
                {
                    if (hintTextOffset > -150)
                        hintPanel.anchoredPosition = alternateHintPos;
                }
                else
                    hintPanel.anchoredPosition = new Vector2(0, hintTextOffset);
            }
        }
        else
        {
            hintPanel.anchoredPosition = new Vector2(-rectTransform.anchoredPosition.x, -rectTransform.anchoredPosition.y - CanvasRect.rect.height * 0.5f + 150f);
        }
        //Debug.Log(rectTransform.anchoredPosition+ hintPanel.anchoredPosition);

        if (LocalizationSettings.SelectedLocale.Identifier.Code != currentLocale)
            ChangeLanguage();
    }

    void HintSetUp()
    {
        hintPanel.anchoredPosition = new Vector2(0, hintTextOffset);
        currentLocale = LocalizationSettings.SelectedLocale.Identifier.Code;
        switch (hintType)
        {
            case HintType.LOOK:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Look", Localization.language);
                keyImage.sprite = key_SPACE;
                iconImage.sprite = hint_LOOK;
                break;
            case HintType.TALK:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Talk", Localization.language);
                keyImage.sprite = key_SPACE;
                iconImage.sprite = hint_TALK;
                break;
            case HintType.PICKFOOD:
                iconImage.sprite = hint_PICKFOOD;
                anim.enabled = false;
                hintPanel.gameObject.SetActive(false);
                dotImage.gameObject.SetActive(false);
                iconImage.transform.localScale = new Vector3(.5f, .5f, .5f);
                iconOnly = true;
                break;
            //还需要吗？
            //case HintType.KICK:
            //    hintText.text = "Kick";
            //    keyImage.sprite = key_Q;
            //    hintImage.sprite = hint_KICK;
            //    break;
            case HintType.SIT:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Sit", Localization.language);
                keyImage.sprite = key_SPACE;
                iconImage.sprite = hint_SIT;
                break;
            case HintType.SIT_TOILET:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Use", Localization.language);
                keyImage.sprite = key_SPACE;
                iconImage.sprite = hint_SIT;
                break;
            case HintType.OBSERVE:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Sit", Localization.language);
                keyImage.sprite = key_SPACE;
                iconImage.sprite = hint_LOOK;
                break;
            case HintType.SWEEP:
                hintText.text = "Sweep";
                keyImage.sprite = RMB;
                iconImage.sprite = hint_SWEEP;
                anim.enabled = false;
                dotImage.gameObject.SetActive(false);
                iconImage.transform.localScale = new Vector3(.5f, .5f, .5f);
                ShowHint();
                iconOnly = true;
                break;
            case HintType.PICKUP:
                iconImage.sprite = hint_PICKUP;
                anim.enabled = false;
                hintPanel.gameObject.SetActive(false);
                dotImage.gameObject.SetActive(false);
                iconImage.transform.localScale = new Vector3(.5f, .5f, .5f);
                iconOnly = true;
                break;
            case HintType.PICKUP_WITHDOT:
                iconImage.sprite = hint_PICKUP;
                hintPanel.gameObject.SetActive(false);
                break;
            case HintType.CLICK:
                iconImage.sprite = hint_CLICK;
                anim.enabled = false;
                hintPanel.gameObject.SetActive(false);
                dotImage.gameObject.SetActive(false);
                iconImage.transform.localScale = new Vector3(.5f, .5f, .5f);
                iconOnly = true;
                break;
            case HintType.DOORBELL_CLICK:
                iconImage.sprite = hint_CLICK;
                hintPanel.gameObject.SetActive(false);
                break;
            case HintType.CAT:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Check", Localization.language);
                keyImage.sprite = key_SPACE;
                iconImage.sprite = hint_CAT;
                break;
            case HintType.INSPECT:
                iconImage.sprite = hint_INSPECT;
                hintPanel.gameObject.SetActive(false);
                break;
            case HintType.DOLL:
                iconImage.sprite = hint_PICKUP;
                hintPanel.gameObject.SetActive(false);
                break;
            case HintType.GROCERYBOX:
                iconImage.sprite = hint_GROCERYBOX;
                anim.enabled = false;
                hintPanel.gameObject.SetActive(false);
                dotImage.gameObject.SetActive(false);
                iconImage.transform.localScale = new Vector3(.5f, .5f, .5f);
                iconOnly = true;
                break;
            case HintType.CONTAINER:
                iconImage.sprite = hint_CONTAINER;
                anim.enabled = false;
                hintPanel.gameObject.SetActive(false);
                dotImage.gameObject.SetActive(false);
                iconImage.transform.localScale = new Vector3(.5f, .5f, .5f);
                iconOnly = true;
                break;
            case HintType.CONTAINER_WITHDOT:
                iconImage.sprite = hint_CONTAINER;
                hintPanel.gameObject.SetActive(false);
                break;
        }
        if (!iconOnly)
            LayoutRebuilder.ForceRebuildLayoutImmediate(hintPanel);
        if (hintPanel.gameObject.activeSelf)
            keyImage.SetNativeSize();
        iconImage.SetNativeSize();
        StartFadeIn();
    }

    void ChangeLanguage()
    {
        currentLocale = LocalizationSettings.SelectedLocale.Identifier.Code;
        switch (hintType)
        {
            case HintType.LOOK:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Look", Localization.language);
                break;
            case HintType.TALK:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Talk", Localization.language);
                break;
            case HintType.SIT:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Sit", Localization.language);
                break;
            case HintType.SIT_TOILET:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Use", Localization.language);
                break;
            case HintType.OBSERVE:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Sit", Localization.language);
                break;
            case HintType.SWEEP:
                hintText.text = "Sweep";
                break;
            case HintType.CAT:
                hintText.text = hintTextTable.GetFieldTextForLanguage("Check", Localization.language);
                break;            
        }        
    }

    IEnumerator LerpColor(float targetAlpha)
    {
        float currentAlpha = canvasGroup.alpha;
        float t = 0;
        while (t < 0.1f)
        {
            if (targetAlpha != 0)
                canvasGroup.alpha = Mathf.Lerp(0, targetAlpha, t / 0.1f);
            else
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, 0, t / 0.1f);
            t += Time.deltaTime;
            yield return null;
        }
        if (targetAlpha != 0)
            canvasGroup.alpha = targetAlpha;
        else
        {
            canvasGroup.alpha = 0;
            Destroy(gameObject);
        }
        yield break;
    }

    /// <summary>
    /// DON'T USE THIS! CALL Disappear() INSTEAD! THIS IS FOR ANIMATION!
    /// </summary>
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    public Vector2 CalculatePosition(Transform _transform)
    {
        Vector2 ViewportPosition = Camera.main.WorldToViewportPoint(_transform.position);
        Vector2 WorldObject_ScreenPosition = new Vector2(
        ((ViewportPosition.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f)),
        ((ViewportPosition.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f)));
        return WorldObject_ScreenPosition;
    }

    public void DotToIcon()
    {
        if (!showingIcon)
        {
            anim.SetTrigger("showIcon");
            showingIcon = true;
        }
    }

    public void IconToDot()
    {
        if (showingIcon)
        {
            anim.SetTrigger("showDot");
            showingIcon = false;
        }
    }

    public void Disappear()
    {
        //if (iconOnly)
        //    StartCoroutine(LerpColor(0));
        //else
        //    anim.SetTrigger("disappear");
        //if (iconOnly)
        anim.enabled = false;
        canvasGroup.DOFade(0, 0.1f).OnComplete(() =>
        {
            if (gameObject != null)
                Destroy(gameObject);
        });
        //else
        //    anim.SetTrigger("disappear");
    }

    public void ShowHint()
    {
        if (!showingHint)
        {
            panelAnim.SetBool("hintOn", true);
            showingHint = true;
        }
    }

    public void HideHint()
    {
        if (showingHint)
        {
            panelAnim.SetBool("hintOn", false);
            showingHint = false;
        }
    }
}
