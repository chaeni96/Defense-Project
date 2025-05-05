using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCamp : MonoBehaviour
{

    [SerializeField] private Slider hpBar;  // Inspector���� �Ҵ�

    [SerializeField] private Canvas hpBarCanvas;  // Inspector���� �Ҵ�

    [SerializeField] private float hpUpdateSpeed = 5f;  // HP Bar ���� �ӵ�

    [SerializeField] private TMP_Text hpText;

    private float targetHPRatio;
    private Coroutine hpUpdateCoroutine;


    public void InitializeObect()
    {
        //hp �ʱ�ȭ
        if (GameManager.Instance != null)
        {
            hpBar.value = GameManager.Instance.GetSystemStat(StatName.CurrentHp) / GameManager.Instance.GetSystemStat(StatName.MaxHP);
            targetHPRatio = hpBar.value;
        }

        UpdateHPText(GameManager.Instance.GetSystemStat(StatName.CurrentHp));


        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;

        GameManager.Instance.OnHPChanged += OnHPChanged;
    }


    private void OnHPChanged(float currentHp)
    {
        targetHPRatio = currentHp / GameManager.Instance.GetSystemStat(StatName.MaxHP);

        hpBar.value = targetHPRatio;


        // HP �ؽ�Ʈ ������Ʈ
        UpdateHPText(currentHp);

        if (GameManager.Instance.GetSystemStat(StatName.CurrentHp) <= 0)
        {
            GameManager.Instance.ChangeState(new GameResultState(GameStateType.Defeat));

        }

        //�ε巴�� ���ӽ�Ű��
        //if (hpUpdateCoroutine != null)
        //{
        //    StopCoroutine(hpUpdateCoroutine);
        //}
        //hpUpdateCoroutine = StartCoroutine(UpdateHPBarSmoothly());
    }

    private void UpdateHPText(float currentHp)
    {
        if (hpText != null)
        {
            float maxHP = GameManager.Instance.GetSystemStat(StatName.MaxHP);

            hpText.text = $"{Mathf.Ceil(currentHp)}/{Mathf.Ceil(maxHP)}";
        }
    }

    private IEnumerator UpdateHPBarSmoothly()
    {
        while (Mathf.Abs(hpBar.value - targetHPRatio) > 0.01f)
        {
            hpBar.value = Mathf.Lerp(hpBar.value, targetHPRatio, Time.deltaTime * hpUpdateSpeed);
            yield return null;
        }
        hpBar.value = targetHPRatio;

        if (GameManager.Instance.GetSystemStat(StatName.CurrentHp) <= 0)
        {
            GameManager.Instance.ChangeState(new GameResultState(GameStateType.Defeat));

        }
    }


    public void CleanUp()
    {
        // �ڷ�ƾ ����
        //if (hpUpdateCoroutine != null)
        //{
        //    StopCoroutine(hpUpdateCoroutine);
        //    hpUpdateCoroutine = null;
        //}

        GameManager.Instance.OnHPChanged -= OnHPChanged;
    }


}
