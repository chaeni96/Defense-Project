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
            previewImage.raycastTarget = false;  // UI �̺�Ʈ ����
        }
    }
    public void SetPreviewState(bool isInteractable)
    {
        if (previewImage != null)
        {
            if (isInteractable)
            {
                // Ȱ��ȭ ���� (������ ����)
                previewImage.color = Color.white;
            }
            else
            {
                // ��Ȱ��ȭ ���� (ȸ�� + ����)
                previewImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
    }
}
