using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using TMPro;
using System;
using UnityEngine.UI;
using Unity.VisualScripting;
using VInspector;
using Beautify.Universal;
using DG.Tweening;

[Serializable]
public class HintTexts
{
    [TextArea]
    public string throwHint, inhaleHint, exhaleHint, lookHint, drinkHint, kickHint, scrollHint, sitHint, standHint, cigHint, chopHint, pickFoodHint, eatHint, powderHint, tabHint, tableDrinkHint, drinkAndThrowHint, tattooViewHint, outerPanelHint, pizzaHint, vinylStandHint, convoHint, broomHint, sweepHint, soccerHint;
}

public class DataHolder : MonoBehaviour
{
    #region Blur Related
    public static LookingObject currentFocus;
    public CinemachineVirtualCamera focusVcam;
    public CinemachineVirtualCamera focusVcam2;
    public static CinemachineVirtualCamera currentCam;
    public static CinemachineVirtualCamera focusCinemachine;
    public static CinemachineVirtualCamera focusCinemachine2;
    public static CinemachinePOV pov;

    static float focusDist = .75f;
    public Volume postProcessingVolume, chromaticVolume;
    static Volume v;
    static CinemachineVirtualCamera playerCinemachine;
    #endregion

    #region Hint Related
    [SerializeField]
    [InspectorName("Hints")]
    HintTexts hintsReference;

    [Foldout("MouseUIs")]
    public Sprite LeftClick;
    public Sprite RightClick;
    public Sprite ScrollDown;
    public Sprite ScrollUp;
    public Sprite Scroll;
    public Sprite Drag;
    public Sprite Tab;
    public Sprite Esc;

    [Foldout("HintPrefabs")]
    public GameObject hintPanelPrefab;
    public GameObject hintPanelHorizontalPrefab;
    public GameObject hintPrefab;
    public GameObject tatHintPrefab;
    public Transform canvasRef;
    public Transform mainCanvasRef;

    public static HintTexts hints;

    static Transform canvas;
    static Transform mainCanvas;
    static GameObject hintPanel;
    static GameObject hintPanHori;
    static GameObject hintPref;
    static GameObject tatHint;
    static List<string> currentHints;
    static List<GameObject> hintPanels;

    static Dictionary<string, GameObject> hintPanelsDict = new Dictionary<string, GameObject>();

    bool hintOff;

    #endregion

    public static bool canMakeSound;
    float beginningAudioCoolDownTimer;

    private void OnApplicationQuit()
    {
        DOTween.KillAll();
    }

    void Start()
    {
        //reset public static variables
        currentFocus = null;
        canMakeSound = false;

        focusCinemachine = focusVcam;
        focusCinemachine2 = focusVcam2;
        playerCinemachine = ReferenceTool.playerCinemachine;
        v = postProcessingVolume;

        hintPanel = hintPanelPrefab;
        hintPanHori = hintPanelHorizontalPrefab;
        hintPref = hintPrefab;
        tatHint = tatHintPrefab;
        hints = hintsReference;
        currentHints = new List<string>();
        hintPanels = new List<GameObject>();

        canvas = canvasRef;
        mainCanvas = mainCanvasRef;
        pov = ReferenceTool.playerPOV;
        SMHAdjustTest.beautify.blurIntensity.Override(0f);
    }

    void Update()
    {
        if (beginningAudioCoolDownTimer < 2)
            beginningAudioCoolDownTimer += Time.deltaTime;
        else
        {
            canMakeSound = true;
        }


        if (hintOff && hintPanels.Count != 0)
        {
            for (int i = 0; i < hintPanels.Count; i++)
            {
                hintPanels[i].SetActive(false);
            }
        }
        else if (!hintOff && hintPanels.Count != 0)
        {
            for (int i = 0; i < hintPanels.Count; i++)
            {
                hintPanels[i].SetActive(true);
            }
        }

        //if (canvasRef)
        //{
        //    if (canvasRef.GetSiblingIndex() != (canvasRef.parent.childCount - 2))
        //        canvasRef.SetSiblingIndex(canvasRef.parent.childCount - 2);
        //}
    }


    #region Focusing and Unfocusing
    public static void LookingFocus()
    {
        ReferenceTool.playerLeftHand.bypassThrow = true;

        DOTween.To(() => focusDist, x => focusDist = x, 2f, 2f).OnUpdate(() =>
        {
            SMHAdjustTest.beautify.blurIntensity.Override(focusDist);
        }).OnComplete(() => { Unfocus(); });

    }

    public static void Unfocus()
    {
        playerCinemachine.LookAt = null;
        focusCinemachine.Priority = 1;
        focusCinemachine.LookAt = null;
        playerCinemachine.ForceCameraPosition(playerCinemachine.transform.position, focusCinemachine.transform.rotation);
        pov.m_HorizontalAxis.m_MaxSpeed = OptionPanelManager.camHorizontalSpeed;
        pov.m_VerticalAxis.m_MaxSpeed = OptionPanelManager.camVerticalSpeed;

        DOTween.To(() => focusDist, x => focusDist = x, 0f, 1f).OnUpdate(() =>
        {
            SMHAdjustTest.beautify.blurIntensity.Override(focusDist);
        }).OnComplete(() =>
        {
            ReferenceTool.playerHolding.looking = false;
            ReferenceTool.playerMovement.enabled = true;
            ReferenceTool.playerLeftHand.bypassThrow = false;
            currentFocus.focusingThis = false;
            currentFocus = null;
        });
    }

    public static void FocusOnObject()
    {

        DOTween.To(() => focusDist, x => focusDist = x, 10f, 3f).OnUpdate(() =>
        {
            SMHAdjustTest.beautify.blurIntensity.Override(focusDist);
        }).OnComplete(() => { UnfocusObject(); });
    }

    public static void UnfocusObject()
    {
        DOTween.To(() => focusDist, x => focusDist = x, 0f, 2f).OnUpdate(() =>
        {
            SMHAdjustTest.beautify.blurIntensity.Override(focusDist);
        });
    }

    public static void FocusOnObject(Transform obj)
    {
        CinemachineVirtualCamera fromCam = ReferenceTool.playerBrain.ActiveVirtualCamera as CinemachineVirtualCamera;

        if (fromCam == playerCinemachine)
        {
            focusCinemachine.LookAt = obj;
            //playerCinemachine.LookAt = obj;
            pov.m_HorizontalAxis.m_MaxSpeed = 0f;
            pov.m_VerticalAxis.m_MaxSpeed = 0f;
            focusCinemachine.m_Priority = 11;
            currentCam = playerCinemachine;
        }
        else if (fromCam != focusCinemachine && fromCam != focusCinemachine2)
        {
            currentCam = fromCam;
            CinemachinePOV cinemachinePOV = currentCam.GetCinemachineComponent<CinemachinePOV>();
            focusCinemachine.LookAt = obj;
            currentCam.LookAt = obj;
            if (cinemachinePOV != null)
            {
                cinemachinePOV.m_HorizontalAxis.m_MaxSpeed = 0f;
                cinemachinePOV.m_VerticalAxis.m_MaxSpeed = 0f;
            }
            focusCinemachine.m_Priority = currentCam.Priority + 1;
        }
        else
        {
            if (fromCam == focusCinemachine)
            {
                focusCinemachine2.LookAt = obj;
                currentCam.LookAt = obj;
                focusCinemachine2.m_Priority = currentCam.Priority + 1;
                focusCinemachine.Priority = 1;
                focusCinemachine.LookAt = null;
            }
            else
            {
                focusCinemachine.LookAt = obj;
                currentCam.LookAt = obj;
                focusCinemachine.m_Priority = currentCam.Priority + 1;
                focusCinemachine2.Priority = 1;
                focusCinemachine2.LookAt = null;
            }
        }

    }

    public static void ResetFocusCamera()
    {
        if (currentCam != null)
        {
            if (focusCinemachine.Priority > 1 || focusCinemachine2.Priority > 1)
            {
                currentCam.ForceCameraPosition(currentCam.transform.position, focusCinemachine.Priority > focusCinemachine2.Priority ? focusCinemachine.transform.rotation : focusCinemachine2.transform.rotation);
            }
            currentCam.LookAt = null;
            focusCinemachine.Priority = 1;
            focusCinemachine.LookAt = null;
            focusCinemachine2.Priority = 1;
            focusCinemachine2.LookAt = null;
            CinemachinePOV cinemachinePOV = currentCam.GetCinemachineComponent<CinemachinePOV>();
            cinemachinePOV.m_HorizontalAxis.m_MaxSpeed = OptionPanelManager.camHorizontalSpeed;
            cinemachinePOV.m_VerticalAxis.m_MaxSpeed = OptionPanelManager.camVerticalSpeed;
            currentCam = null;
        }

    }
    #endregion

    #region Hint Related
    /// <summary>
    /// put in "DataHolder.hints.blablabla" for the string
    /// </summary>
    /// <param name="hint"></param>
    public static void ShowHint(string hint)
    {
        if (!hintPanelsDict.TryGetValue(hint, out _))
        //        if (currentHints.Count == 0 || !currentHints.Contains(hint))
        {
            GameObject instantiatedPanel = Instantiate(hintPanel, canvas);
            List<Image> imgs = new List<Image>();
            List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
            string[] parsed = hint.Split("\n");
            foreach (string s in parsed)
            {
                GameObject instantiatedHintGroup = Instantiate(hintPref, instantiatedPanel.transform);
                int buttonInt = s.IndexOf(" ");
                string button = s.Substring(0, buttonInt);
                string usage = s.Replace(button, "");
                instantiatedHintGroup.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = button;
                imgs.Add(instantiatedHintGroup.transform.GetChild(0).GetComponent<Image>());
                texts.Add(instantiatedHintGroup.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>());
                instantiatedHintGroup.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = usage;
                texts.Add(instantiatedHintGroup.transform.GetChild(1).GetComponent<TextMeshProUGUI>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(instantiatedHintGroup.GetComponent<RectTransform>());
            }
            hintPanelsDict.Add(hint, instantiatedPanel);

        }

    }

    public static void ShowHintHorizontal(string hint)
    {

        if (!hintPanelsDict.TryGetValue(hint, out _))
        {
            GameObject instantiatedPanel = Instantiate(hintPanHori, mainCanvas);
            List<Image> imgs = new List<Image>();
            List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
            string[] parsed = hint.Split("\n");
            foreach (string s in parsed)
            {
                GameObject instantiatedHintGroup = Instantiate(tatHint, instantiatedPanel.transform);
                int buttonInt = s.IndexOf(" ");
                string button = s.Substring(0, buttonInt);
                string usage = s.Replace(button, "");
                instantiatedHintGroup.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = button;
                imgs.Add(instantiatedHintGroup.transform.GetChild(0).GetComponent<Image>());
                texts.Add(instantiatedHintGroup.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>());
                instantiatedHintGroup.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = usage;
                texts.Add(instantiatedHintGroup.transform.GetChild(1).GetComponent<TextMeshProUGUI>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(instantiatedHintGroup.GetComponent<RectTransform>());
                if (button.Contains("Esc"))
                    instantiatedHintGroup.GetComponent<Button>().onClick.AddListener(() => MindPalace.escButton = true);
                else if (button.Contains("Tab"))
                    instantiatedHintGroup.GetComponent<Button>().onClick.AddListener(() => MindPalace.tabButton = true);                
            }
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(instantiatedPanel.GetComponent<RectTransform>());
            
            hintPanelsDict.Add(hint, instantiatedPanel);
            instantiatedPanel.GetComponent<RectTransform>().SetSiblingIndex(mainCanvas.childCount - 2);
        }
    }

    public static void HideHint(string hintToHide)
    {
        if (hintPanelsDict.TryGetValue(hintToHide, out _))
        {
            GameObject hint = hintPanelsDict[hintToHide];
            if (hint != null)
            {
                hintPanelsDict.Remove(hintToHide);
                Destroy(hint);
            }
        }

    }

    public static void HideHintExceptThis(string hintToExclude)
    {
        bool hidden = false;
        if (hintPanels.Count != 0 && !hidden)
        {
            for (int i = 0; i < hintPanels.Count; i++)
            {
                if (currentHints[i] != hintToExclude && currentHints[i] != null)
                {
                    Destroy(hintPanels[i]);
                    hintPanels.Remove(hintPanels[i]);
                    currentHints.Remove(currentHints[i]);
                    hidden = true;
                }
            }
        }
    }

    public static void MoveHintToLower()
    {
        //canvas.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
        //canvas.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        //canvas.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
        //canvas.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30f);
        canvas.GetComponent<VerticalLayoutGroup>().padding.top = 100;
        LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
    }

    public static void MoveHintToTop()
    {
        //canvas.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
        //canvas.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        //canvas.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        //canvas.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        canvas.GetComponent<VerticalLayoutGroup>().padding.top = 20;
        LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
    }

    #endregion

    #region Tattoo Panel
    //public void TurnOnTattooPanel()
    //{
    //    tatCanvasAnim.Play("")
    //}

    //public void TurnOffTattooPanel()
    //{

    //}
    #endregion
}
