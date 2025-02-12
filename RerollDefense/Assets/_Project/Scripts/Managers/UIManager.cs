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

    public Canvas fullWindowCanvas;    // 전체화면 UI용
    public Canvas fieldUICanvas;   // 필드 UI용, 팝업, 플로팅

    // UI 캐시를 위한 딕셔너리. Type을 키로 사용하여 UI 인스턴스를 저장
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
    /// UI 매니저 초기화. 각 씬에 따라 UI를 초기화하고 필요한 캔버스를 설정.
    /// </summary>
    public void InitializeManager(SceneKind scene)
    {
        CleanUp();

        uiCache = new Dictionary<Type, UIBase>();
        InitUIForScene(scene);
    }



    /// <summary>
    /// UI 유형에 따라 올바른 캔버스 트랜스폼을 반환.
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
    /// 특정 UI를 표시. 캐시된 UI가 있으면 활성화, 없으면 새로 생성
    /// </summary>
    public async Task<T> ShowUI<T>() where T : UIBase
    {
       
        T ui = await OpenUI<T>();

        return ui;
    }

    private async Task<T> OpenUI<T>() where T : UIBase
    {
        Type uiType = typeof(T);

        // 캐시된 UI가 있는 경우
        if (uiCache.TryGetValue(uiType, out UIBase cachedUI))
        {
            T uiInstance = (T)cachedUI;
            uiInstance.gameObject.SetActive(true);
            return uiInstance;
        }

        // UIInfoAttribute에서 AddressableKey 가져오기
        var uiInfoAttribute = (UIInfoAttribute)Attribute.GetCustomAttribute(uiType, typeof(UIInfoAttribute));
        if (uiInfoAttribute == null)
        {
            Debug.LogError($"{uiType.Name} 클래스에 UIInfoAttribute가 없습니다.");
            return null;
        }

        // Addressables를 사용하여 프리팹 비동기 로드
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
                Debug.LogError($"프리팹에 {uiType.Name} 컴포넌트가 없습니다.");
                Addressables.Release(handle);
                return null;
            }
        }
        else
        {
            Debug.LogError($"Addressables 로드 실패: {uiInfoAttribute.AddressableKey}");
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

    public async void InitUIForScene(SceneKind nextSceneKind)
    {

        // 기존 UI들을 모두 닫고 비활성화
        CloseAllUI();

        // 각 씬별, 조건별 UI 띄우기
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

    private void CleanUp()
    {
        // 캐시된 UI들 정리
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


}