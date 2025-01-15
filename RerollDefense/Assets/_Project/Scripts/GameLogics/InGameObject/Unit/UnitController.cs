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
    public float attackTimer = 0f;  // 타이머 추가

    [HideInInspector]
    public Vector2 tilePosition;

    [HideInInspector]
    public SkillAttackType attackType;


    public UnitType unitType;

    public GameObject unitStarObject;

    public SpriteRenderer unitSprite;


    [SerializeField] private SpriteRenderer unitBaseSprite;
    

    //inspector에 할당
    [SerializeField] private Material enabledMaterial;   // 배치 가능할 때 사용
    [SerializeField] private Material disabledMaterial; // 배치 불가능할 때 사용
    [SerializeField] private Material originalMaterial; //기본 머테리얼

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

    //유닛 정보 초기화
    public void InitializeUnitInfo(D_UnitData unit, Vector2 tilePos)
    {
        if (unit == null) return;

        unitData = unit;
        tilePosition = new Vector2(tilePos.x, tilePos.y);

        attackType = unitData.f_SkillAttackType;
        unitType = unitData.f_UnitType;

        //TODO : enemy도 사용할수있으므로 basicObject로 이동
        // Subject 등록
        // 모든 StatSubject에 대해 스탯 가져오기 및 합산
        Dictionary<StatName, StatStorage> mergedStats = new Dictionary<StatName, StatStorage>();

        foreach (var subject in unitData.f_StatSubject)
        {
            // StatManager에서 Subject의 스탯 가져오기
            var subjectStats = StatManager.Instance.GetAllStatsForSubject(subject);

            foreach (var stat in subjectStats)
            {
                if (!mergedStats.ContainsKey(stat.stat))
                {
                    // 새로운 스탯 추가
                    mergedStats[stat.stat] = new StatStorage
                    {
                        stat = stat.stat,
                        value = stat.value,
                        multiply = stat.multiply
                    };
                }
                else
                {
                    // 기존 스탯과 합산
                    mergedStats[stat.stat].value += stat.value;
                    mergedStats[stat.stat].multiply *= stat.multiply;
                }
            }

            // Subject 구독
            AddSubject(subject);
        }

        // 합산 결과를 baseStats에 반영
        baseStats = mergedStats;

        // 현재 스탯을 기본 스탯으로 초기화
        foreach (var baseStat in baseStats)
        {
            currentStats[baseStat.Key] = new StatStorage
            {
                stat = baseStat.Value.stat,
                value = baseStat.Value.value,
                multiply = baseStat.Value.multiply
            };
        }
    }


//스탯 변경시 할 행동들
public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        base.OnStatChanged(subject, statChange);
        ApplyEffect();


        //attackSpeed 바뀌었을때는 attackTimer 0부터 다시 시작
        if(statChange.stat ==  StatName.AttackSpeed)
        {
            attackTimer = 0;
        }
    }

    public void MoveScale()
    {
        // DOPunchScale 상세 파라미터
        // punch:크기 변화
        // duration: 전체 재생 시간
        // vibrato: 진동 횟수
        // elasticity: 탄성 (0~1)

        unitSprite.transform.DOPunchScale(punch: new Vector3(0.4f, 0.4f, 0f), duration: 0.1f, vibrato: 4, elasticity: 0.8f);


       

    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // 타이머 리셋
        }
    }

    public async void OnPointerClick(PointerEventData eventData)
    {

        if(isActive)
        {
            //유닛 클릭했을때 팝업창 띄우기
            var unitInfo = await UIManager.Instance.ShowUI<UnitSelectFloatingUI>();

            //팝업창에 타일 정보 넘겨주기

            unitInfo.InitUnitInfo(this);
        }
     


    }

    public void SetPreviewMaterial(bool canPlace)
    {
        // 배치 가능할 때와 불가능할 때의 머테리얼 설정
        Material targetMaterial = canPlace ? enabledMaterial : disabledMaterial;

        if (unitSprite != null)
        {
            unitSprite.material = targetMaterial;
            unitBaseSprite.material = targetMaterial;

            // 프리뷰일 때는 Sorting Order를 높게 설정
            unitSprite.sortingOrder = 100;
            unitBaseSprite.sortingOrder = 99;  // base는 한단계 아래로
        }
    }


    // 프리뷰 종료 시 원본 머테리얼로 복원
    public void DestroyPreviewUnit()
    {

        // 실제 유닛으로 전환 시 원래 sorting order로 복구
        unitSprite.sortingOrder = unitSortingOrder;
        unitBaseSprite.sortingOrder = baseSortingOrder;

        unitSprite.material = originalMaterial;
        unitBaseSprite.material = originalMaterial;


    }


    public void ApplyEffect(float duration = 0.5f)
    {
        // 원래 색상 저장
        UnityEngine.Color originalColor = unitSprite.color;
        UnityEngine.Color effectColor = UnityEngine.Color.yellow; // 고정된 노란색

        // DOTween으로 색상 변경
        unitSprite.DOColor(effectColor, duration * 0.5f)
            .OnComplete(() =>
            {
                unitSprite.DOColor(originalColor, duration * 0.5f);
            });

        unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);

    }

}
