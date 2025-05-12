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
    
    [SerializeField] private GameObject campPanel;
    [SerializeField] private GameObject inventoryPanel;
    
    [SerializeField] private Button campButton;
    [SerializeField] private Button inventoryButton;


    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Ease transitionEase = Ease.OutQuad;

    // 현재 활성화된 패널과 버튼 추적
    private GameObject currentPanel;
    private Button currentButton;
    private List<GameObject> allPanels;
    private List<Button> allButtons;

    private bool initialized = false;

    private EpisodeInfoUI episodeUI;
    private RelicInventoryUI inventoryUI;


    public override void InitializeUI()
    {
        base.InitializeUI();

        // 모든 패널과 버튼을 리스트로 관리
        allPanels = new List<GameObject> { campPanel, inventoryPanel };
        allButtons = new List<Button> { campButton, inventoryButton };

        // 초기 패널 설정 (캠프 패널)
        currentPanel = campPanel;
        currentButton = campButton;

        // 모든 패널 비활성화
        foreach (var panel in allPanels)
        {
            panel.SetActive(false);
        }

        OpenBaseUI();
    }


    private async void OpenBaseUI()
    {
        // 캠프 패널 활성화 및 초기 UI 표시
        campPanel.SetActive(true);

        // EpisodeInfoUI 표시 (캠프 패널의 내용)
        if (episodeUI == null)
        {
            episodeUI = await UIManager.Instance.ShowUI<EpisodeInfoUI>(campPanel.transform);
            episodeUI.CreateEpisodeInfo();
        }
        
        initialized = true;

    }
    // 캠프 패널로 전환
    public async void SwitchToCampPanel()
    {
        if (!initialized || currentPanel == campPanel)
            return;

        await SwitchPanel(campPanel, campButton);

        // 이전 UI 숨기기 (현재 활성화된 UI가 있다면)
        if (inventoryUI != null)
        {
            UIManager.Instance.CloseUI<RelicInventoryUI>();
        }

        // 캠프 UI가 없으면 생성, 있으면 표시
        if (episodeUI == null)
        {
            episodeUI = await UIManager.Instance.ShowUI<EpisodeInfoUI>(campPanel.transform);
            episodeUI.CreateEpisodeInfo();
        }

    }

    // 인벤토리(유물) 패널로 전환
    public async void SwitchToInventoryPanel()
    {
        // if (!initialized || currentPanel == inventoryPanel)
        //     return;
        //
        // await SwitchPanel(inventoryPanel, inventoryButton);
        //
        // // 이전 UI 숨기기 (현재 활성화된 UI가 있다면)
        // if (episodeUI != null)
        // {
        //    UIManager.Instance.CloseUI<EpisodeInfoUI>();
        // }
        //
        // inventoryUI = await UIManager.Instance.ShowUI<RelicInventoryUI>(); // 여기에 실제 UI 타입 지정
        // inventoryUI.InitLobbyDlg(this);

        var popup = await UIManager.Instance.ShowUI<RelicInventoryUI>();

    }
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

    public async void OnClickLobbySettingPopup()
    {
        await UIManager.Instance.ShowUI<LobbySettingPopup>();
    }

}
