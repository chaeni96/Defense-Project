using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("FullWindowLobbyDlg", "FullWindowLobbyDlg", true)]
public class FullWindowLobbyDlg : FullWindowBase
{

    //ȭ�� �г�
    [SerializeField] private EpisodeInfoUI episodeUI;
    
    [SerializeField] private Button inventoryButton;

    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Ease transitionEase = Ease.OutQuad;

    // ���� Ȱ��ȭ�� �гΰ� ��ư ����
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
        
        // ������ �����ؾ��� ���Ǽҵ� ��ȯ
        episodeData = userBestRecordEpisode == 0
            ? D_EpisodeData.FindEntity(e => e.f_episodeNumber == 1)
            : D_EpisodeData.FindEntity(e => e.f_episodeNumber == userBestRecordEpisode + 1);
        
        episodeStageDataList = D_StageData.GetEntitiesByKeyEpisodeKey(episodeData);
        
        if (episodeStageDataList is { Count: > 0 })
        {
            // ���Ǽҵ� UI �ʱ�ȭ
            bool canPlay = episodeData.f_episodeNumber == 1 || userData.f_clearEpisodeNumber >= episodeData.f_episodeNumber;
            
            episodeUI.InitializeEpisodeInfo(new EpisodeInfoParam(episodeData, userData.f_lastClearedStageNumber, episodeStageDataList.Count, canPlay));
        }
    }
    
    // �κ��丮(����) �гη� ��ȯ
    public async void OnClickRelicInventoryButton()
    {
        var popup = await UIManager.Instance.ShowUI<RelicInventoryUI>();
    }
    
    public async void OnClickLobbySettingPopup()
    {
        await UIManager.Instance.ShowUI<LobbySettingPopup>();
    }

    #region Animation
    
    // �ִϸ��̼� ���� �г� ��ȯ�� �޼���
    private Task SwitchPanel(GameObject targetPanel, Button clickedButton)
    {
        targetPanel.SetActive(true);
        currentPanel.SetActive(false);
        currentPanel = targetPanel;
        currentButton = clickedButton;
        return Task.CompletedTask;
    }
    
    // �г� ��ȯ �ִϸ��̼�
    private async Task SwitchPanelAnimation(GameObject targetPanel, Button clickedButton)
    {
        // ���� �г� ���̵� �ƿ�
        CanvasGroup currentCanvasGroup = GetOrAddCanvasGroup(currentPanel);

        // �� �г� �غ�
        targetPanel.SetActive(true);
        targetPanel.transform.SetAsLastSibling(); // �� �г��� �ֻ����� ������
        CanvasGroup targetCanvasGroup = GetOrAddCanvasGroup(targetPanel);
        targetCanvasGroup.alpha = 0;

        // Ʈ������ �ִϸ��̼� ����
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
