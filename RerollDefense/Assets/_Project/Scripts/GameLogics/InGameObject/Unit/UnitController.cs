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
    //BgDatabase에서 읽어오기
    [HideInInspector]
    public int attack;
    [HideInInspector]
    public int attackSpeed;
    [HideInInspector]
    public int attackRange;
    [HideInInspector]
    public int unitBlockSize;
    [HideInInspector]
    public float attackCoolDown;

    [HideInInspector]
    public float attackTimer = 0f;  // 타이머 추가

    [HideInInspector]
    public Vector2 tilePosition;

    [HideInInspector]
    public SkillAttackType attackType;


    public UpgradeUnitType upgradeUnitType;

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

    public void InitializeTilePos(Vector2 tilePos)
    {
        tilePosition = new Vector2(tilePos.x, tilePos.y);
    }

    public Vector2 GetTilePosition()
    {
        return tilePosition;
    }
    public void InitializeUnitData(D_UnitData unit)
    {
        if (unit == null) return;

        unitData = unit;

        attackType = unitData.f_SkillAttackType;
        upgradeUnitType = unitData.f_UpgradeUnitType;

        if (unitData != null || attackType != SkillAttackType.None)
        {
            // statDatas를 순회하면서 스탯 설정
            foreach (var statData in unitData.f_statDatas)
            {
                switch (statData.f_stat.f_name)
                {
                    case "Attack":
                        this.attack = statData.f_value;
                        break;
                    case "AttackSpeed":
                        this.attackSpeed = statData.f_value;
                        break;
                    case "AttackRange":
                        this.attackRange = statData.f_value;
                        break;
                    case "UnitBlockSize":
                        this.unitBlockSize = statData.f_value;
                        break;
                    case "AttackCoolTime":
                        this.attackCoolDown = statData.f_value;
                        break;
                }
            }
        }
        else
        {
            Debug.LogError("UnitData 없음");
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

    //스탯 가져오기
    public Dictionary<string, int> GetStats()
    {
        return new Dictionary<string, int>
    {
        { "Attack", attack },
        { "AttackSpeed", attackSpeed },
        { "AttackRange", attackRange },
        { "UnitBlockSize", unitBlockSize }
    };
    }

    public void UnitMerge(UnitController otherUnit)
    {
        // 같은 타입의 유닛만 합성 가능
        if (this.upgradeUnitType != otherUnit.upgradeUnitType)
        {
            Debug.LogWarning("Different unit types cannot be merged.");
            return;
        }

        // 스탯 합성 로직
        // 예를 들어, 같은 타입의 유닛들의 스탯을 평균내거나 누적할 수 있음
        foreach (var statData in otherUnit.unitData.f_statDatas)
        {
            var currentStat = this.unitData.f_statDatas.Find(s => s.f_stat.f_name == statData.f_stat.f_name);

            if (currentStat != null)
            {
                // 예시: 스탯을 누적
                currentStat.f_value += statData.f_value;
            }
            else
            {
                // 새로운 스탯 추가
                this.unitData.f_statDatas.Add(statData);
            }
        }

        // 유닛 다시 초기화
        InitializeUnitData(this.unitData);

        // 시각적 효과 (예: 펀치 스케일)
        MoveScale();
    }

    public bool IsActive() => isActive;

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // 타이머 리셋
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {

        if(isActive)
        {
            //유닛 클릭했을때 팝업창 띄우기
            UnitSelectFloatingUI unitInfo = UIManager.Instance.ShowUI<UnitSelectFloatingUI>("UnitSelectFloatingUI");

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



#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 시각화
        Gizmos.color = new UnityEngine.Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
