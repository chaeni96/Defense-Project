using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;


public class UnitController : BasicObject, IPointerClickHandler
{
    [HideInInspector]
    public float attackTimer = 0f;  // Ÿ�̸� �߰�

    [HideInInspector]
    public Vector2 tilePosition;

    [HideInInspector]
    public SkillAttackType attackType;

    public List<GameObject> starObjects = new List<GameObject>();  // ������ ������ �����ϱ� ���� ����Ʈ

    public UnitType unitType;

    public GameObject unitStarObject;

    public SpriteRenderer unitSprite;


    [SerializeField] private SpriteRenderer unitBaseSprite;
    

    //inspector�� �Ҵ�
    [SerializeField] private Material enabledMaterial;   // ��ġ ������ �� ���
    [SerializeField] private Material disabledMaterial; // ��ġ �Ұ����� �� ���
    [SerializeField] private Material originalMaterial; //�⺻ ���׸���

    private int unitSortingOrder;
    private int baseSortingOrder;
    private bool isActive = true;


    public D_UnitData unitData;

    public override void Initialize()
    {
        base.Initialize();

        unitSortingOrder = unitSprite.sortingOrder;
        baseSortingOrder = unitBaseSprite.sortingOrder;

    }

    public void UpdateStarDisplay(int? starLevel = null)
    {
        if (unitStarObject == null) return;

        int currentStarLevel = Mathf.RoundToInt(GetStat(StatName.UnitStarLevel));

        if(starLevel != null)
        {
            currentStarLevel = starLevel.Value;
        }

        foreach (var star in starObjects)
        {
            Destroy(star);
        }
        starObjects.Clear();

        float spacing = 0.4f;
        float totalWidth = (currentStarLevel - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < currentStarLevel; i++)
        {
            GameObject star = Instantiate(unitStarObject, transform);
            star.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
            starObjects.Add(star);
        }
    }

    //���� ���� �ʱ�ȭ
    public void InitializeUnitInfo(D_UnitData unit, Vector2 tilePos)
    {
        if (unit == null) return;

        unitData = unit;
        tilePosition = new Vector2(tilePos.x, tilePos.y);

        attackType = unitData.f_SkillAttackType;
        unitType = unitData.f_UnitType;

        // ���� ���ȵ� �ʱ�ȭ
        baseStats.Clear();
        currentStats.Clear();


        //TODO : enemy�� ����Ҽ������Ƿ� basicObject�� �̵�
        // Subject ���
        // ��� StatSubject�� ���� ���� �������� �� �ջ�
        foreach (var subject in unitData.f_StatSubject)
        {
            var subjectStats = StatManager.Instance.GetAllStatsForSubject(subject);

            foreach (var stat in subjectStats)
            {
                if (!baseStats.ContainsKey(stat.stat))
                {
                    baseStats[stat.stat] = new StatStorage
                    {
                        stat = stat.stat,
                        value = stat.value,
                        multiply = stat.multiply
                    };
                }
                else
                {
                    baseStats[stat.stat].value += stat.value;
                    baseStats[stat.stat].multiply *= stat.multiply;
                }
            }

            AddSubject(subject);
        }


        // ���� ������ �⺻ �������� �ʱ�ȭ
        foreach (var baseStat in baseStats)
        {
            int statValue = baseStat.Value.value;

            // StarLevel�� ���� ���� ����
            if (baseStat.Key != StatName.UnitStarLevel && baseStats.ContainsKey(StatName.UnitStarLevel))
            {
                statValue *= baseStats[StatName.UnitStarLevel].value;
            }

            currentStats[baseStat.Key] = new StatStorage
            {
                stat = baseStat.Value.stat,
                value = statValue,
                multiply = baseStat.Value.multiply
            };
        }

        UpdateStarDisplay();

    }


    //���� ����� �� �ൿ��
    public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        base.OnStatChanged(subject, statChange);
        ApplyEffect();


        //attackSpeed �ٲ�������� attackTimer 0���� �ٽ� ����
        if (statChange.stat == StatName.AttackSpeed)
        {
            attackTimer = 0;
        }
    }

    public void MoveScale()
    {
        // DOPunchScale �� �Ķ����
        // punch:ũ�� ��ȭ
        // duration: ��ü ��� �ð�
        // vibrato: ���� Ƚ��
        // elasticity: ź�� (0~1)

        unitSprite.transform.DOPunchScale(punch: new Vector3(0.4f, 0.4f, 0f), duration: 0.1f, vibrato: 4, elasticity: 0.8f);

    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // Ÿ�̸� ����
        }
    }

    public async void OnPointerClick(PointerEventData eventData)
    {

        if(isActive)
        {
            //���� Ŭ�������� �˾�â ����
            var unitInfo = await UIManager.Instance.ShowUI<UnitSelectFloatingUI>();

            //�˾�â�� Ÿ�� ���� �Ѱ��ֱ�

            unitInfo.InitUnitInfo(this);
        }
     


    }

    public void UpGradeUnitLevel(int value)
    {

        if (!currentStats.ContainsKey(StatName.UnitStarLevel)) return;

            // ���ο� StarLevel ����
            currentStats[StatName.UnitStarLevel].value = value;

        // StarLevel�� ����Ǿ����Ƿ� �ٸ� ��� ���ȵ� ���ο� StarLevel�� ���� ����
        foreach (var stat in currentStats)
        {
            if (stat.Key != StatName.UnitStarLevel)
            {
                // baseStats���� �⺻���� �����ͼ� ���ο� StarLevel�� ����
                stat.Value.value = baseStats[stat.Key].value * value;
            }
        }
   

        UpdateStarDisplay();
    }


    public void SetPreviewMaterial(bool canPlace)
    {
        // ��ġ ������ ���� �Ұ����� ���� ���׸��� ����
        Material targetMaterial = canPlace ? enabledMaterial : disabledMaterial;

        if (unitSprite != null)
        {
            unitSprite.material = targetMaterial;
            unitBaseSprite.material = targetMaterial;

            // �������� ���� Sorting Order�� ���� ����
            unitSprite.sortingOrder = 100;
            unitBaseSprite.sortingOrder = 99;  // base�� �Ѵܰ� �Ʒ���
        }
    }


    // ������ ���� �� ���� ���׸���� ����
    public void DestroyPreviewUnit()
    {

        // ���� �������� ��ȯ �� ���� sorting order�� ����
        unitSprite.sortingOrder = unitSortingOrder;
        unitBaseSprite.sortingOrder = baseSortingOrder;

        unitSprite.material = originalMaterial;
        unitBaseSprite.material = originalMaterial;


    }


    public void ApplyEffect(float duration = 0.5f)
    {
        // ���� ���� ����
        UnityEngine.Color originalColor = unitSprite.color;
        UnityEngine.Color effectColor = UnityEngine.Color.yellow; // ������ �����

        // DOTween���� ���� ����
        unitSprite.DOColor(effectColor, duration * 0.5f)
            .OnComplete(() =>
            {
                unitSprite.DOColor(originalColor, duration * 0.5f);
            });

        unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);

    }

}
