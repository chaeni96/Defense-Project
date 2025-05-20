using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UnitTileObject : MonoBehaviour
{
    public Image backgroundImage;


    public void InitTileImage(bool isShow = false)
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = isShow;
        }
    }

    public void SetPosition(float x, float y)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(x, y);
        }
    }
}
