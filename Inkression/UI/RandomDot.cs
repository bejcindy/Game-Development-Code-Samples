using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomDot : MonoBehaviour
{
    public Sprite[] dotSprites;

    private void Awake()
    {
        int dotIndex = Random.Range(0, dotSprites.Length);
        GetComponent<Image>().sprite = dotSprites[dotIndex];
    }
}
