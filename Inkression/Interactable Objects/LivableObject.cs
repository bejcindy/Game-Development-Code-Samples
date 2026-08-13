using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using static Cinemachine.CinemachineOrbitalTransposer;
using VInspector;
using static GameData;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LivableObject : MonoBehaviour, ISaveSystem
{

    protected Transform player;
    protected PlayerHolding playerHolding;
    protected PlayerLeftHand playerLeftHand;
    protected PlayerMovement playerMovement;
    protected Material mat;
    protected CinemachineVirtualCamera playerCam;

    [SerializeField]
    protected bool processable;
    protected bool checkVisible;
    protected bool pickType;
    protected bool hasGroupControl;
    protected GroupMaster groupControl;
    protected Vector3 pointOnScreen;

    protected string id;


    [Foldout("State")]
    public bool activated;
    protected bool firstActivated;
    protected bool singleCheck;
    public bool noChangeLayer;

    public bool interactable;
    public bool selected;
    [SerializeField] protected bool isVisible;
    public bool centerFocused;
    [SerializeField]  protected bool showDot;
    [SerializeField]  protected bool showIcon;


    [Foldout("Basic")]
    [SerializeField] protected float minDist;
    [SerializeField] protected Renderer rend;

    [Foldout("Tattoo")]
    public CharacterTattoo myTattoo;

    [Foldout("Rigged")]
    [SerializeField] protected bool rigged;
    [SerializeField] protected RiggedVisibleDetector visibleDetector;


    [Foldout("Special")]
    public bool overrideMechLock;

    [Foldout("Activate Event")]
    public UnityEvent OnActivateEvent;

    [Foldout("Hint")]
    public ObjectHintManager.HintType myHint;
    protected Transform hintFollow;
    protected float hintTextOffset;
    protected GameObject hint;
    protected ObjectHintManager objectHintManager;

    [SerializeField] 
    public ScriptController myZone;
    public bool scriptOff;

    bool[] checkBoundVisible;

    protected virtual void Awake()
    {
        if(SceneManager.GetActiveScene().name != "Prologue")
        {
            if (GetComponent<ObjectID>())
                id = GetComponent<ObjectID>().id;
        }

        if (GetComponent<Renderer>() != null)
        {
            rend = GetComponent<Renderer>();
        }
        mat = rend.material;
        mat.EnableKeyword("_WhiteDegree");
        myZone = GetComponentInParent<ScriptController>();
    }

    protected virtual void Start()
    {
        player = ReferenceTool.player;
        playerHolding = ReferenceTool.playerHolding;
        playerLeftHand = ReferenceTool.playerLeftHand;
        playerMovement = ReferenceTool.playerMovement;


        if (GetComponent<GroupMaster>() != null)
        {
            hasGroupControl = true;
            groupControl = GetComponent<GroupMaster>();
        }
        checkBoundVisible = new bool[8];
        playerCam = ReferenceTool.playerCinemachine;

    }

    protected virtual void OnDisable()
    {
        isVisible = false;
        interactable = false;

        if (!noChangeLayer)
        {
            if (activated)
            {
                gameObject.layer = 17;
                foreach (Transform child in transform)
                {
                    child.gameObject.layer = 17;
                }
            }
            else
            {
                gameObject.layer = 0;
                foreach (Transform child in transform)
                {
                    child.gameObject.layer = 0;
                }
            }
        }

    }

    protected virtual void Update()
    {
        if (myZone != null)
        {
            if (myZone.inZone)
                scriptOff = false;
            else
                scriptOff = true;
        }

        processable = (GameProgress.mechUnlocked || overrideMechLock) && !scriptOff && !PauseMenu.isPaused && !MindPalace.tatMenuOn;


        if (processable)
        {
            if (singleCheck)
            {
                if (!activated)
                {
                    DetectVisible();
                    if (isVisible)
                        DetectInteractable();
                    else
                        interactable = false;
                }
                else
                {
                    ActivationCheck();
                }
            }
            else
            {
                DetectVisible();
                DetectInteractable();
                if(activated)
                    ActivationCheck();
            }



        }
        else
        {
            isVisible = false;
            interactable = false;
        }

        LayerCheck();

    }


    protected virtual void DetectVisible()
    {
        if (rend)
        {
            checkVisible = IsObjectVisible(rend);
            if (!rigged)
            {
                if (checkVisible && !playerHolding.selectedNPC)
                {
                    isVisible = IsInView();
                }
                else
                    isVisible = false;
            }
            else
            {
                isVisible = visibleDetector.isVisible;
            }
        }
    }

    protected virtual void DetectInteractable()
    {
        if (Vector3.Distance(transform.position, Camera.main.transform.position) <= minDist)
        {
            if (isVisible)
                interactable = true;
            else
                interactable = false;
        }
        else
        {
            interactable = false;
        }
    }

    public bool GetIsVisible()
    {
        return isVisible;
    }

    protected virtual void ActivationCheck()
    {
        if (!firstActivated)
        {
            TurnOnColor(mat);

            if (hasGroupControl)
            {
                groupControl.activateAll = true;
            }
        }
        if (OnActivateEvent != null)
        {
            OnActivateEvent.Invoke();
        }

        if (!noChangeLayer)
        {
            ChangeActivatedLayer();
        }
    }

    protected virtual void TurnOnColor(Material material)
    {
        if (pickType)
        {
            snapshot = FMODUnity.RuntimeManager.CreateInstance("snapshot:/EnableObject");
            snapshot.start();
        }
        firstActivated = true;
        material.DOFloat(0, "_WhiteDegree", 2).SetEase(Ease.InQuad).OnComplete(ColorChangeComplete);
    }

    protected virtual void ColorChangeComplete()
    {
        if (myTattoo && !myTattoo.triggered)
            Invoke(nameof(TurnOnTat), 1f);


    }

    protected virtual void ChangeActivatedLayer()
    {
        if (gameObject.layer != 17 && gameObject.layer != 18)
        {
            if (gameObject.layer == 6 || gameObject.layer == 18)
            {
                gameObject.layer = 18;
            }
            else
            {
                gameObject.layer = 17;
            }

        }
    }

    protected virtual void LayerCheck() { }
    protected virtual void HintCheck() { }

    public virtual void OnBecameSelected() { }
    public virtual void OnBecameDeselected() { }

    protected virtual void GenerateHint()
    {
        if (!hint)
        {
            hint = Instantiate(ReferenceTool.objHint, ReferenceTool.objHintPanel);
            objectHintManager = hint.GetComponent<ObjectHintManager>();
            objectHintManager.objectToFollow = hintFollow;
            objectHintManager.hintTextOffset = hintTextOffset;
            objectHintManager.hintType = myHint;
            hint.SetActive(true);
        }

    }

    protected virtual void TurnOffHint()
    {
        if (hint)
        {
            objectHintManager.Disappear();
            hint = null;
            objectHintManager = null;
        }
    }

    protected virtual void ShowIcon()
    {
        if (!interactable)
        {
            if (Vector3.Distance(transform.position, Camera.main.transform.position) <= minDist)
            {
                if (isVisible)
                    showIcon = true;
                else
                    showIcon = false;
            }
            else
                showIcon = false;
        }
        else
            showIcon = true;
    }

    protected virtual void ShowDot()
    {
        if (!showIcon)
        {
            if (Vector3.Distance(transform.position, Camera.main.transform.position) <= minDist * 2)
            {
                if (isVisible)
                    showDot = true;
                else
                    showDot = false;
            }
            else
                showDot = false;
        }
        else
            showDot = true;
    }



    public void DisableInteraction()
    {
        minDist = 0;
    }

    public void EnableInteraction()
    {
        minDist = 3;
    }

    public void TurnOnTat()
    {
        myTattoo.triggered = true;
    }

    public void Activate()
    {
        activated = true;
    }


    public void EnableInteract()
    {
        overrideMechLock = true;
    }

    FMOD.Studio.EventInstance snapshot;

    protected virtual bool IsInView()
    {
        pointOnScreen = Camera.main.WorldToScreenPoint(rend.bounds.center);

        //Is in front
        if (pointOnScreen.z < 0)
        {
            return false;
        }

        //Is in FOV
        if (centerFocused)
        {
            int pointsInScreen = 0;
            Vector3 pointA = rend.bounds.min;
            Vector3 pointB = rend.bounds.min + new Vector3(rend.bounds.size.x, 0, 0);
            Vector3 pointC = rend.bounds.min + new Vector3(0, rend.bounds.size.y, 0);
            Vector3 pointD = rend.bounds.min + new Vector3(0, 0, rend.bounds.size.z);
            Vector3 pointE = rend.bounds.max - new Vector3(rend.bounds.size.x, 0, 0);
            Vector3 pointF = rend.bounds.max - new Vector3(0, rend.bounds.size.y, 0);
            Vector3 pointG = rend.bounds.max - new Vector3(0, 0, rend.bounds.size.z);
            Vector3 pointH = rend.bounds.max;

            checkBoundVisible[0] = CheckPointInView(pointA);
            checkBoundVisible[1] = CheckPointInView(pointB);
            checkBoundVisible[2] = CheckPointInView(pointC);
            checkBoundVisible[3] = CheckPointInView(pointD);
            checkBoundVisible[4] = CheckPointInView(pointE);
            checkBoundVisible[5] = CheckPointInView(pointF);
            checkBoundVisible[6] = CheckPointInView(pointG);
            checkBoundVisible[7] = CheckPointInView(pointH);

            for (int i = 0; i < checkBoundVisible.Length; i++)
            {
                if (checkBoundVisible[i])
                    pointsInScreen++;
            }

            if (pointsInScreen < 3)
                return false;

        }
        else
        {
            if ((pointOnScreen.x < Screen.width * 0.2f) || (pointOnScreen.x > Screen.width * 0.8f) ||
               (pointOnScreen.y < Screen.height * 0.2f) || (pointOnScreen.y > Screen.height * 0.8f))
            {
                return false;
            }

        }

        if (!centerFocused)
        {
            if (rend != null)
            {
                RaycastHit hit;
                if (Physics.Linecast(rend.bounds.center, Camera.main.transform.position, out hit, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.name != gameObject.name && !hit.collider.CompareTag("Player"))
                    {
                        return false;
                    }

                }
            }
        }

        return true;
    }

    bool CheckPointInView(Vector3 pointPos)
    {
        Vector3 pointOnScreen = Camera.main.WorldToScreenPoint(pointPos);
        if ((pointOnScreen.x < Screen.width * 0.05f) || (pointOnScreen.x > Screen.width * 0.95f) ||
           (pointOnScreen.y < Screen.height * 0.05f) || (pointOnScreen.y > Screen.height * 0.95f))
        {
            return false;
        }
        return true;
    }
    protected virtual bool IsObjectVisible(Renderer renderer)
    {
        return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), renderer.bounds);
    }
    protected virtual bool isDoorHandleVisible(Collider col)
    {
        return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), col.bounds);
    }

    public virtual void LoadData(GameData data)
    {
        if (data.livableDict.TryGetValue(id, out LivableValues values))
        {
            activated = values.activated;
            if (!values.isActive)
                gameObject.SetActive(false);
            else
                gameObject.SetActive(true);
            if (activated)
            {
                if (mat.HasFloat("_WhiteDegree"))
                    mat.SetFloat("_WhiteDegree", 0);
                firstActivated = true;
            }
            this.enabled = values.isEnabled;
            minDist = values.minDist;
            transform.SetParent(values.parent);
        }
    }
    public virtual void SaveData(ref GameData data)
    {
        if (id == null)
            Debug.LogError(gameObject.name + " ID is null.");
        if (id == "")
            Debug.LogError(gameObject.name + " ID is empty.");
        if (data.livableDict.ContainsKey(id))
            data.livableDict.Remove(id);
        LivableValues values = new LivableValues(activated, gameObject.activeSelf, this.enabled, minDist, transform.parent);
        data.livableDict.Add(id, values);
    }   
}