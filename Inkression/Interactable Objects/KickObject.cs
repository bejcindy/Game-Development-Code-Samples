using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using VInspector;

public class KickObject : MonoBehaviour
{
    [Foldout("Kicking")]
    public float kickForce;
    Rigidbody rb;
    Transform player;
    public LivableObject activateTrigger;
    public RiggedVisibleDetector detector;
    public GameObject kickHint;
    public GameObject instantiatedHint;
    public Vector3 hintPosition;

    bool isVisible, interactable;
    Collider coll;
    bool hinted;
    bool activated, firstActivated;
    GameObject usefulChild;
    public EventReference kickedSound;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = ReferenceTool.player;
        coll = GetComponent<Collider>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.GetComponent<GroupMaster>())
                usefulChild = child.gameObject;
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (isColliderVisible(coll))
        {
            isVisible = IsInView();
        }
        else
            isVisible = false;


        if (GameProgress.mechUnlocked)
            DetectInteractable();

        if (interactable)
        {

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Destroy(instantiatedHint);
                activated = true;
                //transform.GetChild(0).gameObject.GetComponent<MeshCollider>().enabled = false;
                if(transform.GetChild(1).gameObject.GetComponent<Collider>())
                    transform.GetChild(1).gameObject.GetComponent<Collider>().enabled = false;

                coll.enabled = true;
                rb.isKinematic = false;
                rb.AddForce(Camera.main.transform.forward * kickForce, ForceMode.Impulse);
                if (!kickedSound.IsNull)
                    RuntimeManager.PlayOneShot(kickedSound, transform.position);
            }
        }

        if (activated)
        {
            if(usefulChild != null)
                usefulChild.GetComponent<GroupMaster>().activateAll = true;

        }

    }

    void DetectInteractable()
    {
        if (Vector3.Distance(transform.position, player.position) <= 3)
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
    bool IsInView()
    {
        Vector3 pointOnScreen = Camera.main.WorldToScreenPoint(coll.bounds.center);


        //Is in front
        if (pointOnScreen.z < 0)
        {
            return false;
        }


        if ((pointOnScreen.x < Screen.width * 0.2f) || (pointOnScreen.x > Screen.width * 0.8f) ||
           (pointOnScreen.y < Screen.height * 0.2f) || (pointOnScreen.y > Screen.height * 0.8f))
        {
            return false;
        }

        if (GetComponent<Renderer>())
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position - new Vector3(0, 0.1f, 0), GetComponent<Renderer>().bounds.center - (Camera.main.transform.position - new Vector3(0, 0.1f, 0)), out hit, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                //if (hit.collider)
                //    Debug.Log(gameObject.name+" raycast hit this: "+hit.collider.gameObject.name);
                if (hit.collider.name != gameObject.name && !hit.collider.CompareTag("Player"))
                {

                    return false;
                }

            }
        }


        return true;
    }

    bool isColliderVisible(Collider col)
    {
        return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), col.bounds);
    }
}
