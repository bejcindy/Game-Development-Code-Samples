using Beautify;
using Beautify.Universal;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Rendering;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class LevelIntroUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform filmStripPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private LocalizeStringEvent lowTimerText;
    [SerializeField] private LocalizeStringEvent midTimerText;
    [SerializeField] private LocalizeStringEvent highTimerText;
    [SerializeField] private RectTransform criteriaGroup;

    [Header("Animation Settings")]
    [SerializeField] private float filmStripSlideInDuration = 0.7f;
    [SerializeField] private float titlePopDuration = 0.5f;
    [SerializeField] private float criteriaSlideInDelay = 0.18f;
    [SerializeField] private float criteriaSlideInDuration = 0.45f;
    [SerializeField] private float filmStripSlideOutDelay = 1.5f;
    [SerializeField] private float filmStripSlideOutDuration = 0.7f;
    [SerializeField] private float blurLerpDuration = 0.5f;

    private List<GameObject> criteriaItems = new List<GameObject>();
    private Beautify.Universal.Beautify beautify;
    private Coroutine showRoutine;
    EventInstance filmEventInstance;

    private void Awake()
    {
        titleText.alpha = 0;
        titleText.transform.localScale = Vector3.one * 0.8f;
        foreach (Transform child in criteriaGroup)
        {
            criteriaItems.Add(child.gameObject);
            var canvasGroup = child.GetComponent<CanvasGroup>();
            var rt = child.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-Screen.width, -criteriaItems.IndexOf(child.gameObject) * 40f);
            canvasGroup.alpha = 0; // Slightly yellowish, transparent
        }
    }

    public void Show()
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        BeautifySettings.UnloadBeautify();
        beautify = BeautifySettings.settings;

        // Get level info
        LevelState levelState = ObjectFinder.Instance.levelState;


        float goldSeconds = levelState.GetGoldTimeThresholdSeconds();
        float silverSeconds = levelState.GetSilverTimeThresholdSeconds();
        float bronzeSeconds = levelState.GetBronzeTimeThresholdSeconds();

        if (lowTimerText != null)
        {
            lowTimerText.StringReference.Arguments = new object[] { FormatTimeAsMinutes(goldSeconds) };
            lowTimerText.RefreshString();
        }

        if (midTimerText != null)
        {
            midTimerText.StringReference.Arguments = new object[] { FormatTimeAsMinutes(silverSeconds) };
            midTimerText.RefreshString();
        }

        if (highTimerText != null)
        {
            highTimerText.StringReference.Arguments = new object[] { FormatTimeAsMinutes(bronzeSeconds) };
            highTimerText.RefreshString();
        }

        showRoutine = StartCoroutine(ShowSequence());
    }

    private string FormatTimeAsMinutes(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        return minutes.ToString();
    }

    private IEnumerator ShowSequence()
    {
        // Lerp blur in
        yield return StartCoroutine(LerpBlur(0.5f, blurLerpDuration));

        // Slide in film strip
        filmEventInstance = AudioManager.Instance.CreateInstance(FMODEvents.Instance.Film);
        filmEventInstance.start();
        yield return filmStripPanel.DOAnchorPos(Vector2.zero, filmStripSlideInDuration).SetEase(Ease.OutCubic).WaitForCompletion();

        // Pop/fade in title (noir: pop with a little shake, like a typewriter effect)
        titleText.DOFade(1, titlePopDuration * 0.7f);
        Debug.Log("Checking if titleText causing dotween null reference");
        titleText.transform.DOScale(1.1f, titlePopDuration * 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            titleText.transform.DOScale(1f, titlePopDuration * 0.5f).SetEase(Ease.InOutSine);
        });
        yield return new WaitForSeconds(titlePopDuration);

        // Slide in criteria one by one, fade color left to right
        EventReference[] criteriaSounds = new EventReference[]
        {
            FMODEvents.Instance.Criteria1,
            FMODEvents.Instance.Criteria2,
            FMODEvents.Instance.Criteria3,
            FMODEvents.Instance.Criteria4
        };
        for (int i = 0; i < criteriaItems.Count; i++)
        {
            var go = criteriaItems[i];
            var canvasGroup = go.GetComponent<CanvasGroup>();
            var rt = go.GetComponent<RectTransform>();

            Sequence s = DOTween.Sequence();
            s.Append(rt.DOAnchorPosX(225, criteriaSlideInDuration).SetEase(Ease.OutCubic));
            Debug.Log($"Checking if canvasGroup causing dotween null reference for criteria {i}");
            s.Join(canvasGroup.DOFade(1, criteriaSlideInDuration * 0.8f));
            s.Play();
            AudioManager.Instance.PlayOneShot(criteriaSounds[i]);
            yield return new WaitForSeconds(criteriaSlideInDelay);
        }

        // Wait before sliding out
        yield return new WaitForSeconds(filmStripSlideOutDelay);

        // Slide out to right
        filmEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        filmEventInstance.release();
        yield return filmStripPanel.DOAnchorPos(new Vector2(2000, 0), filmStripSlideOutDuration).SetEase(Ease.InCubic).WaitForCompletion();

        // Lerp blur out
        yield return StartCoroutine(LerpBlur(0f, blurLerpDuration));

        // Notify state machine to continue
        EventsMaster.Event_OnPlayerStateChange(new CarryState(PlayerStateMachine.Instance));
    }

    private IEnumerator LerpBlur(float target, float duration)
    {
        if (beautify == null) yield break;
        float start = beautify.blurIntensity.value;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            beautify.blurIntensity.value = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        beautify.blurIntensity.value = target;
    }

    public void Hide()
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        titleText.alpha = 0;

        if (beautify != null)
            StartCoroutine(LerpBlur(0f, blurLerpDuration));
    }

    private void OnDestroy()
    {
        DOTween.Kill(filmStripPanel);
        DOTween.Kill(titleText);
        if (titleText != null)
            DOTween.Kill(titleText.transform);

        foreach (var item in criteriaItems)
        {
            if (item != null)
            {
                DOTween.Kill(item.transform);
                DOTween.Kill(item.GetComponent<CanvasGroup>());
            }
        }

        if (filmEventInstance.isValid())
        {
            filmEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            filmEventInstance.release();
        }
    }
}