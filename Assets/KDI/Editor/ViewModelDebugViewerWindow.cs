using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Kylin.LWDI.Editor
{
    public class ViewModelDebugViewerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _showInactiveViewModels = false;
        private string _searchFilter = "";
        private readonly Dictionary<Type, ViewModelDebugInfo> viewModelInfos = new Dictionary<Type, ViewModelDebugInfo>();
        private bool _showAllDependencyObjects = false;

        [MenuItem("Tools/Kylin/LWDI/ViewModel Debug Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<ViewModelDebugViewerWindow>("ViewModel Debugger");
            window.minSize = new Vector2(450, 300);
            window.Show();
        }

        private class ViewModelDebugInfo
        {
            public Type type { get; set; }
            public object Instance { get; set; }
            public int referenceCount { get; set; } = 0;
            public bool vmIsActive { get; set; } = false;
            public string[] sceneNames { get; set; } = Array.Empty<string>();
            public bool isGlobal { get; set; } = false;
            public bool isViewModel { get; set; } = false;
        }

        private void OnEnable()
        {
            // 창이 활성화될 때마다 정보 갱신
            EditorApplication.update += RefreshViewModelInfo;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RefreshViewModelInfo;
        }

        private void RefreshViewModelInfo()
        {
            viewModelInfos.Clear();
            var instances = DependencyContainer.Instance.GetAllInstances();

            foreach (var pair in instances)
            {
                var type = pair.Key;
                var instance = pair.Value;
                
                // 모든 IDependencyObject 처리
                var isViewModel = instance is BaseViewModel;
                int referenceCount = 0;
                bool isActive = false;
                
                if (isViewModel && instance is BaseViewModel viewModel)
                {
                    referenceCount = GetReferenceCount(viewModel);
                    isActive = GetIsActiveViewModel(viewModel);
                }

                var attr = type.GetCustomAttributes(typeof(ViewModelAttribute), true).FirstOrDefault() as ViewModelAttribute;
                var isGlobal = attr?.IsGlobal ?? false;
                var sceneNames = attr?.SceneNames ?? Array.Empty<string>();

                viewModelInfos[type] = new ViewModelDebugInfo
                {
                    type = type,
                    Instance = instance,
                    referenceCount = referenceCount,
                    vmIsActive = isActive,
                    sceneNames = sceneNames,
                    isGlobal = isGlobal,
                    isViewModel = isViewModel
                };
            }
        }

        private int GetReferenceCount(BaseViewModel viewModel)
        {
            var fieldInfo = typeof(BaseViewModel).GetField("referenceCount",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo != null ? (int)fieldInfo.GetValue(viewModel) : -1;
        }

        private bool GetIsActiveViewModel(BaseViewModel viewModel)
        {
            var fieldInfo = typeof(BaseViewModel).GetField("isActiveViewModel", BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo != null && (bool)fieldInfo.GetValue(viewModel);
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space();
            DrawViewModelsList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshViewModelInfo();
            }

            _showInactiveViewModels = EditorGUILayout.ToggleLeft("비활성 ViewModel 표시", _showInactiveViewModels, 
                GUILayout.Width(150));
                
            _showAllDependencyObjects = EditorGUILayout.ToggleLeft("모든 DI 객체 표시", _showAllDependencyObjects, 
                GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("검색:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
            
            // 등록된 총 객체 수 표시
            EditorGUILayout.LabelField($"등록된 총 객체 수: {viewModelInfos.Count}", EditorStyles.boldLabel);
        }

        private void DrawViewModelsList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var filteredObjects = viewModelInfos.Values
                .Where(info => _showAllDependencyObjects || info.isViewModel) // ViewModel만 보여줄지 모든 DI 객체를 보여줄지
                .Where(info => _showInactiveViewModels || !info.isViewModel || info.vmIsActive) // 비활성 ViewModel 필터링
                .Where(info => string.IsNullOrEmpty(_searchFilter) || 
                               info.type.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(info => info.isViewModel ? 0 : 1) // ViewModel을 먼저 정렬
                .ThenBy(info => info.type.Name)
                .ToList();

            if (filteredObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("표시할 객체가 없습니다.", MessageType.Info);
            }
            else
            {
                DrawTableHeader();
                
                foreach (var info in filteredObjects)
                {
                    DrawObjectInfo(info);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("타입", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.LabelField("객체 종류", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("참조 카운트", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("활성 상태", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("스코프", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawObjectInfo(ViewModelDebugInfo info)
        {
            EditorGUILayout.BeginHorizontal(
                info.isViewModel && info.vmIsActive ? EditorStyles.helpBox : EditorStyles.textField);

            GUIStyle style = new GUIStyle(EditorStyles.label);
            if (info.isViewModel && !info.vmIsActive)
            {
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            }

            EditorGUILayout.LabelField(info.type.Name, style, GUILayout.Width(200));
            
            // 객체 종류 (ViewModel 또는 Model)
            string objectType = info.isViewModel ? "ViewModel" : "Model";
            EditorGUILayout.LabelField(objectType, style, GUILayout.Width(80));
            
            // 참조 카운트 (ViewModel인 경우만)
            string refCount = info.isViewModel ? info.referenceCount.ToString() : "-";
            EditorGUILayout.LabelField(refCount, style, GUILayout.Width(80));
            
            // 활성 상태 (ViewModel인 경우만)
            string activeStatus = info.isViewModel ? (info.vmIsActive ? "활성" : "비활성") : "-";
            EditorGUILayout.LabelField(activeStatus, style, GUILayout.Width(80));

            // 스코프 (ViewModelAttribute가 있는 경우만)
            string scope = info.sceneNames.Length > 0 || info.isGlobal ? (info.isGlobal ? "전역" : "씬") : "-";
            EditorGUILayout.LabelField(scope, style, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();

            // 씬 이름 목록 표시 (글로벌이 아니고 씬 이름이 있는 경우)
            if (!info.isGlobal && info.sceneNames.Length > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("씬:", GUILayout.Width(30));
                EditorGUILayout.LabelField(string.Join(", ", info.sceneNames), style);
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
        }
    }
}