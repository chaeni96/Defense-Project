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
    [SerializeField] private GameObject boosterPanel;
    
    [SerializeField] private Button campButton;
    [SerializeField] private Button boosterButton;


    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Ease transitionEase = Ease.OutQuad;

    // ���� Ȱ��ȭ�� �гΰ� ��ư ����
    private GameObject currentPanel;
    private Button currentButton;
    private List<GameObject> allPanels;
    private List<Button> allButtons;

    private bool initialized = false;

    private UIBase campUI;
    private UIBase boosterUI;


    public override void InitializeUI()
    {
        base.InitializeUI();

        // ��� �гΰ� ��ư�� ����Ʈ�� ����
        allPanels = new List<GameObject> { campPanel, boosterPanel };
        allButtons = new List<Button> { campButton, boosterButton };

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

        // StageSelectUI ǥ�� (ķ�� �г��� ����)
        if(campUI == null)
        {
            campUI = await UIManager.Instance.ShowUI<StageSelectUI>();

        }
        initialized = true;

    }
    // ķ�� �гη� ��ȯ
    public async void SwitchToCampPanel()
    {
        if (!initialized || currentPanel == campPanel)
            return;

        // ���̵� �ִϸ��̼� ����
        await SwitchPanel(campPanel, campButton);

        // ���� UI ����� (���� Ȱ��ȭ�� UI�� �ִٸ�)
        if (boosterUI != null)
        {
            UIManager.Instance.CloseUI<BoosterSelectUI>();
        }

        // ķ�� UI�� ������ ����, ������ ǥ��
        if (campUI == null)
        {
            campUI = await UIManager.Instance.ShowUI<StageSelectUI>();
        }

    }

    // �ν��� �гη� ��ȯ
    public async void SwitchToBoosterPanel()
    {
        if (!initialized || currentPanel == boosterPanel)
            return;

        // ���̵� �ִϸ��̼� ����
        await SwitchPanel(boosterPanel, boosterButton);

        // ���� UI ����� (���� Ȱ��ȭ�� UI�� �ִٸ�)
        if (campUI != null)
        {
           UIManager.Instance.CloseUI<StageSelectUI>();
        }

        // �ν��� UI�� ������ ����, ������ ǥ��
        if (boosterUI == null)
        {
            boosterUI = await UIManager.Instance.ShowUI<BoosterSelectUI>(); // ���⿡ ���� UI Ÿ�� ����
        }
   
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

}
