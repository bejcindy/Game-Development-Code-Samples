using DG.Tweening;
using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VInspector;
using TMPro;

public class Vinyl : PickUpObject
{
    [Foldout("Vinyl")]
    public RecordPlayer recordPlayer;
    public Transform myHolder;
    public LocationController tattooZone;
    public bool onRecordPlayer;
    public bool onStand;
    public bool standSelect;

    bool listened;
    bool listneCountAdded;
    bool loyiAtVinyl;
    bool hadDialogue;
    float playTime;

    FirstPersonOcclusion mySong;
    GameObject songInfo;
    GameObject vinylDialogue;
    Collider coll;

    public string songName, artistName;

    protected override void Awake()
    {
        base.Awake();
        mySong = transform.GetChild(0).GetComponent<FirstPersonOcclusion>();
        songInfo = transform.GetChild(1).gameObject;
        songInfo.SetActive(false);
        vinylDialogue = transform.GetChild(2).gameObject;
        targetRot = new Vector3(0, -90, 60);
        coll = GetComponent<Collider>();
        objType = HandObjectType.DOUBLE;
        songName = songInfo.transform.GetChild(0).GetComponent<TMP_Text>().text;
        artistName = songInfo.transform.GetChild(1).GetComponent<TMP_Text>().text;
    }


    protected override void Start()
    {
        base.Start();
        recordPlayer = ReferenceTool.recordPlayer;
    }


    protected override void Update()
    {
        loyiAtVinyl = DialogueLua.GetVariable("Vinyl/LoyiInBF").asBool;
        if (!hadDialogue)
        {
            if (loyiAtVinyl && tattooZone.inZone)
            {
                if (playTime > 5f && !MindPalace.tatMenuOn)
                {
                    vinylDialogue.SetActive(true);
                    hadDialogue = true;
                }
            }
        }

        if (listened && !listneCountAdded)
        {
            listneCountAdded = true;
            recordPlayer.vinylListenCount++;
        }


        base.Update();
        noPickUp = onStand || (onRecordPlayer && (recordPlayer.isPlaying || recordPlayer.moving));

        if (inHand)
        {
            if (recordPlayer.readyPlacing && !playerHolding.objectLerping)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    VinylToTurntable();
                }
            }
        }


        if (onRecordPlayer)
        {
            if (recordPlayer.isPlaying)
            {
                playTime += Time.deltaTime;

                if (playTime > 5f)
                    listened = true;

                transform.Rotate(Vector3.up * 50 * Time.deltaTime, Space.Self);
            }
        }

    }

    public override void OnGrabObject()
    {
        if (recordPlayer.currentRecord == this)
            recordPlayer.currentRecord = null;
        coll.enabled = false;
        inHand = true;
        onStand = false;
        standSelect = false;
        onRecordPlayer = false;
        activated = true;
        transform.DOScale(2, 0.5f);
    }

    public void VinylToTurntable()
    {
        coll.enabled = true;
        inHand = false;
        onStand = false;
        thrown = false;
        playerLeftHand.RemoveObjectInHand();
        onRecordPlayer = true;
        rb.isKinematic = true;
        recordPlayer.PlaceRecord(this);
        playerLeftHand.ResetHand();
    }

    public void VinylToStand()
    {
        thrown = false;
        inHand = false;
        rb.isKinematic = true;
        noPickUp = true;
        playerLeftHand.ResetHand();
        transform.parent = myHolder;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        onStand = true;
        coll.enabled = true;
    }

    public void PlaySong()
    {
        if (!mySong.gameObject.activeSelf)
            mySong.gameObject.SetActive(true);
        else
            mySong.TogglePlayAndStop(true);
    }

    public void StopSong()
    {
        mySong.TogglePlayAndStop(false);
        playTime = 0;
    }

    public void ShowSongInfo()
    {
        songInfo.SetActive(true);
    }

    public void HideSongInfo()
    {
        songInfo.SetActive(false);
    }

    protected override void LayerCheck()
    {
        if ((standSelect || selected) && !recordPlayer.moving)
            gameObject.layer = 9;
        else if (inHand)
            gameObject.layer = 16;
        else
        {
            if (activated)
                gameObject.layer = 17;
            else
                gameObject.layer = 0;
        }
    }

    public override void LoadData(GameData data)
    {
        base.LoadData(data);
        if (data.pickupDict.TryGetValue(id, out PickUpValues puValues))
        {
            if (puValues.pos != Vector3.zero)
                transform.position = puValues.pos;
        }
        if (data.vinylDict.TryGetValue(id, out bool savedHadDialogue))
        {
            hadDialogue = savedHadDialogue;
        }
    }

    public override void SaveData(ref GameData data)
    {
        if (!onRecordPlayer && !onStand)
        {
            base.SaveData(ref data);
        }
        else
        {
            if (id == null)
                Debug.LogError(gameObject.name + " ID is null.");
            if (id == "")
                Debug.LogError(gameObject.name + " ID is empty.");

            if (data.livableDict.ContainsKey(id))
                data.livableDict.Remove(id);
            LivableValues values = new LivableValues(activated, gameObject.activeSelf, this.enabled, minDist, transform.parent);
            data.livableDict.Add(id, values);

            if (data.vinylDict.ContainsKey(id))
                data.vinylDict.Remove(id);
            data.vinylDict.Add(id, hadDialogue);

            if (data.pickupDict.ContainsKey(id))
                data.pickupDict.Remove(id);
            data.pickupDict.Add(id, null);
        }
    }
}
