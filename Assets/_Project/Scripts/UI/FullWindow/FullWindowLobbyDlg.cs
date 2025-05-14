using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("FullWindowLobbyDlg", "FullWindowLobbyDlg", true)]
public class FullWindowLobbyDlg : FullWindowBase
{

    //화면 패널
    [SerializeField] private EpisodeInfoUI episodeUI;
    
    [SerializeField] private Button inventoryButton;

    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Ease transitionEase = Ease.OutQuad;

    // 현재 활성화된 패널과 버튼 추적
    private GameObject currentPanel;
    private Button currentButton;
    private List<GameObject> allPanels;
    private List<Button> allButtons;

    private bool initialized = false;

    public override void InitializeUI()
    {
        base.InitializeUI();

        OpenBaseUI();
    }

    private void OpenBaseUI()
    {
        InitializeEpisodeUI();
        
        initialized = true;
    }

    private void InitializeEpisodeUI()
    {
        var userData = D_LocalUserData.GetEntity(0);
        int userBestRecordEpisode = userData.f_clearEpisodeNumber;

        D_EpisodeData episodeData = null;
        List<D_StageData> episodeStageDataList = null;
        
        // 유저가 진행해야할 에피소드 반환
        episodeData = userBestRecordEpisode == 0
            ? D_EpisodeData.FindEntity(e => e.f_episodeNumber == 1)
            : D_EpisodeData.FindEntity(e => e.f_episodeNumber == userBestRecordEpisode + 1);
        
        episodeStageDataList = D_StageData.GetEntitiesByKeyEpisodeKey(episodeData);
        
        if (episodeStageDataList is { Count: > 0 })
        {
            // 에피소드 UI 초기화
            bool canPlay = episodeData.f_episodeNumber == 1 || userData.f_clearEpisodeNumber >= episodeData.f_episodeNumber;
            
            episodeUI.InitializeEpisodeInfo(new EpisodeInfoParam(episodeData, userData.f_lastClearedStageNumber, episodeStageDataList.Count, canPlay));
        }
    }
    
    // 인벤토리(유물) 패널로 전환
    public async void OnClickRelicInventoryButton()
    {
        var popup = await UIManager.Instance.ShowUI<RelicInventoryUI>();
    }
    
    public async void OnClickLobbySettingPopup()
    {
        await UIManager.Instance.ShowUI<LobbySettingPopup>();
    }

    #region Animation
    
    // 애니메이션 없는 패널 전환용 메서드
    private Task SwitchPanel(GameObject targetPanel, Button clickedButton)
    {
        targetPanel.SetActive(true);
        currentPanel.SetActive(false);
        currentPanel = targetPanel;
        currentButton = clickedButton;
        return Task.CompletedTask;
    }
    
    // 패널 전환 애니메이션
    private async Task SwitchPanelAnimation(GameObject targetPanel, Button clickedButton)
    {
        // 이전 패널 페이드 아웃
        CanvasGroup currentCanvasGroup = GetOrAddCanvasGroup(currentPanel);

        // 새 패널 준비
        targetPanel.SetActive(true);
        targetPanel.transform.SetAsLastSibling(); // 새 패널을 최상위로 가져옴
        CanvasGroup targetCanvasGroup = GetOrAddCanvasGroup(targetPanel);
        targetCanvasGroup.alpha = 0;

        // 트랜지션 애니메이션 실행
        TaskCompletionSource<bool> animationComplete = new TaskCompletionSource<bool>();

        DOTween.Sequence()
            .Append(currentCanvasGroup.DOFade(0, transitionDuration).SetEase(transitionEase))
            .Append(targetCanvasGroup.DOFade(1, transitionDuration).SetEase(transitionEase))
            .OnComplete(() => {
                currentPanel.SetActive(false);
                currentPanel = targetPanel;
                currentButton = clickedButton;
                animationComplete.SetResult(true);
            });

        await animationComplete.Task;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject panel)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    #endregion
}
