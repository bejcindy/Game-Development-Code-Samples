using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VinylHolder : MonoBehaviour, ISaveSystem
{
    public Vinyl myVinyl;
    public bool hasVinyl;
    string id;

    void Awake()
    {
        if (GetComponent<ObjectID>())
            id = GetComponent<ObjectID>().id;
        else
            Debug.LogError(gameObject.name + " doesn't have ObjectID Component.");
    }


    // Update is called once per frame
    void Update()
    {
        hasVinyl = transform.childCount > 0;
    }


    public void LoadData(GameData data)
    {
        if (data.vinylHolderDict.TryGetValue(id, out GameObject savedVinyl))
        {
            if (savedVinyl != null)
            {
                myVinyl = savedVinyl.GetComponent<Vinyl>();
                myVinyl.GetComponent<Rigidbody>().isKinematic = false;
            }
        }
    }

    public void SaveData(ref GameData data)
    {
        if (id == null)
            Debug.LogError(gameObject.name + " ID is null.");
        if (id == "")
            Debug.LogError(gameObject.name + " ID is empty.");
        if (data.vinylHolderDict.ContainsKey(id))
            data.vinylHolderDict.Remove(id);
        if (myVinyl)
            data.vinylHolderDict.Add(id, myVinyl.gameObject);
        else
            data.vinylHolderDict.Add(id, null);
    }
}
