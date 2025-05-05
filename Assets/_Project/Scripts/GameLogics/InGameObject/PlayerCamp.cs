using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCamp : MonoBehaviour
{

    [SerializeField] private Slider hpBar;  // Inspector에서 할당

    [SerializeField] private Canvas hpBarCanvas;  // Inspector에서 할당

    [SerializeField] private float hpUpdateSpeed = 5f;  // HP Bar 감소 속도

    [SerializeField] private TMP_Text hpText;

    private float targetHPRatio;
    private Coroutine hpUpdateCoroutine;


    public void InitializeObect()
    {
        //hp 초기화
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


        // HP 텍스트 업데이트
        UpdateHPText(currentHp);

        if (GameManager.Instance.GetSystemStat(StatName.CurrentHp) <= 0)
        {
            GameManager.Instance.ChangeState(new GameResultState(GameStateType.Defeat));

        }

        //부드럽게 감속시키기
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
        // 코루틴 정리
        //if (hpUpdateCoroutine != null)
        //{
        //    StopCoroutine(hpUpdateCoroutine);
        //    hpUpdateCoroutine = null;
        //}

        GameManager.Instance.OnHPChanged -= OnHPChanged;
    }


}
