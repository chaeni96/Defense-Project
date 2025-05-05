using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[UIInfo("BuffIconFloatingUI", "BuffIconFloatingUI", false)]
public class BuffIconFloatingUI : FloatingPopupBase
{
    
    [SerializeField] private Transform buffContainer;
    [SerializeField] private GameObject buffIconPrefab;

    private Dictionary<int, BuffIconObject> activeBuffIcons = new Dictionary<int, BuffIconObject>();


    public override void InitializeUI()
    {
        base.InitializeUI();
    }


    public void AddBuffIcon(BuffTimeBase buff, string buffDescription)
    {
        var buffData = buff.GetBuffData();
        int buffId = buff.GetBuffUID();

        // 이미 존재하는 아이콘이면 리턴
        if (activeBuffIcons.ContainsKey(buffId)) return;

        // 버프 아이콘 생성
        GameObject iconObj = Instantiate(buffIconPrefab, buffContainer);
        BuffIconObject buffIcon = iconObj.GetComponent<BuffIconObject>();

        // 버프 아이콘 초기화
        buffIcon.Initialize(buffData, buffDescription);
        activeBuffIcons.Add(buffId, buffIcon);

        // 일시적인 버프면 자동 제거 설정
        //if (buffData.f_buffType == BuffType.Temporal)
        //{
        //    StartCoroutine(RemoveTemporalBuffIcon(buffId, buffData.f_duration));
        //}
    }

    private IEnumerator RemoveTemporalBuffIcon(int buffId, float duration)
    {
        yield return new WaitForSeconds(duration);
        RemoveBuffIcon(buffId);
    }

    public void RemoveBuffIcon(int buffId)
    {
        if (activeBuffIcons.TryGetValue(buffId, out BuffIconObject icon))
        {
            Destroy(icon.gameObject);
            activeBuffIcons.Remove(buffId);
        }
    }

    public override void HideUI()
    {
        foreach (var icon in activeBuffIcons.Values)
        {
            Destroy(icon.gameObject);
        }
        activeBuffIcons.Clear();
        base.HideUI();
    }
}
