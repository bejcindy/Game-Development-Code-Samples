using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ObjectID : MonoBehaviour
{
    public string id;

    [ContextMenu("Generate ID")]
    public void GenerateGuid()
    {        
        if (id == "")
            id = System.Guid.NewGuid().ToString();        
    }
}
