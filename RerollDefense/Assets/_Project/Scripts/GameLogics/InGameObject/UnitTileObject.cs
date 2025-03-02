using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UnitTileObject : MonoBehaviour
{
    public Image unitImage;
    [SerializeField] private Image backgroundImage;


    public void InitUnitImage(Sprite sprite, bool isBase = false)
    {
        if (unitImage != null)
        {
            if (sprite != null)
            {
                unitImage.sprite = sprite;
                unitImage.color = Color.white;
            }
            else if (isBase)
            {
                // base 유닛인 경우 흰색으로 표시
                unitImage.sprite = null; // 또는 기본 스프라이트 설정
                unitImage.color = Color.white;
            }
            else
            {
                // 빈 타일인 경우 투명하게
                unitImage.color = new Color(0, 0, 0, 0);
            }
        }

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
