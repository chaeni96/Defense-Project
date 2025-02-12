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

        // �̹� �����ϴ� �������̸� ����
        if (activeBuffIcons.ContainsKey(buffId)) return;

        // ���� ������ ����
        GameObject iconObj = Instantiate(buffIconPrefab, buffContainer);
        BuffIconObject buffIcon = iconObj.GetComponent<BuffIconObject>();

        // ���� ������ �ʱ�ȭ
        buffIcon.Initialize(buffData, buffDescription);
        activeBuffIcons.Add(buffId, buffIcon);

        // �Ͻ����� ������ �ڵ� ���� ����
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
