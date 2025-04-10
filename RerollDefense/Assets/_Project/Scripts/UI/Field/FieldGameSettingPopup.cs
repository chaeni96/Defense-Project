using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[UIInfo("FieldGameSettingPopup", "FieldGameSettingPopup", false)]
public class FieldGameSettingPopup : FloatingPopupBase
{
    [SerializeField] private Transform buffContainer;
    [SerializeField] private GameObject buffIconPrefab;
    private Dictionary<int, BuffIconObject> activeBuffIcons = new Dictionary<int, BuffIconObject>();

    public override void InitializeUI()
    {
        base.InitializeUI();

        // UI�� ǥ�õ� �� ���� Ȱ��ȭ�� ��� ������ ������ ǥ��
        RefreshBuffIcons();
    }

    // ��� Ȱ�� ������ �ҷ��� UI�� ǥ���ϴ� �޼���
    private void RefreshBuffIcons()
    {
        // ���� ���� ������ ��� ����
        ClearBuffIcons();

        // BuffManager�κ��� ���� Ȱ��ȭ�� ��� ���� ���� ��������
        Dictionary<StatSubject, List<BuffTimeBase>> allActiveBuffs = BuffManager.Instance.GetAllActiveBuffs();

        // �� ��� Ȱ��ȭ�� �������� ��ȸ�ϸ� ������ ����
        foreach (var kvp in allActiveBuffs)
        {
            StatSubject subject = kvp.Key;
            List<BuffTimeBase> buffs = kvp.Value;

            foreach (var buff in buffs)
            {
                int buffId = buff.GetBuffUID();

                // BuffManager���� ���� ���� ��������
                string description = BuffManager.Instance.GetBuffDescription(buffId);

                // ���� ������ �߰�
                AddBuffIcon(buff, description);
            }
        }
    }

    // ���� ������ �߰� �޼���
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
    }

    // ���� ���� ������ ��� ����
    private void ClearBuffIcons()
    {
        foreach (var icon in activeBuffIcons.Values)
        {
            Destroy(icon.gameObject);
        }
        activeBuffIcons.Clear();
    }


    public override void HideUI()
    {
        base.HideUI();
    }


    // ���� ����ϱ� ��ư Ŭ�� �ڵ鷯
    public void OnClickContinueGameButton()
    {
        // ���� �Ͻ����� ���¿��� ���� ���·� ����
        if (GameManager.Instance.currentState is GamePauseState pauseState)
        {
            pauseState.ResumeGame();
        }
    }

    // �κ�� ���ư��� ��ư Ŭ�� �ڵ鷯
    public void OnClickReturnToLobbyButton()
    {
        if (GameManager.Instance.currentState is GamePauseState pauseState)
        {
            pauseState.ReturnToLobby();
        }
    }

}
