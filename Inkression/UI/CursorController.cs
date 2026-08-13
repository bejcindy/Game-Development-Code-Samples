using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CursorController : MonoBehaviour
{
    RectTransform rect;
    Canvas canvas;
    GameObject cursorObj;
    public static bool hoveringGrabObj, grabbiingObj, hoveringTattooSprite;
    public Sprite regular, openHand, closeHand, tattooHover;
    Image cursorImg;

    // Start is called before the first frame update
    void Start()
    {
        //transform.SetAsLastSibling();
        rect = GetComponent<RectTransform>();
        canvas = transform.parent.GetComponent<Canvas>();
        cursorObj = transform.GetChild(0).gameObject;
        cursorImg = cursorObj.GetComponent<Image>();
        hoveringGrabObj = false;
        grabbiingObj = false;
    }

    // Update is called once per frame
    void Update()
    {

        if (Cursor.lockState == CursorLockMode.None)
        {
            if (SceneManager.GetActiveScene().name == "Prologue")
            {
                if (PrologueProgression.inTattoo && !PauseMenu.isPaused)
                    cursorObj.SetActive(false);
                else
                {
                    transform.SetAsLastSibling();
                    rect.anchoredPosition = Input.mousePosition / canvas.scaleFactor;
                    cursorObj.SetActive(true);
                }
            }
            else
            {
                transform.SetAsLastSibling();
                rect.anchoredPosition = Input.mousePosition / canvas.scaleFactor;
                cursorObj.SetActive(true);
            }
        }
        else
        {
            cursorObj.SetActive(false);
        }

        if (cursorObj.activeSelf)
        {
            if (grabbiingObj)
                cursorImg.sprite = closeHand;
            else if (hoveringGrabObj)
                cursorImg.sprite = openHand;
            else if (hoveringTattooSprite)
                cursorImg.sprite = tattooHover;
            else
                cursorImg.sprite = regular;
        }
    }
}
