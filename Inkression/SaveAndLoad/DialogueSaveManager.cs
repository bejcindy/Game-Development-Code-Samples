using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class DialogueSaveManager : MonoBehaviour, ISaveSystem
{
    public List<DialogueSaver> triggerDialogues;

    public static Dictionary<DialogueSaver, DialogueData> dialogueData = new();

    private void Awake()
    {
        triggerDialogues = FindObjectsOfType(typeof(DialogueSaver), true).OfType<DialogueSaver>().ToList();
        foreach(DialogueSaver dialogue in triggerDialogues)
        {
            dialogueData.Add(dialogue, new DialogueData(dialogue.gameObject.activeSelf, dialogue.colliderEnabled));
        }
    }

    public virtual void SaveData(ref GameData data)
    {
        foreach (DialogueSaver dialogue in triggerDialogues)
        {
            string convoID = dialogue.GetComponent<ObjectID>().id;

            if (String.IsNullOrEmpty(convoID))
                Debug.LogError(dialogue.gameObject.name + " ID is null.");

            if (data.convoDict.ContainsKey(convoID))
                data.convoDict.Remove(convoID);
            data.convoDict.Add(convoID, dialogueData[dialogue]);
        }
    }

    public virtual void LoadData(GameData data)
    {
        foreach (DialogueSaver dialogue in triggerDialogues)
        {
            string convoID = dialogue.GetComponent<ObjectID>().id;

            if (data.convoDict.TryGetValue(convoID, out DialogueData values))
            {
                dialogue.colliderEnabled = values.isColliderActive;
                if (!values.isColliderActive)
                    dialogue.gameObject.SetActive(false);
                else
                {
                    if (values.isActive)
                        dialogue.gameObject.SetActive(true);
                    else
                        dialogue.gameObject.SetActive(false);
                }

            }
        }

    }
}
