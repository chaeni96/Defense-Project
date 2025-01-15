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

    //���� ���� �ʱ�ȭ
    public void InitializeUnitInfo(D_UnitData unit, Vector2 tilePos)
    {
        if (unit == null) return;

        unitData = unit;
        tilePosition = new Vector2(tilePos.x, tilePos.y);

        attackType = unitData.f_SkillAttackType;
        unitType = unitData.f_UnitType;


        stats.Clear();
        stats = new Dictionary<StatName, float>();

        foreach (var statData in unitData.f_UnitsStat)
        {
            stats[statData.f_StatName] = statData.f_StatValue;
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


    public void CleanUp()
    {
        if (stats != null)
        {
            stats.Clear();
        }
        stats = null;
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


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // ���� ���� �ð�ȭ
        Gizmos.color = new UnityEngine.Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, stats[StatName.AttackRange]);
    }
#endif
}
