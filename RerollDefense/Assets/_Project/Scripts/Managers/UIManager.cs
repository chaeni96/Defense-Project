using BGDatabaseEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;

    public Canvas mainCanvas;    // ��üȭ�� UI��
    public Canvas fieldUICanvas;   // �˾� UI��

    // UI ĳ�ø� ���� ��ųʸ�. Type�� Ű�� ����Ͽ� UI �ν��Ͻ��� ����
    private Dictionary<Type, UIBase> uiCache = new Dictionary<Type, UIBase>();
    private bool isUIOperationInProgress = false;

    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("UIManager");
                    _instance = singleton.AddComponent<UIManager>();
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
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// UI �Ŵ��� �ʱ�ȭ. �� ���� ���� UI�� �ʱ�ȭ�ϰ� �ʿ��� ĵ������ ����.
    /// </summary>
    public async void InitializeUIManager(SceneType scene)
    {
        uiCache = new Dictionary<Type, UIBase>();
        AssignCanvases();
        await InitUIForScene(scene);
    }

    /// <summary>
    /// ĵ���� �Ҵ�. �ʿ��� ĵ������ ã�ų� ����.
    /// </summary>
    private void AssignCanvases()
    {
        if (mainCanvas == null)
            mainCanvas = GameObject.Find("FullWindowCanvas")?.GetComponent<Canvas>();
        if (fieldUICanvas == null)
            fieldUICanvas = GameObject.Find("FieldCanvas")?.GetComponent<Canvas>();
    }

    /// <summary>
    /// UI ������ ���� �ùٸ� ĵ���� Ʈ�������� ��ȯ.
    /// </summary>
    private Transform GetCanvasTransform(Type uiType)
    {
        if (typeof(FullWindowBase).IsAssignableFrom(uiType))
            return mainCanvas.transform;
        else if (typeof(PopupBase).IsAssignableFrom(uiType))
            return fieldUICanvas.transform;
        return null;
    }

    /// <summary>
    /// Ư�� UI�� ǥ��. ĳ�õ� UI�� ������ Ȱ��ȭ, ������ ���� ����
    /// </summary>
    public async Task<T> ShowUI<T>() where T : UIBase
    {
        if (isUIOperationInProgress)
        {
            Debug.LogWarning("�ٸ� UI �۾��� ���� ���Դϴ�.");
            return null;
        }

        isUIOperationInProgress = true;
        T ui = await OpenUI<T>();
        isUIOperationInProgress = false;

        return ui;
    }

    private async Task<T> OpenUI<T>() where T : UIBase
    {
        Type uiType = typeof(T);

        // ĳ�õ� UI�� �ִ� ���
        if (uiCache.TryGetValue(uiType, out UIBase cachedUI))
        {
            T uiInstance = (T)cachedUI;
            uiInstance.gameObject.SetActive(true);
            return uiInstance;
        }

        // UIInfoAttribute���� AddressableKey ��������
        var uiInfoAttribute = (UIInfoAttribute)Attribute.GetCustomAttribute(uiType, typeof(UIInfoAttribute));
        if (uiInfoAttribute == null)
        {
            Debug.LogError($"{uiType.Name} Ŭ������ UIInfoAttribute�� �����ϴ�.");
            return null;
        }

        // Addressables�� ����Ͽ� ������ �񵿱� �ε�
        var handle = Addressables.LoadAssetAsync<GameObject>(uiInfoAttribute.AddressableKey);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Transform parent = GetCanvasTransform(uiType);
            GameObject uiObject = Instantiate(handle.Result, parent);
            uiObject.name = uiInfoAttribute.ObjectName;

            T ui = uiObject.GetComponent<T>();
            if (ui != null)
            {
                ui.InitializeUI();
                uiCache[uiType] = ui;
                return ui;
            }
            else
            {
                Debug.LogError($"�����տ� {uiType.Name} ������Ʈ�� �����ϴ�.");
                Addressables.Release(handle);
                return null;
            }
        }
        else
        {
            Debug.LogError($"Addressables �ε� ����: {uiInfoAttribute.AddressableKey}");
            return null;
        }
    }


    public void CloseUI<T>() where T : UIBase
    {
        Type uiType = typeof(T);

        if (uiCache.TryGetValue(uiType, out UIBase ui))
        {
            ui.HideUI();

            if (ui.DestroyOnHide)
            {
                uiCache.Remove(uiType);
                Destroy(ui.gameObject);
            }
        }
    }


    public void CloseAllUI()
    {
        foreach (var ui in uiCache.Values)
        {
            if (ui != null)
            {
                ui.HideUI();
                Destroy(ui.gameObject);
            }
        }
        uiCache.Clear();
    }

    public async Task InitUIForScene(SceneType nextSceneKind)
    {

        // ���� UI���� ��� �ݰ� ��Ȱ��ȭ
        CloseAllUI();

        // �� ����, ���Ǻ� UI ����
        switch (nextSceneKind)
        {
            case SceneType.Lobby:
                break;
            case SceneType.Game:
                await ShowUI<FullWindowInGameDlg>();
                break;
        }
    }

}