using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueSaver : MonoBehaviour
{
    public bool colliderEnabled = true;
    public Collider coll;
    void Awake()
    {
        coll = GetComponent<Collider>();
        coll.enabled = colliderEnabled;
    }

    private void Update()
    {
        DialogueSaveManager.dialogueData[this].isColliderActive = coll.enabled;

    }
    public void OnEnable()
    {
        if (DialogueSaveManager.dialogueData.ContainsKey(this))
        {
            DialogueSaveManager.dialogueData[this].isActive = true;
        }
        coll.enabled = colliderEnabled;
    }

    public void OnDisable()
    {
        DialogueSaveManager.dialogueData[this].isActive = false;
    }

}

[System.Serializable]
public class DialogueData
{
    public bool isActive;
    public bool isColliderActive;

    public DialogueData(bool _isActive, bool _isColliderActive)
    {
        isActive = _isActive;
        isColliderActive = _isColliderActive;
    }
}
