using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildCardManager : MonoBehaviour
{
    private static WildCardManager _instance;

    public static WildCardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WildCardManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("WildCardManager");
                    _instance = singleton.AddComponent<WildCardManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 와일드 카드내의 모든 효과 적용
  
    // 스탯 부스터 적용
    public void ApplyWildCardEffect(D_WildCardData cardData)
    {
        if (cardData.f_BuffData == null) return;

        foreach (var buffData in cardData.f_BuffData)
        {
            var targets = GetTargetsForBuff(buffData);
            foreach (var target in targets)
            {
                ApplyBuff(buffData, target);
            }
        }
    }


    public void ApplyBuff(D_BuffData buffData, BasicObject target)
    {
        BuffTimeBase buff;

        switch (buffData.f_buffType)
        {
            case BuffType.Temporary:
                buff = new TemporaryBuff();
                break;
            case BuffType.Permanent:
                buff = new PermanentBuff();
                break;
            case BuffType.AreaBased:
                buff = new AreaBuff();
                break;
            default:
                return;
        }

        buff.Initialize(target, buffData);
        buff.StartBuff();
    }

    private List<BasicObject> GetTargetsForBuff(D_BuffData buffData)
    {
        // 버프 대상 찾기 로직
        var targets = new List<BasicObject>();
        // 예: 모든 아군 유닛, 특정 타입의 유닛 등

        //유닛이나 버프매니저에서 찾아와야됨
        return targets;
    }

    public void CleanUp()
    {
        // 필요한 정리 작업
    }


}
