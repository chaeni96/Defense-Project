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
    
    [SerializeField] private GameObject campPanel;
    [SerializeField] private GameObject inventoryPanel;
    
    [SerializeField] private Button campButton;
    [SerializeField] private Button inventoryButton;


    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Ease transitionEase = Ease.OutQuad;

    // ���� Ȱ��ȭ�� �гΰ� ��ư ����
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

        // ��� �гΰ� ��ư�� ����Ʈ�� ����
        allPanels = new List<GameObject> { campPanel, inventoryPanel };
        allButtons = new List<Button> { campButton, inventoryButton };

        // �ʱ� �г� ���� (ķ�� �г�)
        currentPanel = campPanel;
        currentButton = campButton;

        // ��� �г� ��Ȱ��ȭ
        foreach (var panel in allPanels)
        {
            panel.SetActive(false);
        }

        OpenBaseUI();
    }


    private async void OpenBaseUI()
    {
        // ķ�� �г� Ȱ��ȭ �� �ʱ� UI ǥ��
        campPanel.SetActive(true);

        // EpisodeInfoUI ǥ�� (ķ�� �г��� ����)
        if (episodeUI == null)
        {
            episodeUI = await UIManager.Instance.ShowUI<EpisodeInfoUI>(campPanel.transform);
            episodeUI.CreateEpisodeInfo();
        }
        
        initialized = true;

    }
    // ķ�� �гη� ��ȯ
    public async void SwitchToCampPanel()
    {
        if (!initialized || currentPanel == campPanel)
            return;

        await SwitchPanel(campPanel, campButton);

        // ���� UI ����� (���� Ȱ��ȭ�� UI�� �ִٸ�)
        if (inventoryUI != null)
        {
            UIManager.Instance.CloseUI<RelicInventoryUI>();
        }

        // ķ�� UI�� ������ ����, ������ ǥ��
        if (episodeUI == null)
        {
            episodeUI = await UIManager.Instance.ShowUI<EpisodeInfoUI>(campPanel.transform);
            episodeUI.CreateEpisodeInfo();
        }

    }

    // �κ��丮(����) �гη� ��ȯ
    public async void SwitchToInventoryPanel()
    {
        // if (!initialized || currentPanel == inventoryPanel)
        //     return;
        //
        // await SwitchPanel(inventoryPanel, inventoryButton);
        //
        // // ���� UI ����� (���� Ȱ��ȭ�� UI�� �ִٸ�)
        // if (episodeUI != null)
        // {
        //    UIManager.Instance.CloseUI<EpisodeInfoUI>();
        // }
        //
        // inventoryUI = await UIManager.Instance.ShowUI<RelicInventoryUI>(); // ���⿡ ���� UI Ÿ�� ����
        // inventoryUI.InitLobbyDlg(this);

        var popup = await UIManager.Instance.ShowUI<RelicInventoryUI>();

    }
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

    public async void OnClickLobbySettingPopup()
    {
        await UIManager.Instance.ShowUI<LobbySettingPopup>();
    }

}
