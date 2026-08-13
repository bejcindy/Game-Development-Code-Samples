using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class ObjectHintPanelManager : MonoBehaviour
{
    CanvasGroup canvasGroup;
    PlayerHolding playerHolding;
    PlayerLeftHand playerLeftHand;
    float fadeSpeed = 8f;

    private void Start()
    {
        playerHolding = ReferenceTool.playerHolding;
        playerLeftHand = ReferenceTool.playerLeftHand;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        if (MindPalace.tatMenuOn)
        {
            if (canvasGroup.alpha > 0)
                canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            else
                canvasGroup.alpha = 0;
        }
        else if (playerHolding.inDialogue && !FoodManager.inDiningSession)
        {
            if (canvasGroup.alpha > 0)
                canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            else
                canvasGroup.alpha = 0;
        }
        else
        {
            if (canvasGroup.alpha < 1)
                canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            else
                canvasGroup.alpha = 1;
        }


    }
}
