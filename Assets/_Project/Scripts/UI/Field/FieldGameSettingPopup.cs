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

        // UI가 표시될 때 현재 활성화된 모든 버프를 가져와 표시
        RefreshBuffIcons();
    }

    // 모든 활성 버프를 불러와 UI에 표시하는 메서드
    private void RefreshBuffIcons()
    {
        // 기존 버프 아이콘 모두 제거
        ClearBuffIcons();

        // BuffManager로부터 현재 활성화된 모든 버프 정보 가져오기
        Dictionary<StatSubject, List<BuffTimeBase>> allActiveBuffs = BuffManager.Instance.GetAllActiveBuffs();

        // 각 대상별 활성화된 버프들을 순회하며 아이콘 생성
        foreach (var kvp in allActiveBuffs)
        {
            StatSubject subject = kvp.Key;
            List<BuffTimeBase> buffs = kvp.Value;

            foreach (var buff in buffs)
            {
                int buffId = buff.GetBuffUID();

                // BuffManager에서 버프 설명 가져오기
                string description = BuffManager.Instance.GetBuffDescription(buffId);

                // 버프 아이콘 추가
                AddBuffIcon(buff, description);
            }
        }
    }

    // 버프 아이콘 추가 메서드
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
    }

    // 기존 버프 아이콘 모두 제거
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


    // 게임 계속하기 버튼 클릭 핸들러
    public void OnClickContinueGameButton()
    {
        // 현재 일시정지 상태에서 이전 상태로 복귀
        if (GameManager.Instance.currentState is GamePauseState pauseState)
        {
            pauseState.ResumeGame();
        }
    }

    // 로비로 돌아가기 버튼 클릭 핸들러
    public void OnClickReturnToLobbyButton()
    {
        if (GameManager.Instance.currentState is GamePauseState pauseState)
        {
            pauseState.ReturnToLobby();
        }
    }

}
