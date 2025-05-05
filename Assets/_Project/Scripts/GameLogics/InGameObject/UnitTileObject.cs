using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UnitTileObject : MonoBehaviour
{
    public Image backgroundImage;


    public void InitUnitImage(Sprite sprite, bool isBase = false)
    {
      
        // 배경 이미지 활성화/비활성화 (sprite가 있거나 isBase가 true일 때)
        if (backgroundImage != null)
        {
            backgroundImage.enabled = (sprite != null || isBase);
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
