using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CostGaugeUI : MonoBehaviour
{
    [Header("Circle UI")]
    [SerializeField] private Image circleProgress;

    [Header("Bar UI")]
    [SerializeField] private RectTransform barContainer;
    [SerializeField] private Image barFillPrefab;
    [SerializeField] private TMP_Text maxCostText;
    //[SerializeField] private TMP_Text currentCostText;

    private List<Image> barFills = new List<Image>();
    private int currentFilledIndex = 0;
    private int costPerTick; //한번의 틱마다 증가하는 코스트 양
    private Color originFillColor;
    private int maxCost;

    private bool canAddCost;

    //살아있는 트윈
    private Dictionary<Image, Tween> activeTweens = new Dictionary<Image, Tween>();

    public void Initialize(int storeLevel)
    {
        CleanUP();

        UpdateText();
        CreateBarFills(storeLevel);

        // 초기 코스트만큼 바로 채우기
        int initialCost = GameManager.Instance.GetSystemStat(StatName.Cost);

        costPerTick = 1;
        currentFilledIndex = initialCost;
        maxCost = GameManager.Instance.GetSystemStat(StatName.MaxCost);

        canAddCost = true;

        originFillColor = barFillPrefab.color;

        for (int i = 0; i < maxCost; i++)
        {
            if(i < currentFilledIndex)
            {
                barFills[i].color = originFillColor;
            }
            else
            {
                barFills[i].color = Color.clear;
            }
        }
        circleProgress.fillAmount = 0;

        GameManager.Instance.OnCostUsed += UpdateBarFillsOnCostUsed;
        // 와일드카드 선택 시작/종료 이벤트 구독
        StageManager.Instance.OnWaveFinish += StopCostAdd;
        StageManager.Instance.OnWaveStart += StartCostAdd;
    }

    public void UpdateText()
    {

        maxCost = GameManager.Instance.GetSystemStat(StatName.MaxCost);
        int currentCost = GameManager.Instance.GetSystemStat(StatName.Cost);
        maxCostText.text = $"{currentCost.ToString()}/{maxCost.ToString()}";
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
        if (canAddCost && currentFilledIndex < maxCost)
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
        for (int i = 0; i < barFills.Count; i++)
        {
            if (i < currentFilledIndex)
            {
                if (barFills[i].color == Color.clear) // 처음 등장할때 반짝스
                {
                    AnimateFillAppearance(barFills[i]);
                }
            }
            else
            {
                barFills[i].color = Color.clear;
            }
        }
        UpdateText();
    }

    private void AnimateFillAppearance(Image fill)
    {
        if (activeTweens.TryGetValue(fill, out Tween existingTween))
        {
            existingTween.Kill(); // 진행 중이던 트윈을 종료
            activeTweens.Remove(fill);
        }

        fill.color = Color.white; // 처음에는 완전 흰색
        Tween tween = fill.DOColor(originFillColor, 0.14f).SetEase(Ease.OutQuad);
        activeTweens[fill] = tween;
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
            fill.gameObject.SetActive(true);
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
                    if (activeTweens.TryGetValue(barFills[currentFilledIndex], out Tween existingTween))
                    {
                        existingTween.Kill(); // 진행 중이던 트윈을 중단
                        activeTweens.Remove(barFills[currentFilledIndex]);
                    }

                    barFills[currentFilledIndex].color = Color.clear;
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
