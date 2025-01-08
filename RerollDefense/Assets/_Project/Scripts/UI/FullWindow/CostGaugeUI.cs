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
        int initialCost = GameManager.Instance.CurrentCost;


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

        int maxCost = GameManager.Instance.MaxCost;
        maxCostText.text = $"Max : {maxCost.ToString()}";
        int currentCost = GameManager.Instance.CurrentCost;
        currentCostText.text = currentCost.ToString();

    }
    private void UpdateBarFillsOnCostUsed(int usedCost)
    {
        for (int i = 0; i < usedCost; i++)
        {
            if (currentFilledIndex > 0)
            {
                currentFilledIndex--;
                barFills[currentFilledIndex].fillAmount = 0f;
            }
        }

        UpdateText();
    }


    private void ClearBarFills()
    {
        foreach (var fill in barFills)
        {
            if (fill != null) Destroy(fill.gameObject);
        }
        barFills.Clear();
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

    private void Update()
    {
        if (currentFilledIndex != GameManager.Instance.CurrentCost)
        {
            currentFilledIndex = GameManager.Instance.CurrentCost;
            for (int i = 0; i < barFills.Count; i++)
            {
                barFills[i].fillAmount = i < currentFilledIndex ? 1f : 0f;
            }
        }

        if (currentFilledIndex < barFills.Count)
        {
            circleProgress.fillAmount += Time.deltaTime / manaGenerateTime;
            if (circleProgress.fillAmount >= 1f)
            {
                circleProgress.fillAmount = 0;
                GameManager.Instance.AddCost();
                UpdateText();
            }
        }
    }


    public void CleanUP()
    {
        // 이벤트 구독 해제
        GameManager.Instance.OnCostUsed -= UpdateBarFillsOnCostUsed;
        ClearBarFills();
    }
}
