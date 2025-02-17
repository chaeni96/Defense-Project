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
    private int costPerTick; //�ѹ��� ƽ���� �����ϴ� �ڽ�Ʈ ��

    private bool canAddCost;

    public void Initialize(int storeLevel)
    {
        CleanUP();

        UpdateText();
        CreateBarFills(storeLevel);

        // �ʱ� �ڽ�Ʈ��ŭ �ٷ� ä���
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
        // ���ϵ�ī�� ���� ����/���� �̺�Ʈ ����
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
        circleProgress.fillAmount = 0f;  // ���α׷��� �ٵ� �ʱ�ȭ
    }

    private void StartCostAdd()
    {
        canAddCost = true;
    }


    private void Update()
    {
        // ���� �ε����� ���� �ڽ�Ʈ �� ����ȭ
        if (currentFilledIndex != GameManager.Instance.GetSystemStat(StatName.Cost))
        {
            currentFilledIndex = GameManager.Instance.GetSystemStat(StatName.Cost);
            UpdateBars();
        }

        // MaxCost���� ���� ���� �ڵ� ���� ó��
        if (canAddCost && GameManager.Instance.GetSystemStat(StatName.Cost) < GameManager.Instance.GetSystemStat(StatName.MaxCost))
        {
            circleProgress.fillAmount += Time.deltaTime / GameManager.Instance.GetSystemStat(StatName.CostChargingSpeed);
            if (circleProgress.fillAmount >= 1f)
            {
                circleProgress.fillAmount = 0;


                // costPerTick ���� �ٷ� �Ѱ� ������� ��ε�ĳ��Ʈ
                StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
                {
                    statName = StatName.Cost,
                    value = costPerTick, // ���� ���氪 ����
                    multiply = 1f
                });

                UpdateText();
            }
        }
        else
        {
            // MaxCost �̻��� ���� circle�� �� �� ���·� ����
            circleProgress.fillAmount = 1f;
        }
    }

    private void UpdateBars()
    {
        // ���� �ڽ�Ʈ�� ���� �� ������Ʈ
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

        // ���� �ڽ�Ʈ �ε������� ����� ��ŭ ����
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

        // �ڽ�Ʈ�� ����ϸ� ��Ŭ �������� �ʱ�ȭ
        circleProgress.fillAmount = 0f;
        UpdateText();
    }


    public void CleanUP()
    {
        // �̺�Ʈ ���� ����
        GameManager.Instance.OnCostUsed -= UpdateBarFillsOnCostUsed;
        StageManager.Instance.OnWaveFinish -= StopCostAdd;
        StageManager.Instance.OnWaveStart -= StartCostAdd;
        ClearBarFills();
    }
}
