using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using FMOD;
using FMODUnity;
using VInspector;
using DG.Tweening;
public class LookingObject : LivableObject
{
    PlayerMovement pm;
    private bool chainActivated;
    private DialogueSystemTrigger dialogue;
    private GameObject[] sameTypePosters;
    string lookSound = "event:/Sound Effects/Poster_Look";

    [Foldout("Looking")]
    public bool focusingThis;
    public bool backLookDir;
    public bool upLookDir;
    public Transform lookGroup;

    protected override void Start()
    {
        base.Start();
        pm = ReferenceTool.playerMovement;
        if (TryGetComponent<DialogueSystemTrigger>(out DialogueSystemTrigger dia))
        {
            dialogue = dia;
        }
        sameTypePosters = GameObject.FindGameObjectsWithTag(gameObject.tag);
        singleCheck = true;
        myHint = ObjectHintManager.HintType.LOOK;
        hintFollow = transform;
        hintTextOffset = -300f;
        mat.SetFloat("_WhiteDegree", 1);
    }

    protected override void Update()
    {
        
        base.Update();

        if (processable && !activated)
        {
            ShowDot();
            ShowIcon();
            HintCheck();

            if (interactable)
            {
                if (!chainActivated)
                    playerHolding.AddLookable(gameObject);
                else
                {
                    playerHolding.RemoveLookable(gameObject);
                    selected = false;
                }

                if (selected)
                {
                    if (playerHolding.inDialogue || playerHolding.selectedNPC)
                    {
                        selected = false;
                        playerHolding.focusedObj = null;
                    }
                    if (Input.GetKeyDown(KeyCode.Space) && !focusingThis && !chainActivated)
                    {
                        FocusOnObject();
                    }
                }

            }
            else
            {
                playerHolding.RemoveLookable(gameObject);
                selected = false;
            }
        }
        else
        {
            if (hint)
            {
                objectHintManager.Disappear();
                hint = null;
                objectHintManager = null;
            }
            playerHolding.RemoveLookable(gameObject);
            selected = false;
        }

        if (chainActivated && interactable && !playerHolding.inDialogue)
        {
            activated = true;
        }
    }

    public virtual void FocusOnObject()
    {
        if (gameObject.tag.Contains("Poster"))
            RuntimeManager.PlayOneShot(lookSound, transform.position);

        RuntimeManager.PlayOneShot("event:/Sound Effects/Focus", transform.position);

        playerHolding.RemoveLookable(gameObject);
        playerHolding.focusedObj = null;
        selected = false;

        activated = true;
        focusingThis = true;
        DataHolder.currentFocus = this;
        DataHolder.LookingFocus();

        DataHolder.focusCinemachine.LookAt = transform;
        playerCam.LookAt = transform;
        DataHolder.pov.m_HorizontalAxis.m_MaxSpeed = 0f;
        DataHolder.pov.m_VerticalAxis.m_MaxSpeed = 0f;
        DataHolder.focusCinemachine.m_Priority = playerCam.m_Priority + 1;
        playerHolding.looking = true;
        pm.enabled = false;

    }


    protected override void ActivationCheck()
    {
        if (!firstActivated)
        {
            TurnOnColor(mat);

            if (hasGroupControl)
            {
                groupControl.activateAll = true;
            }
        }

    }

    protected override void LayerCheck()
    {
        if (focusingThis)
        {
            gameObject.layer = 13;
            if(lookGroup!= null)
            {
                foreach(Renderer rend in lookGroup.GetComponentsInChildren<Renderer>())
                    rend.gameObject.layer = 13;
            }
        }
        else if (selected)
        {
            gameObject.layer = 9;
        }
        else if (activated)
        {
            gameObject.layer = 17;
            if (lookGroup != null)
            {
                foreach (Renderer rend in lookGroup.GetComponentsInChildren<Renderer>())
                    rend.gameObject.layer = 17;
            }
        }

        else
        {
            gameObject.layer = 0;
            if (lookGroup != null)
            {
                foreach (Renderer rend in lookGroup.GetComponentsInChildren<Renderer>())
                    rend.gameObject.layer = 0;
            }
        }
    }

    protected override void ColorChangeComplete()
    {
        if (myTattoo && !myTattoo.triggered)
        {
            Invoke(nameof(TurnOnTat), 2f);
        }

        if (OnActivateEvent != null)
        {
            OnActivateEvent.Invoke();
        }
        if (hasGroupControl)
        {
            groupControl.activateAll = true;
        }
        if (dialogue != null && !chainActivated)
            dialogue.enabled = true;

        if (sameTypePosters.Length > 0)
        {
            ActivateAll();
        }
    }

    protected override void HintCheck()
    {
        if (interactable || showIcon || showDot)
        {
            if (!hint)
            {
                GenerateHint();
            }
            else
            {
                if (selected)
                {
                    objectHintManager.ShowHint();
                    objectHintManager.DotToIcon();
                }
                else
                {
                    objectHintManager.HideHint();
                    objectHintManager.IconToDot();
                }
            }
        }
        else if (hint && !showDot)
        {
            TurnOffHint();
        }
    }

    float allowedAngle = .5f;
    protected override bool IsObjectVisible(Renderer renderer)
    {
        if (GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), renderer.bounds))
        {
            if (Vector3.Distance(transform.position, Camera.main.transform.position) <= minDist * 2)
            {
                //get direction
                if (Vector3.Dot(backLookDir ? -transform.forward : upLookDir ? transform.up : transform.forward, Vector3.Normalize(Camera.main.transform.position - transform.position)) > allowedAngle)
                {
                    RaycastHit hit;
                    if (Physics.Linecast(renderer.bounds.center, Camera.main.transform.position, out hit, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider.name != gameObject.name && !hit.collider.CompareTag("Player"))
                            return false;
                        else
                            return true;
                    }
                    return true;
                }
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
        else
            return false;
    }

    protected override void DetectInteractable()
    {
        if (Vector3.Distance(transform.position, Camera.main.transform.position) <= minDist)
        {
            Vector3 toPlayer = Vector3.Normalize(Camera.main.transform.position - transform.position);

            interactable = Vector3.Dot(backLookDir ? -transform.forward : upLookDir ? transform.up : transform.forward, toPlayer) > 0.6f;
            //if (!backLookDir)
            //    interactable = Vector3.Dot(transform.forward, toPlayer) > 0.6f;
            //else
            //    interactable = Vector3.Dot(-transform.forward, toPlayer) > 0.6f;
        }
        else
        {
            interactable = false;
        }
    }


    void OnConversationStart(Transform other)
    {
        playerHolding.inDialogue = true;
    }

    void OnConversationEnd(Transform other)
    {
        playerHolding.inDialogue = false;
    }

    void ActivateAll()
    {
        foreach (GameObject obj in sameTypePosters)
        {
            if (obj.GetComponent<LookingObject>())
            {
                LookingObject looking = obj.GetComponent<LookingObject>();
                looking.chainActivated = true;
            }

        }
    }

    public override void LoadData(GameData data)
    {
        base.LoadData(data);
        if (activated)
        {
            if (data.lookingDict.TryGetValue(id, out bool savedPosterLinkAct))
                chainActivated = savedPosterLinkAct;
        }
    }
    public override void SaveData(ref GameData data)
    {
        base.SaveData(ref data);
        if (data.lookingDict.ContainsKey(id))
            data.lookingDict.Remove(id);
        data.lookingDict.Add(id, chainActivated);
    }
}
