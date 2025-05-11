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

    public Canvas fullWindowCanvas;    // ��üȭ�� UI��
    public Canvas fieldUICanvas;   // �ʵ� UI��, �˾�, �÷���

    // UI ĳ�ø� ���� ��ųʸ�. Type�� Ű�� ����Ͽ� UI �ν��Ͻ��� ����
    private Dictionary<Type, UIBase> uiCache = new Dictionary<Type, UIBase>();

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
    public void InitializeManager()
    {


        CleanUp();
        uiCache = new Dictionary<Type, UIBase>();
    }



    /// <summary>
    /// UI ������ ���� �ùٸ� ĵ���� Ʈ�������� ��ȯ.
    /// </summary>
    private Transform GetCanvasTransform(Type uiType)
    {
        if (typeof(FullWindowBase).IsAssignableFrom(uiType))
            return fullWindowCanvas.transform;
        else if (typeof(PopupBase).IsAssignableFrom(uiType) || typeof(FloatingPopupBase).IsAssignableFrom(uiType))
            return fieldUICanvas.transform;
        return null;
    }

    /// <summary>
    /// Ư�� UI�� ǥ��. ĳ�õ� UI�� ������ Ȱ��ȭ, ������ ���� ����
    /// </summary>
    public async Task<T> ShowUI<T>() where T : UIBase
    {
       
        T ui = await OpenUI<T>();

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

    //Ư�� GameObject�� �θ�� �����ؼ� �����ϴ� �޼���
    public async Task<T> ShowUI<T>(Transform customParent) where T : UIBase
    {
        Type uiType = typeof(T);

        // ĳ�õ� UI�� �ִ� ���
        if (uiCache.TryGetValue(uiType, out UIBase cachedUI))
        {
            T uiInstance = (T)cachedUI;
            uiInstance.gameObject.SetActive(true);

            // �θ� ����
            uiInstance.transform.SetParent(customParent, false);

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
            // ���� ������ �θ� ���
            GameObject uiObject = Instantiate(handle.Result, customParent);
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
            }
        }
    }

    /// <summary>
    /// Ư�� Ÿ���� UI���������
    /// </summary>
    /// <typeparam name="T">������ UI�� Ÿ��</typeparam>
    /// <returns>ã�� UI �ν��Ͻ�, ������ null</returns>
    public T GetUI<T>() where T : UIBase
    {
        Type uiType = typeof(T);

        // ĳ�õ� UI�� �ִ� ���
        if (uiCache.TryGetValue(uiType, out UIBase cachedUI))
        {
            T uiInstance = (T)cachedUI;
            if (uiInstance.gameObject.activeSelf)
            {
                return uiInstance;
            }
        }

        // ĳ�ÿ� ���ų� Ȱ��ȭ���� ���� ��� null ��ȯ
        return null;
    }



    public async void InitUIForScene(SceneKind nextSceneKind)
    {

        // �� ����, ���Ǻ� UI ����
        switch (nextSceneKind)
        {
            case SceneKind.Lobby:
                await ShowUI<FullWindowLobbyDlg>();
                break;
            case SceneKind.InGame:
                await ShowUI<FullWindowInGameDlg>();
                break;
        }
    }

    public void CleanUp()
    {
        // ĳ�õ� UI�� ����
        foreach (var ui in uiCache.Values)
        {
            if (ui != null)
            {
                ui.HideUI();
            }
        }
        uiCache.Clear();

    }


}