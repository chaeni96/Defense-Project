using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
    public string tileUniqueID;

    public SkillAttackType attackType;
    public SpriteRenderer unitSprite;

    [SerializeField] private SpriteRenderer unitBaseSprite;

   
    private bool isActive = true;
    private D_TileShpeData tileData;

    public override void Initialize()
    {
        base.Initialize();

    }

    //같은 tileshpae을 가진 유닛이 여러개일수있으므로 생성시 고유 ID 부여
    public void RegistereTileID(string uniqueID, D_TileShpeData tile)
    {
        tileUniqueID = uniqueID;
        tileData = tile;
    }

    public void ShowPreviewUnit()
    {
        UnityEngine.Color color;

        if (unitSprite != null)
        {
            color = unitSprite.color;
            color.a = 0.5f; // 50% 투명
            unitSprite.color = color;
        }
        
        if(unitBaseSprite != null)
        {
            color = unitBaseSprite.color;
            color.a = 0.5f;
            unitBaseSprite.color = color;

        }
        
    }

    //tileShpaeName은 이제 상점 통해서 랜덤으로 가져오는것
    public void InitializeUnitStat(D_unitBuildData buildData)
    {
        if (buildData == null) return;

        var unitData = buildData.f_unitData;
        attackType = unitData.f_SkillAttackType;
        if (unitData != null)
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
        //유닛 클릭했을때 팝업창 띄우기
         FieldUnitInfoUI unitInfo = UIManager.Instance.ShowUI<FieldUnitInfoUI>("FieldUnitInfoUI");

        //팝업창에 타일 정보 넘겨주기

        unitInfo.InitUnitInfo(this);


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
