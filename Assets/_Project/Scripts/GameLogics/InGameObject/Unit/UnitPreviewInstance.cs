using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UnitPreviewInstance : MonoBehaviour
{
    public Image previewImage;

    public void Initialize(Sprite unitSprite)
    {
        if (previewImage != null)
        {
            previewImage.sprite = unitSprite;
            previewImage.raycastTarget = false;  // UI 이벤트 방지
        }
    }
    public void SetPreviewState(bool isInteractable)
    {
        if (previewImage != null)
        {
            if (isInteractable)
            {
                // 활성화 상태 (완전한 색상)
                previewImage.color = Color.white;
            }
            else
            {
                // 비활성화 상태 (회색 + 투명)
                previewImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
    }
}
