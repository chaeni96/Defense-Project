using BGDatabaseEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;

    public Canvas mainCanvas;    // ��üȭ�� UI��
    public Canvas fieldUICanvas;   // �˾� UI��

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

    public void InitializeUIManager(SceneType scene)
    {
        uiCache = new Dictionary<Type, UIBase>();
        //ui������ �о�;���
        AssignCanvases();
        InitUIForScene(scene);
    }

    //ĵ���� �Ҵ�
    private void AssignCanvases()
    {
        if (mainCanvas == null)
            mainCanvas = GameObject.Find("FullWindowCanvas")?.GetComponent<Canvas>();
        if (fieldUICanvas == null)
            fieldUICanvas = GameObject.Find("FieldCanvas")?.GetComponent<Canvas>();
    }

    //ĵ���� Ʈ������ �����;ߵ�
    private Transform GetCanvasTransform(Type uiType)
    {
        if (typeof(FullWindowBase).IsAssignableFrom(uiType))
            return mainCanvas.transform;
        else if (typeof(PopupBase).IsAssignableFrom(uiType))
            return fieldUICanvas.transform;
        return null;
    }

    //�ٸ� ui�۾����̸� �����ϸ� �ȵ�
    public T ShowUI<T>(string prefabName = null) where T : UIBase
    {
        if (isUIOperationInProgress)
        {
            Debug.LogWarning("�ٸ� UI �۾��� ���� ���Դϴ�.");
            return null;
        }

        isUIOperationInProgress = true;
        T ui = OpenUI<T>(prefabName);
        isUIOperationInProgress = false;

        return ui;
    }
    private T OpenUI<T>(string prefabName) where T : UIBase
    {
        if (uiCache.TryGetValue(typeof(T), out UIBase cachedUI))
        {
            T ui = (T)cachedUI;
            ui.gameObject.SetActive(true);
            return ui;
        }

        if (string.IsNullOrEmpty(prefabName))
        {
            prefabName = typeof(T).Name;
        }

        //TODO : FindEntity���� �ٸ��ɷ�
        var prefabData = D_UIPrefabData.FindEntity(data => data.f_PrefabKey == prefabName);
        if (prefabData != null)
        {
            Transform parent = GetCanvasTransform(typeof(T));
            GameObject uiObject = ResourceManager.Instance.Instantiate(prefabData.f_PrefabKey, parent);
            if (uiObject != null)
            {
                T ui = uiObject.GetComponent<T>();
                if (ui != null)
                {
                    ui.InitializeUI();
                    uiCache[typeof(T)] = ui;
                    return ui;
                }
            }
        }

        Debug.LogError($"UI Prefab not found: {prefabName}");
        return null;
    }


    public void CloseUI<T>() where T : UIBase
    {
        if (uiCache.TryGetValue(typeof(T), out UIBase ui))
        {
            ui.CloseUI();
            ui.gameObject.SetActive(false);
            uiCache.Remove(typeof(T));
            Destroy(ui.gameObject);
        }
    }

    public void CloseAllUI()
    {
        foreach (var ui in uiCache.Values)
        {
            if (ui != null)
            {
                ui.CloseUI();
                Destroy(ui.gameObject);
            }
        }
        uiCache.Clear();
    }

    public void InitUIForScene(SceneType nextSceneKind)
    {


        // ���� UI���� ��� �ݰ� ��Ȱ��ȭ
        CloseAllUI();

        // �� ����, ���Ǻ� UI ����
        switch (nextSceneKind)
        {
            case SceneType.Lobby:
                break;
            case SceneType.Game:
                ShowUI<FullWindowInGameDlg>("FullWindowInGameDlg");
                break;
        }
    }

}