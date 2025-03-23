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
    private int costPerTick; //�ѹ��� ƽ���� �����ϴ� �ڽ�Ʈ ��
    private Color originFillColor;
    private int maxCost;

    private bool canAddCost;
    private List<Image> preparingFills = new List<Image>();

    private bool isPreparingUse = false;
    private int currentPrepareCardCost;

    //����ִ� Ʈ��
    private Dictionary<Image, Tween> activeTweens = new Dictionary<Image, Tween>();

    public void Initialize(int storeLevel)
    {
        CleanUP();

        UpdateText();
        CreateBarFills(storeLevel);

        // �ʱ� �ڽ�Ʈ��ŭ �ٷ� ä���
        int initialCost = GameManager.Instance.GetSystemStat(StatName.Cost);
        currentPrepareCardCost = 0;
        costPerTick = 1;
        currentFilledIndex = initialCost;
        maxCost = GameManager.Instance.GetSystemStat(StatName.MaxCost);

        canAddCost = true;

        originFillColor = barFillPrefab.color;

        for (int i = 0; i < maxCost; i++)
        {
            barFills[i].color = (i < currentFilledIndex) ? originFillColor : Color.clear;
        }
        circleProgress.fillAmount = 0;

        GameManager.Instance.OnCostUsed += UpdateBarFillsOnCostUsed;
        GameManager.Instance.OnCostUsePrePare += PrepareUse;
        GameManager.Instance.OnCostUsePrePareCancle += CancelUse;
        // ���ϵ�ī�� ���� ����/���� �̺�Ʈ ����
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
        if (canAddCost && currentFilledIndex < maxCost)
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
        for (int i = 0; i < barFills.Count; i++)
        {
            if (i < currentFilledIndex)
            {
                if (barFills[i].color == Color.clear) // ó�� �����Ҷ� ��¦��
                {
                    AnimateFillAppearance(barFills[i]);
                }
            }
            else
            {
                //AnimateFillDisappear(barFills[i]);
                barFills[i].color = Color.clear;
            }
        }
        if (isPreparingUse)
        {
            PrepareUse(currentPrepareCardCost);
        }
        UpdateText();
    }
    private void AnimateFillDisappear(Image fill)
    {
        if (activeTweens.TryGetValue(fill, out Tween existingTween))
        {
            existingTween.Kill(); // ���� Ʈ�� �ߴ�
            activeTweens.Remove(fill);
        }
        fill.color = originFillColor;

        Sequence disappearSequence = DOTween.Sequence();
        disappearSequence.Append(fill.DOFade(0, 0.3f)) // ���� ��������
                         .Join(fill.rectTransform.DOScale(Vector3.one * 1.5f, 0.3f)) // Ŀ��
                         .OnComplete(() =>
                         {
                             fill.color = Color.clear;
                             fill.rectTransform.localScale = Vector3.one; // ũ�� ������� ����
                     });

        activeTweens[fill] = disappearSequence;
    }
    private void AnimateFillAppearance(Image fill)
    {
        if (activeTweens.TryGetValue(fill, out Tween existingTween))
        {
            existingTween.Kill(); // ���� ���̴� Ʈ���� ����
            activeTweens.Remove(fill);
        }


        fill.color = Color.white; // ó������ ���� ���
        Tween tween = fill.DOColor(originFillColor, 0.3f).SetEase(Ease.OutQuad);
        activeTweens[fill] = tween;
    }

    public void PrepareUse(int amount)
    {
        CancelUse();
        currentPrepareCardCost = amount;
        isPreparingUse = true;
        preparingFills.Clear();

        int startIndex = Mathf.Max(0, currentFilledIndex - amount);
        for (int i = startIndex; i < currentFilledIndex; i++)
        {
            if (barFills[i] != null)
            {
                preparingFills.Add(barFills[i]);
                StartBlinkAnimation(barFills[i]);
            }
        }
    }
    private void StartBlinkAnimation(Image fill)
    {
        if (activeTweens.TryGetValue(fill, out Tween existingTween))
        {
            existingTween.Kill();
            activeTweens.Remove(fill);
        }

        fill.color = originFillColor;
        Tween blinkTween = fill.DOColor(Color.gray, 0.2f)
    .SetEase(Ease.InOutSine);

        /*Tween blinkTween = fill.DOFade(0.2f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);*/
       /* Tween blinkTween = fill.DOColor(originFillColor, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);*/

        activeTweens[fill] = blinkTween;
    }
    //����Ϸ��ٰ� ���� Ȥ�� ��� ����
    public void CancelUse()
    {
        isPreparingUse = false;
        currentPrepareCardCost = 0;

        for (int i = 0; i < preparingFills.Count; i++)
        {
            Image fill = preparingFills[i];

            if (activeTweens.TryGetValue(fill, out Tween existingTween))
            {
                existingTween.Kill();
                activeTweens.Remove(fill);
            }

            // �̹� ���� fill�� ���� ������ �ǵ����� ����
            int fillIndex = barFills.IndexOf(fill);
            if (fillIndex < currentFilledIndex) // ������ ���� fill�� ���� ������ ����
            {
                fill.DOColor(originFillColor, 0.2f).SetEase(Ease.OutQuad);
            }
        }

        preparingFills.Clear();
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

        // ���� �ڽ�Ʈ �ε������� ����� ��ŭ ����
        for (int i = 0; i < usedCost; i++)
        {
            if (currentFilledIndex > 0)
            {
                currentFilledIndex--;
                if (currentFilledIndex < barFills.Count)  
                {
                    if (activeTweens.TryGetValue(barFills[currentFilledIndex], out Tween existingTween))
                    {
                        existingTween.Kill(); // ���� ���̴� Ʈ���� �ߴ�
                        activeTweens.Remove(barFills[currentFilledIndex]);
                    }

                    //AnimateFillDisappear(barFills[currentFilledIndex]);

                    barFills[currentFilledIndex].color = Color.clear;
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
        StageManager.Instance.OnWaveFinish -= StopCostAdd;
        StageManager.Instance.OnWaveStart -= StartCostAdd;
        ClearBarFills();
    }
}
