using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CostGaugeUI : MonoBehaviour
{
    [Header("Circle UI")]
    [SerializeField] private Image circleProgress;

    [Header("Bar UI")]
    [SerializeField] private RectTransform barContainer;
    [SerializeField] private Image barFillPrefab;
    [SerializeField] private TMP_Text maxCostText;
    [SerializeField] private TMP_Text currentCostText;

    private List<Image> barFills = new List<Image>();
    private int currentFilledIndex = 0;
    private int costPerTick; //한번의 틱마다 증가하는 코스트 양

    private bool canAddCost;

    public void Initialize(int storeLevel)
    {
        CleanUP();

        UpdateText();
        CreateBarFills(storeLevel);

        // 초기 코스트만큼 바로 채우기
        int initialCost = GameManager.Instance.GetSystemStat(StatName.Cost);

        costPerTick = 1;
        currentFilledIndex = initialCost;

        canAddCost = true;

        for (int i = 0; i < currentFilledIndex; i++)
        {
            barFills[i].fillAmount = 1f;
        }
        circleProgress.fillAmount = 0;

        GameManager.Instance.OnCostUsed += UpdateBarFillsOnCostUsed;
        // 와일드카드 선택 시작/종료 이벤트 구독
        StageManager.Instance.OnWaveFinish += StopCostAdd;
        StageManager.Instance.OnWaveStart += StartCostAdd;
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

    private void StopCostAdd()
    {
        canAddCost = false;
        circleProgress.fillAmount = 0f;  // 프로그레스 바도 초기화
    }

    private void StartCostAdd()
    {
        canAddCost = true;
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
        if (canAddCost && GameManager.Instance.GetSystemStat(StatName.Cost) < GameManager.Instance.GetSystemStat(StatName.MaxCost))
        {
            circleProgress.fillAmount += Time.deltaTime / GameManager.Instance.GetSystemStat(StatName.CostChargingSpeed);
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
        StageManager.Instance.OnWaveFinish -= StopCostAdd;
        StageManager.Instance.OnWaveStart -= StartCostAdd;
        ClearBarFills();
    }
}
