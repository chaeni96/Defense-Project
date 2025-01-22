using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CostGaugeUI : MonoBehaviour
{
    [Header("Circle UI")]
    [SerializeField] private Image circleProgress;
    [SerializeField] private float manaGenerateTime = 3f;

    [Header("Bar UI")]
    [SerializeField] private RectTransform barContainer;
    [SerializeField] private Image barFillPrefab;
    [SerializeField] private TMP_Text maxCostText;
    [SerializeField] private TMP_Text currentCostText;

    private List<Image> barFills = new List<Image>();
    private int currentFilledIndex = 0;
    private int costPerTick; //한번의 틱마다 증가하는 코스트 양
    private void Start()
    {
        // 게임 매니저의 OnCostUsed 이벤트에 구독
        GameManager.Instance.OnCostUsed += UpdateBarFillsOnCostUsed;
    }

    public void Initialize(int storeLevel)
    {
        CleanUP();

        UpdateText();
        CreateBarFills(storeLevel);

        // 초기 코스트만큼 바로 채우기
        int initialCost = GameManager.Instance.GetSystemStat(StatName.Cost);

        costPerTick = 1;
        currentFilledIndex = initialCost;

        for (int i = 0; i < currentFilledIndex; i++)
        {
            barFills[i].fillAmount = 1f;
        }
        circleProgress.fillAmount = 0;

        GameManager.Instance.OnCostUsed += UpdateBarFillsOnCostUsed;

    }

    public void UpdateText()
    {

        int maxCost = GameManager.Instance.GetSystemStat(StatName.MaxCost);
        maxCostText.text = $"Max : {maxCost.ToString()}";
        int currentCost = GameManager.Instance.GetSystemStat(StatName.Cost);
        currentCostText.text = currentCost.ToString();

    }

    private void ClearBarFills()
    {
        foreach (var fill in barFills)
        {
            if (fill != null) Destroy(fill.gameObject);
        }
        barFills.Clear();
    }


    private void Update()
    {
        // 현재 인덱스와 실제 코스트 값 동기화
        if (currentFilledIndex != GameManager.Instance.GetSystemStat(StatName.Cost))
        {
            currentFilledIndex = GameManager.Instance.GetSystemStat(StatName.Cost);
            UpdateBars();
        }

        // MaxCost보다 작을 때만 자동 증가 처리
        if (GameManager.Instance.GetSystemStat(StatName.Cost) < GameManager.Instance.GetSystemStat(StatName.MaxCost))
        {
            circleProgress.fillAmount += Time.deltaTime / manaGenerateTime;
            if (circleProgress.fillAmount >= 1f)
            {
                circleProgress.fillAmount = 0;


                // costPerTick 값을 바로 넘겨 변경사항 브로드캐스트
                StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
                {
                    statName = StatName.Cost,
                    value = costPerTick, // 직접 변경값 전달
                    multiply = 1f
                });

                UpdateText();
            }
        }
        else
        {
            // MaxCost 이상일 때는 circle을 꽉 찬 상태로 유지
            circleProgress.fillAmount = 1f;
        }
    }

    private void UpdateBars()
    {
        // 현재 코스트에 따라 바 업데이트
        for (int i = 0; i < barFills.Count; i++)
        {
            barFills[i].fillAmount = i < currentFilledIndex ? 1f : 0f;
        }
        UpdateText();
    }

    private void CreateBarFills(int storeLevel)
    {
        int segmentCount = Mathf.Clamp(9 + storeLevel, 10, 20);
        float segmentWidth = barContainer.rect.width / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            Image fill = Instantiate(barFillPrefab, barContainer);
            RectTransform fillRect = fill.rectTransform;

            fillRect.sizeDelta = new Vector2(segmentWidth - 2f, barContainer.rect.height);
            fill.fillAmount = 0;
            barFills.Add(fill);
        }
    }
    private void UpdateBarFillsOnCostUsed(int usedCost)
    {

        currentFilledIndex = Mathf.Min(currentFilledIndex, barFills.Count);

        // 현재 코스트 인덱스에서 사용한 만큼 감소
        for (int i = 0; i < usedCost; i++)
        {
            if (currentFilledIndex > 0)
            {
                currentFilledIndex--;
                if (currentFilledIndex < barFills.Count)  
                {
                    barFills[currentFilledIndex].fillAmount = 0f;
                }
            }
        }

        // 코스트를 사용하면 서클 게이지를 초기화
        circleProgress.fillAmount = 0f;
        UpdateText();
    }


    public void CleanUP()
    {
        // 이벤트 구독 해제
        GameManager.Instance.OnCostUsed -= UpdateBarFillsOnCostUsed;
        ClearBarFills();
    }
}
