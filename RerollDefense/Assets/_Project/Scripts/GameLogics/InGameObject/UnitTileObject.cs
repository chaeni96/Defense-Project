using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UnitTileObject : MonoBehaviour
{
    public Image backgroundImage;


    public void InitUnitImage(Sprite sprite, bool isBase = false)
    {
      
        // ��� �̹��� Ȱ��ȭ/��Ȱ��ȭ (sprite�� �ְų� isBase�� true�� ��)
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
