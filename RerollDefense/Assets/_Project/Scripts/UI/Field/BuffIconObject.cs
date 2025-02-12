using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BuffIconObject : MonoBehaviour
{
    [SerializeField] private Sprite damageEnemyBuffImage;
    [SerializeField] private Sprite IncreaseStatBuffImage;
    [SerializeField] private Sprite rangeBuffImage;

    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TMP_Text descriptionText;


    [SerializeField] private Image iconImage;


    private float descriptionDuration = 3f;
    private Coroutine hideDescriptionCoroutine;

    public void Initialize(D_BuffData buffData, string buffDescription)
    {
        // 아이콘 이미지 설정

        switch(buffData.f_buffType)
        {
            case BuffType.Temporal:
                iconImage.sprite = IncreaseStatBuffImage;
                break;

            case BuffType.Instant:
                iconImage.sprite = damageEnemyBuffImage;
                break;

            case BuffType.Range:
                iconImage.sprite = rangeBuffImage;
                break;
        }

        descriptionText.text = buffDescription;
        descriptionPanel.SetActive(false);
    }

    public void OnClickIcon()
    {
        if (descriptionPanel.activeSelf)
            descriptionPanel.SetActive(false);
        else
            ShowDescription();
    }

    private void ShowDescription()
    {
        descriptionPanel.SetActive(true);

        if (hideDescriptionCoroutine != null)
        {
            StopCoroutine(hideDescriptionCoroutine);
        }
        hideDescriptionCoroutine = StartCoroutine(HideDescriptionAfterDelay());
    }

    private IEnumerator HideDescriptionAfterDelay()
    {
        yield return new WaitForSeconds(descriptionDuration);
        descriptionPanel.SetActive(false);
    }
}
