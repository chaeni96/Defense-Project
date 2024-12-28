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
        UpdateText();
        ClearBarFills();
        CreateBarFills(storeLevel);
        currentFilledIndex = 0;
        circleProgress.fillAmount = 0;
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
        if (currentFilledIndex < barFills.Count)
        {
            circleProgress.fillAmount += Time.deltaTime / manaGenerateTime;

            if (circleProgress.fillAmount >= 1f)
            {
                circleProgress.fillAmount = 0;
                barFills[currentFilledIndex].fillAmount = 1;
                currentFilledIndex++;
                GameManager.Instance.AddMana();
                UpdateText();
            }        
        }
    }


    private void OnDestroy()
    {
        // 이벤트 구독 해제
        GameManager.Instance.OnCostUsed -= UpdateBarFillsOnCostUsed;
        ClearBarFills();
    }
}
