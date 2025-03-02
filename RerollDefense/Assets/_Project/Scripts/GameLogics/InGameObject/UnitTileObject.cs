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
                // base ������ ��� ������� ǥ��
                unitImage.sprite = null; // �Ǵ� �⺻ ��������Ʈ ����
                unitImage.color = Color.white;
            }
            else
            {
                // �� Ÿ���� ��� �����ϰ�
                unitImage.color = new Color(0, 0, 0, 0);
            }
        }

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
