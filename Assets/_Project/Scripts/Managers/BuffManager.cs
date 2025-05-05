using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager _instance;

    private BuffIconFloatingUI buffIconUI;

    public static BuffManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuffManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("BuffManager");
                    _instance = singleton.AddComponent<BuffManager>();
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
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

    }

    //현재 적용된 버프들 -> 나중에 특정 subject 모든 버프 제거하거나, 특정 ID 버프 제거등 사용하기 위함
    private Dictionary<StatSubject, List<BuffTimeBase>> activeBuffs = new Dictionary<StatSubject, List<BuffTimeBase>>();
    // 버프 ID와 설명 매핑
    private Dictionary<int, string> buffDescriptions = new Dictionary<int, string>();


    //WildCard나 아이템 등에서 버프 적용하는 메서드 -> 호출하면 버프 적용
    public void ApplyBuff(D_BuffData buffData, StatSubject subject, string description = "")
    {
        //버프 타입에 맞는 버프 객체 생성
        var buff = CreateBuff(buffData.f_buffType);
        if (buff == null) return;

        //특정 대상(statSubject)에 대한 버프리스트 초기화, 해당 대상에 대한 버프리스트 존재하지않으면 만들어서 추가
        if (!activeBuffs.ContainsKey(subject))
        {
            activeBuffs[subject] = new List<BuffTimeBase>();
        }

        //버프 초기화
        buff.Initialize(buffData);
        //버프 시작
        buff.StartBuff(subject);
        activeBuffs[subject].Add(buff);

        // 설명 저장 (버프 ID와 연결)
        int buffId = buff.GetBuffUID();
        buffDescriptions[buffId] = description;

    }

    // 버프 설명 가져오기
    public string GetBuffDescription(int buffId)
    {
        if (buffDescriptions.TryGetValue(buffId, out string description))
        {
            return description;
        }
        return "";
    }

    // 버프 프리뷰 표시
    public async void ShowBuffIcon(D_WildCardData cardData)
    {
        if (cardData.f_BuffData == null || cardData.f_BuffData.Count == 0) return;

        if (buffIconUI == null)
        {
            buffIconUI = await UIManager.Instance.ShowUI<BuffIconFloatingUI>();
        }

        // 모든 버프효과를 대표하는 하나의 프리뷰 버프 생성
        // 첫 번째 버프데이터의 타입을 사용하거나, 별도의 대표 아이콘을 사용할 수 있음
        var buffIcon = CreateBuff(cardData.f_BuffData[0].f_buffType);
        if (buffIcon != null)
        {
            buffIcon.Initialize(cardData.f_BuffData[0]);  // 기본 정보 초기화
            buffIconUI.AddBuffIcon(buffIcon, cardData.f_Description);  // 하나의 통합된 아이콘 표시
        }
    }

    private BuffTimeBase CreateBuff(BuffType type)
    {
        switch (type)
        {
            case BuffType.Temporal:
                return new TemporalBuff();
            case BuffType.Instant:
                return new InstantBuff();
            case BuffType.Range:
                return new RangeBuff();
            default:
                return null;
        }
    }

    public Dictionary<StatSubject, List<BuffTimeBase>> GetAllActiveBuffs()
    {
        // 현재 활성화된 모든 버프 정보를 복사해서 반환
        Dictionary<StatSubject, List<BuffTimeBase>> buffsCopy = new Dictionary<StatSubject, List<BuffTimeBase>>();

        foreach (var kvp in activeBuffs)
        {
            buffsCopy[kvp.Key] = new List<BuffTimeBase>(kvp.Value);
        }

        return buffsCopy;
    }

    //버프 삭제 관련 메서드들
    public void RemoveAllBuffsFromSubject(StatSubject subject)
    {
        if (activeBuffs.TryGetValue(subject, out var buffs))
        {
            // 게임 상태 급히 변경될때(로비로 이동)나 비동기 작업할때 순회중에 수정이 발생해서 버그남 -> 버프 리스트의 복사본을 만들어 순회
            var buffsCopy = buffs.ToList();
            foreach (var buff in buffsCopy)
            {
                RemoveBuff(buff, subject);
            }
            activeBuffs.Remove(subject);
        }
    }

    private void RemoveBuff(BuffTimeBase buff, StatSubject subject)
    {
        buff.RemoveEffects(subject);

        activeBuffs[subject].Remove(buff);
    }

    public void RemoveBuffById(int buffId, StatSubject subject)
    {
        if (activeBuffs.TryGetValue(subject, out var buffs))
        {
            var buff = buffs.FirstOrDefault(b => b.GetBuffUID() == buffId);
            if (buff != null)
            {
                RemoveBuff(buff, subject);
            }
        }
    }

    public void CleanUp()
    {
        foreach (var subject in activeBuffs.Keys.ToList())
        {
            RemoveAllBuffsFromSubject(subject);
        }
        activeBuffs.Clear();
    }

}
