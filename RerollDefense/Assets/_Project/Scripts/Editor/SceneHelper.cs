using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;

internal class SceneHelper
{
    private static int _sceneCount;
    private static readonly List<string> _scenes = new();
    private static readonly List<SceneInfo> _sceneInfos = new();
    private static int _selectIndex;
    private static string _scenePath;
    private static GUIContent _guiContentPopup = new();

    [InitializeOnLoadMethod]
    private static void InitializeSceneGUI()
    {
        ToolbarExtender.LeftToolbarGUI.Clear();
        ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUIScene);

        _sceneCount = EditorBuildSettings.scenes.Length;

        EditorBuildSettingsScene[] buildSettingsScenes = EditorBuildSettings.scenes;
        _sceneInfos.Clear();
        _scenes.Clear();
        for (int i = 0; i < _sceneCount; i++)
        {
            _sceneInfos.Add(new SceneInfo()
            {
                Path = buildSettingsScenes[i].path,
                Name = Path.GetFileNameWithoutExtension(buildSettingsScenes[i].path),
                Index = i,
            });

            _scenes.Add(_sceneInfos[i].Name);
        }

        SetSelectIndex(SceneManager.GetActiveScene().name);

        EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;

        EditorApplication.playModeStateChanged -= EditorApplicationOnplayModeStateChanged;
        EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;

        EditorBuildSettings.sceneListChanged -= InitializeSceneGUI;
        EditorBuildSettings.sceneListChanged += InitializeSceneGUI;
    }

    private static void OnSceneChanged(Scene current, Scene next)
    {
        SetSelectIndex(next.name);

        if (string.IsNullOrEmpty(current.name))
        {
            return;
        }
        // Debug.Log($"current : {current.name}, next : {next}.name");

        SelectPopup(next.name);
    }

    private static void SetSelectIndex(string activeSceneName)
    {
        for (int i = 0; i < _sceneCount; i++)
        {
            if (_sceneInfos[i].Name.Equals(activeSceneName))
            {
                _selectIndex = _sceneInfos[i].Index;
                return;
            }
        }

        _selectIndex = -1;
    }

    private static void OnToolbarGUIScene()
    {
        if (_sceneInfos.Count == 0
            || EditorApplication.isPlaying)
        {
            return;
        }

        GUILayout.FlexibleSpace();
        EditorGUI.BeginChangeCheck();

        SelectPopup(SceneManager.GetActiveScene().name);

        if (EditorGUI.EndChangeCheck())
        {
            if (Application.isPlaying)
            {
                Debug.Log("플레이모드중 사용 불가");
                return;
            }

            var sceneInfo = _sceneInfos[_selectIndex];
            if (File.Exists(Path.Combine(Path.GetFullPath("."), sceneInfo.Path)) == false)
            {
                EditorUtility.DisplayDialog("에러", $"{sceneInfo.Name}은 없는 Scene입니다. Build Settings에서 해당 Scene을 지워주세요.",
                    "확인");
                SetSelectIndex(SceneManager.GetActiveScene().name);
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(sceneInfo.Path, OpenSceneMode.Single);
            }
            else
            {
                SetSelectIndex(SceneManager.GetActiveScene().name);
            }
        }

        if (GUILayout.Button(new GUIContent($"▶{_sceneInfos[0].Name}", "게임의 첫 씬을 플레이합니다"), EditorStyles.toolbarButton))
        {
            StartScene(_sceneInfos[0].Path);
        }
    }

    private static void SelectPopup(string sceneName)
    {
        _guiContentPopup.text = sceneName;
        float activeWidth = 100;
        try
        {
            activeWidth = GUI.skin.label.CalcSize(_guiContentPopup).x + 20;
            _selectIndex = EditorGUILayout.Popup(_selectIndex, _scenes.ToArray(), GUILayout.Width(activeWidth));
        }
#pragma warning disable CS0168
        catch (ArgumentException e)
        {
        }
        catch (NullReferenceException e)
        {
        }
#pragma warning restore CS0168
    }

    private static void StartScene(string path)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.Log("플레이모드중 사용 불가");
            return;
        }

        _scenePath = path;
        EditorApplication.update += OnUpdate;
    }

    private static void OnUpdate()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPaused ||
            EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EditorApplication.update -= OnUpdate;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            SceneHelperSingleton.instance.OpenedScenePath = SceneManager.GetActiveScene().path;

            //프리팹 모드였다면 프리팹경로 저장
            PrefabStage openedPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (openedPrefabStage != null)
            {
                SceneHelperSingleton.instance.OpenedPrefabPath = openedPrefabStage.assetPath;
            }

            EditorSceneManager.OpenScene(_scenePath);
            EditorApplication.isPlaying = true;
        }
    }

    private static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange mode)
    {
        if (mode == PlayModeStateChange.EnteredEditMode)
        {
            if (string.IsNullOrEmpty(SceneHelperSingleton.instance.OpenedScenePath) == false)
            {
                EditorSceneManager.OpenScene(SceneHelperSingleton.instance.OpenedScenePath);
                SceneHelperSingleton.instance.OpenedScenePath = string.Empty;
            }

            //프리팹 모드였다면 프리팹 open
            if (string.IsNullOrEmpty(SceneHelperSingleton.instance.OpenedPrefabPath) == false)
            {
                PrefabStageUtility.OpenPrefab(SceneHelperSingleton.instance.OpenedPrefabPath);
                SceneHelperSingleton.instance.OpenedPrefabPath = string.Empty;
            }
        }
    }
}

struct SceneInfo
{
    internal string Name;
    internal string Path;
    internal int Index;
}