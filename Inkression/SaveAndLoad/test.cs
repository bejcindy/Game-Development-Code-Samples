using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour,ISaveSystem
{
    public void LoadData(GameData data)
    {
        transform.position = data.playerPosition;
    }
    public void SaveData(ref GameData data)
    {
        
    }
}
