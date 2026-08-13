using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using VInspector;

public class VinylStand : LivableObject
{
    [Foldout("Vinyl Stand")]
    public Vinyl selectedVinyl;
    public Vinyl holdingVinyl;

    List<Transform> holders = new();
    public bool inScrollCD;
    public float scrollCD;
    float scrollCDVal;
    readonly string vinylPlaceSFX = "event:/Sound Effects/ObjectInteraction/Vinyls/Vinyl_Stand";
    bool showingHint;
    int selectIndex;
    protected override void Start()
    {
        base.Start();
        scrollCDVal = scrollCD;
        foreach(Transform child in transform)
            holders.Add(child);
    }

    protected override void Update()
    {
        base.Update();

        if (interactable)
        {
            if (playerLeftHand.isHolding)
            {
                selectedVinyl = null;

                if (playerLeftHand.holdingObj.GetComponent<Vinyl>())
                {
                    holdingVinyl = playerLeftHand.holdingObj.GetComponent<Vinyl>();
                    if(!playerHolding.objectLerping)
                        DetectVinylPlacement();
                }
            }
            else
            {
                if((playerHolding.selectedObj == null || playerHolding.selectedObj.GetComponent<PickUpObject>().selected))
                {
                    if (!selectedVinyl)
                        SelectInitialVinyl();
                    else
                    {
                        if (!inScrollCD)
                        {
                            selectIndex = selectedVinyl.transform.parent.GetSiblingIndex();
                            PlayerChooseVinyl();
                        }

                        selectedVinyl.ShowSongInfo();

                        if (!showingHint)
                        {
                            ReferenceTool.optionHintManager.HintSetUp(OptionHintManager.HintType.VINYLSTAND);
                            showingHint = true;
                        }
                    }
                }

            }

            if (selectedVinyl)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    RuntimeManager.PlayOneShot("event:/Sound Effects/ObjectInteraction/Vinyls/Vinyl_Grab", Camera.main.transform.position);
                    selectedVinyl.activated = true;
                    selectedVinyl.onStand = false;
                    playerHolding.OccupyLeft(selectedVinyl.transform);
                    selectedVinyl.HideSongInfo();
                    selectedVinyl = null;
                    if (showingHint)
                    {
                        ReferenceTool.optionHintManager.HintOff();
                        showingHint = false;
                    }
                }
            }
        }
        else
        {
            if (selectedVinyl)
            {
                selectedVinyl.HideSongInfo();
                selectedVinyl.standSelect = false;
            }
            selectedVinyl = null;
            holdingVinyl = null;
            if (showingHint)
            {
                ReferenceTool.optionHintManager.HintOff();
                showingHint = false;
            }
        }

        if (inScrollCD)
        {
            if(scrollCD > 0)
            {
                scrollCD -= Time.deltaTime;
            }
            else
            {
                inScrollCD = false;
                scrollCD = scrollCDVal;
            }
        }
    }

    void SelectInitialVinyl()
    {
        foreach(Transform t in holders)
        {
            if(t.childCount > 0)
            {
                selectedVinyl = t.GetChild(0).GetComponent<Vinyl>();
                selectedVinyl.standSelect = true;
                return;
            }
        }
    }

    Vinyl FindNextVinyl(int startIndex, bool forward)
    {
        if (forward)
        {
            for (int i = startIndex; i < holders.Count; i++)
            {
                if (holders[i].childCount > 0)
                {
                    return holders[i].GetChild(0).GetComponent<Vinyl>();
                }
            }
            return selectedVinyl;

        }
        else
        {
            for (int i = startIndex; i >= 0; i--)
            {
                if (holders[i].childCount > 0)
                {
                    return holders[i].GetChild(0).GetComponent<Vinyl>();
                }
            }
            return selectedVinyl;
        }

    }

    void PlayerChooseVinyl()
    {
        if (Input.mouseScrollDelta.y > 0)
        {
            inScrollCD = true;
            if (selectIndex < holders.Count - 1)
            {
                selectedVinyl.HideSongInfo();
                selectedVinyl.standSelect = false;
                selectedVinyl = FindNextVinyl(selectIndex +1, true);
                selectedVinyl.standSelect = true;

            }
            else
            {
                selectedVinyl.HideSongInfo();
                selectedVinyl.standSelect = false;
                selectedVinyl = FindNextVinyl(0, true);
                selectedVinyl.standSelect = true;
            }
        }
        if(Input.mouseScrollDelta.y < 0)
        {
            inScrollCD = true;
            if (selectIndex > 0)
            {
                selectedVinyl.HideSongInfo();
                selectedVinyl.standSelect = false;
                selectedVinyl = FindNextVinyl(selectIndex - 1, false);
                selectedVinyl.standSelect = true;
            }
            else
            {
                selectedVinyl.HideSongInfo();
                selectedVinyl.standSelect = false;
                selectedVinyl = FindNextVinyl(holders.Count -1, false);
                selectedVinyl.standSelect = true;
            }
        }
    }

    void DetectVinylPlacement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RuntimeManager.PlayOneShot(vinylPlaceSFX, Camera.main.transform.position);
            holdingVinyl.VinylToStand();
            playerHolding.UnoccupyLeft();
            holdingVinyl = null;
            activated = true;
        }
    }

    protected override void LayerCheck()
    {
        if(interactable && playerLeftHand.isHolding && playerLeftHand.holdingObj.GetComponent<Vinyl>() && !playerHolding.objectLerping)
        {
            gameObject.layer = 9;
        }
        else
        {
            if (activated)
                gameObject.layer = 17;
            else
                gameObject.layer = 0;
        }
    }
}
