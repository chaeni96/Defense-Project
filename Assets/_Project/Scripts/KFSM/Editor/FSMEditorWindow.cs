using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using static UnityEditor.TypeCache;
using UnityEditor.UIElements;
using System.Text;
using System.IO;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kylin.FSM
{
    public class FSMGraphView : GraphView
    {
        public Action<StateNode> OnNodeSelected;
        public Action<TransitionEdge> OnEdgeSelected;
        public Action OnMultiSelectionChanged;
        public Action OnSelectionCleared;
        public Dictionary<int, StateNode> StateNodes { get; } = new Dictionary<int, StateNode>();
        public StateNode AnyStateNode { get; private set; }
        public EntryNode entryNode { get; set; }
        public FSMGraphView()
        {
            Debug.Log("FSM Graph View constructor called");

            name = "FSM Graph";

            // 드래그 확대 축소 등 조작 설정
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());


            // 선택 변경 시 호출되는 이벤트 처리
            this.RegisterCallback<MouseUpEvent>(e =>
            {
                // 선택 카운트에 따라 다른 처리
                if (selection.Count > 1)
                {
                    // 다중 선택 처리
                    Debug.Log($"다중 선택됨 - 항목 수: {selection.Count}");
                    OnMultiSelectionChanged?.Invoke();
                    return;
                }
                if(selection.Count <= 0)
                {
                    //빈곳 클릭시 인스펙터 초기화
                    OnSelectionCleared?.Invoke();
                }
                
            });
            // 그리드 배경 추가
            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);
        }
        public void CreateEntryNode()
        {
            // 기존 Entry 노드가 있으면 제거
            if (entryNode != null)
            {
                Debug.Log("기존 Entry 노드 제거");
                RemoveElement(entryNode);
                entryNode = null;
            }

            // 새 Entry 노드 생성
            entryNode = new EntryNode();
            entryNode.SetPosition(new Rect(300, 300, 100, 80));
            AddElement(entryNode);

            Debug.Log("새 Entry 노드 생성됨");
        }
        public void ConnectEntryToState(int stateId)
        {
            Debug.Log($"ConnectEntryToState 호출됨: stateId={stateId}");

            if (entryNode == null)
            {
                Debug.LogError("Entry 노드가 없습니다. 먼저 Entry 노드를 생성하세요.");
                CreateEntryNode();
                if (entryNode == null)
                {
                    Debug.LogError("Entry 노드 생성 실패");
                    return;
                }
            }

            if (!StateNodes.TryGetValue(stateId, out var targetNode))
            {
                Debug.LogError($"상태 ID {stateId}인 노드를 찾을 수 없습니다");
                return;
            }

            Debug.Log($"Entry → 상태 {stateId} 연결 시작");

            try
            {
                // 기존 Entry 연결 모두 제거
                var existingEdges = entryNode.OutputPort.connections.ToList();
                foreach (var edge in existingEdges)
                {
                    if (edge.output != null)
                        edge.output.Disconnect(edge);

                    if (edge.input != null)
                        edge.input.Disconnect(edge);

                    RemoveElement(edge);
                    Debug.Log("기존 Entry 연결 제거됨");
                }

                // 새 연결 생성
                var entryEdge = new EntryTransitionEdge(entryNode.OutputPort, targetNode.inputPort);

                // 포트에 연결
                entryNode.OutputPort.Connect(entryEdge);
                targetNode.inputPort.Connect(entryEdge);

                // 그래프에 추가
                AddElement(entryEdge);

                // 초기 상태 ID 업데이트
                entryNode.InitialStateId = stateId;

                Debug.Log($"Entry가 상태 ID {stateId}에 성공적으로 연결됨");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Entry 연결 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }
        // AnyState 노드 생성 - 이미 존재하면 아무것도 하지 않음
        public void CreateAnyStateNode()
        {
            // 기존 AnyState 노드가 있으면 제거
            if (AnyStateNode != null)
            {
                Debug.Log("기존 AnyState 노드 제거");
                RemoveElement(AnyStateNode);

                // StateNodes 딕셔너리에서도 제거
                if (StateNodes.ContainsKey(Transition.ANY_STATE))
                {
                    StateNodes.Remove(Transition.ANY_STATE);
                }

                AnyStateNode = null;
            }

            var entry = new StateEntry
            {
                Id = Transition.ANY_STATE,
                stateTypeName = "AnyState",
                position = new Vector2(100, 100)
            };

            AnyStateNode = new StateNode(entry, -1);
            AnyStateNode.inputContainer.RemoveAt(0);
            AnyStateNode.SetPosition(new Rect(entry.position, new Vector2(150, 100)));
            AnyStateNode.capabilities &= ~Capabilities.Deletable; // 삭제 불가능하게 설정
            AnyStateNode.style.backgroundColor = new StyleColor(new Color(0.6f, 0.4f, 0.7f, 0.8f)); // 특별한 색상 지정

            AddElement(AnyStateNode);
            StateNodes[Transition.ANY_STATE] = AnyStateNode;

            Debug.Log("AnyState 노드 생성됨");
        }
        private void CleanupEntryConnections()
        {
            if (entryNode == null) return;

            Debug.Log("Entry 연결 정리 시작");

            // 기존 연결 모두 찾기
            var existingEdges = entryNode.OutputPort.connections.ToList();

            foreach (var edge in existingEdges)
            {
                // 포트 연결 해제
                if (edge.output != null)
                {
                    edge.output.Disconnect(edge);
                }

                if (edge.input != null)
                {
                    edge.input.Disconnect(edge);
                }

                // 그래프에서 엣지 제거
                RemoveElement(edge);
                Debug.Log("기존 Entry 연결 제거됨");
            }

            // 출력 포트가 깨끗한지 확인
            if (entryNode.OutputPort.connections.Count() > 0)
            {
                Debug.LogWarning($"Entry 출력 포트에 여전히 {entryNode.OutputPort.connections.Count()} 개의 연결이 있습니다!");
            }
            else
            {
                Debug.Log("Entry 출력 포트가 깔끔하게 정리되었습니다.");
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            // Entry 노드의 OutputPort에서 시작하는 경우 - 이미 연결된 항목이 있으면 빈 리스트 반환
            if (startPort.node is EntryNode && startPort.direction == Direction.Output)
            {
                // 이미 연결이 있으면 빈 리스트 반환 (더 이상 연결 불가능)
                if (startPort.connections.Any())
                {
                    return compatiblePorts; // 빈 리스트 반환
                }
            }

            // EntryTransitionEdge가 드래그되는 경우 방지
            foreach (var connection in startPort.connections)
            {
                if (connection is EntryTransitionEdge)
                {
                    return compatiblePorts; // 엔트리 트랜지션인 경우 드래그 방지
                }
            }

            // Entry 노드로의 연결 시도인 경우 (어떤 노드에서든 Entry 노드로는 연결 불가능)
            if (startPort.direction == Direction.Output)
            {
                foreach (var port in ports.ToList())
                {
                    if (port.direction == Direction.Input && port != startPort && !(port.node is EntryNode))
                    {
                        compatiblePorts.Add(port);
                    }
                }
            }
            else // Input 포트에서 시작하는 경우
            {
                foreach (var port in ports.ToList())
                {
                    if (port.direction == Direction.Output && port != startPort && !(port.node is EntryNode && startPort.node is EntryNode))
                    {
                        compatiblePorts.Add(port);
                    }
                }
            }

            return compatiblePorts;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView || evt.target is GridBackground)
            {
                // State 타입 가져오기
                var stateTypes = TypeCache.GetTypesDerivedFrom<StateBase>()
                    .Where(t => !t.IsAbstract && t != typeof(StateBase))
                    .ToArray();

                if (stateTypes.Length == 0)
                {
                    evt.menu.AppendAction("No State Types Found", _ => { }, DropdownMenuAction.Status.Disabled);
                }
                else
                {
                    // 메뉴 경로별 타입 정렬을 위한 사전
                    var menuPathToTypes = new Dictionary<string, List<Type>>();

                    // 기본 경로 (어트리뷰트가 없는 타입용)
                    const string defaultPath = "Create/State/Other";
                    menuPathToTypes[defaultPath] = new List<Type>();

                    foreach (var t in stateTypes)
                    {
                        // FSMContextFolder 어트리뷰트 가져오기
                        var attr = t.GetCustomAttribute<FSMContextFolderAttribute>();
                        string menuPath = attr != null ? attr.MenuPath : defaultPath;

                        // 메뉴 경로에 타입 추가
                        if (!menuPathToTypes.TryGetValue(menuPath, out var typeList))
                        {
                            typeList = new List<Type>();
                            menuPathToTypes[menuPath] = typeList;
                        }

                        typeList.Add(t);
                    }

                    // 모든 메뉴 경로를 정렬하여 알파벳 순서로 표시
                    var sortedPaths = menuPathToTypes.Keys.OrderBy(path => path).ToList();

                    foreach (var menuPath in sortedPaths)
                    {
                        var types = menuPathToTypes[menuPath];

                        // 각 경로에 포함된 타입들을 정렬
                        types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

                        foreach (var t in types)
                        {
                            // 노드 생성 액션 추가 
                            Vector2 position = evt.mousePosition; // 마우스 위치에 노드 생성
                            string actionPath = $"{menuPath}/{t.Name}";

                            evt.menu.AppendAction(actionPath, _ => {
                                var window = EditorWindow.GetWindow<FSMEditorWindow>();
                                window.CreateNode(position, t.FullName);
                            });
                        }
                    }
                }

                evt.menu.AppendSeparator();
            }

            // 부모 메서드 호출하여 기본 컨텍스트 메뉴 항목 추가
            base.BuildContextualMenu(evt);
        }
    }

    public class FSMEditorWindow : EditorWindow
    {
        private FSMGraphView _graphView;
        public FSMDataAsset _dataAsset;
        private FSMDataAsset _originalAsset;
        SerializedObject _dataObject;
        VisualElement _inspector;
        Dictionary<string, TransitionEdge> _transitionEdges = new Dictionary<string, TransitionEdge>();

        const string k_AssetPath = "Assets/_Project/AddressableResources/Remote/FSMData/FSMData.asset";
        const string k_ConstantsPath = "Assets/_Project/Scripts/KFSM/Generated/TransitionConstants.cs";

        private bool _anyStateCreated = false;

        // 현재 보여줄 요소 추적
        private enum SelectionType { None, Node, Edge, Multiple }
        private SelectionType _currentSelectionType = SelectionType.None;
        private object _currentSelectedItem = null;

        private FSMDataCollection _dataCollection;
        private string _currentFsmId = "NewFSM"; // 현재 편집 중인 FSM의 ID
        private TextField _fsmIdField; // FSM ID를 표시하고 편집할 수 있는 필드
        private DropdownField _fsmSelector; // 존재하는 FSM 중에서 선택할 수 있는 드롭다운
        private const string k_CollectionPath = "Assets/_Project/AddressableResources/Remote/FSMData/FSMDataCollection.asset";

        public int initialStateId;

        [MenuItem("Window/Custom Tools/Visual FSM Editor", false, 100)]
        public static void Open()
        {
            var window = GetWindow<FSMEditorWindow>("FSM Editor");
        }

        private void SetupToolbar()
        {
            // 툴바 생성
            var toolbar = new Toolbar();

            // 1. Save FSM 버튼
            toolbar.Add(new ToolbarButton(SaveFSM) { text = "Save FSM" });

            // 2. Save As 버튼 (다른 이름으로 저장)
            toolbar.Add(new ToolbarButton(SaveAsNewFSM) { text = "Save As" });

            // 3. Load FSM 버튼
            toolbar.Add(new ToolbarButton(LoadFSMFromFile) { text = "Load FSM" });

            // 4. New FSM 버튼
            toolbar.Add(new ToolbarButton(CreateNewFSM) { text = "New FSM" });

            // 5. Delete FSM 버튼
            toolbar.Add(new ToolbarButton(DeleteFSMFromFile) { text = "Delete FSM" });

            // 6. Generate State Factory 버튼
            toolbar.Add(new ToolbarButton(GenerateStateFactory) { text = "Generate Factory" });

            // 툴바를 루트 요소에 추가
            rootVisualElement.Add(toolbar);
        }

        // 1. Save FSM - 현재 FSM 저장
        private void SaveFSM()
        {
            if (_originalAsset != null)
            {
                // 이미 로드된 Asset이 있으면 바로 저장
                SaveToExistingAsset(_originalAsset);
            }
            else
            {
                // 신규 또는 로드되지 않은 경우 익스플로러로 저장 위치 지정
                SaveAsNewFSM();
            }
        }

        // 2. Save As - 다른 이름으로 저장
        private void SaveAsNewFSM()
        {
            string defaultPath = "Assets/_Project/AddressableResources/Remote/FSMData";
            string defaultName = _currentFsmId == "NewFSM" ? "NewFSM" : _currentFsmId;

            string path = EditorUtility.SaveFilePanelInProject(
                "Save FSM Data",
                defaultName + ".asset",
                "asset",
                "Choose location to save FSM data",
                defaultPath);

            if (string.IsNullOrEmpty(path))
                return; // 사용자가 취소함

            // 파일 이름에서 FSM ID 추출
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            _currentFsmId = fileName;

            // CollectionSO에 이미 같은 ID가 있는지 확인
            bool idExists = _dataCollection.IndexOfId(_currentFsmId) >= 0;

            if (idExists)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite FSM",
                    $"FSM with ID '{_currentFsmId}' already exists in collection. Overwrite?",
                    "Overwrite", "Cancel");

                if (!overwrite)
                    return;
            }

            // 새 Asset 생성 및 저장
            FSMDataAsset newAsset = ScriptableObject.CreateInstance<FSMDataAsset>();

            // 현재 데이터 복제
            newAsset.StateEntries = new List<StateEntry>();
            foreach (var entry in _dataAsset.StateEntries)
            {
                newAsset.StateEntries.Add(CloneStateEntry(entry));
            }

            newAsset.Transitions = new List<TransitionEntry>();
            foreach (var transition in _dataAsset.Transitions)
            {
                newAsset.Transitions.Add(CloneTransitionEntry(transition));
            }

            // 초기 상태 ID 저장
            newAsset.InitialStateId = initialStateId;

            // 에셋 저장
            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();

            // 새 에셋을 현재 로드된 에셋으로 설정
            _originalAsset = newAsset;

            // FSM 컬렉션에 추가/갱신
            _dataCollection.AddFSMData(_currentFsmId, newAsset);
            EditorUtility.SetDirty(_dataCollection);
            AssetDatabase.SaveAssets();

            Debug.Log($"FSM saved as: {path}");
        }

        // 기존 에셋에 저장하는 내부 메서드
        private void SaveToExistingAsset(FSMDataAsset asset)
        {
            // 자원 생성
            //GenerateTransitionConstants();

            // Asset 초기화
            asset.StateEntries.Clear();
            asset.Transitions.Clear();

            // 현재 데이터 복사
            foreach (var entry in _dataAsset.StateEntries)
            {
                asset.StateEntries.Add(CloneStateEntry(entry));
            }

            foreach (var transition in _dataAsset.Transitions)
            {
                asset.Transitions.Add(CloneTransitionEntry(transition));
            }

            // 초기 상태 ID 저장
            asset.InitialStateId = initialStateId;

            // 에셋 저장
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            // CollectionSO 업데이트
            _dataCollection.AddFSMData(_currentFsmId, asset);
            EditorUtility.SetDirty(_dataCollection);
            AssetDatabase.SaveAssets();

            Debug.Log($"FSM '{_currentFsmId}' 저장 완료");
        }


        // 3. Load FSM - 파일 열기 대화상자로 FSM 불러오기
        private void LoadFSMFromFile()
        {
            if (HasUnsavedChanges())
            {
                bool save = EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    "Current FSM has unsaved changes. Save before loading a new FSM?",
                    "Save", "Discard");

                if (save)
                    SaveFSM();
            }

            string defaultPath = "Assets/_Project/AddressableResources/Remote/FSMData";
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Load FSM Data",
                defaultPath,
                new[] { "FSM Assets", "asset" });

            if (string.IsNullOrEmpty(path))
                return; // 사용자가 취소함

            // 프로젝트 경로로 변환
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            FSMDataAsset loadedAsset = AssetDatabase.LoadAssetAtPath<FSMDataAsset>(path);
            if (loadedAsset == null)
            {
                EditorUtility.DisplayDialog("Load Error", "Failed to load FSM asset.", "OK");
                return;
            }

            // FSM ID 추출
            string assetName = System.IO.Path.GetFileNameWithoutExtension(path);
            _currentFsmId = assetName;

            // 중요: 그래프 요소 완전 초기화
            ResetGraphView();

            // 로드된 데이터 설정
            _originalAsset = loadedAsset;
            _dataAsset = CloneFSMDataAsset(loadedAsset);
            _dataObject = new SerializedObject(_dataAsset);

            // 초기 상태 ID 저장
            initialStateId = loadedAsset.InitialStateId;
            Debug.Log($"Loaded FSM Initial State ID: {initialStateId}");

            // AnyState 생성 플래그 리셋
            _anyStateCreated = false;

            // 그래프 완전 새로 구성
            CreateNewGraph();

            Debug.Log($"Loaded FSM from: {path}");
        }
        private void ClearEntireGraph()
        {
            if (_graphView == null)
                return;

            Debug.Log("그래프 완전 초기화 시작");

            // 현재 선택 초기화
            _graphView.ClearSelection();
            _currentSelectionType = SelectionType.None;
            _currentSelectedItem = null;
            ClearInspector();

            // 그래프 엘리먼트 제거를 위한 안전 복사본 만들기
            var allElements = new List<GraphElement>();

            // 먼저 모든 엣지 추가
            allElements.AddRange(_graphView.edges.ToList());

            // 그 다음 모든 노드 추가
            allElements.AddRange(_graphView.nodes.ToList());

            // 모든 엘리먼트 삭제 시도
            _graphView.DeleteElements(allElements);

            // Entry 노드 참조 초기화
            _graphView.entryNode = null;

            // 내부 컬렉션 초기화
            _graphView.StateNodes.Clear();
            _transitionEdges.Clear();

            Debug.Log("그래프 완전 초기화 완료");
        }
        private void ResetGraphView()
        {
            if (_graphView == null)
                return;

            Debug.Log("그래프 완전 초기화 시작");

            // 현재 선택 초기화
            _graphView.ClearSelection();
            _currentSelectionType = SelectionType.None;
            _currentSelectedItem = null;
            ClearInspector();

            // 모든 엣지부터 제거 (노드보다 먼저 제거해야 함)
            foreach (var edge in _graphView.edges.ToList())
            {
                _graphView.RemoveElement(edge);
            }

            // 모든 노드 제거 (Entry 노드 포함)
            foreach (var node in _graphView.nodes.ToList())
            {
                _graphView.RemoveElement(node);
            }

            // Entry 노드 참조 명시적으로 초기화
            if (_graphView.entryNode != null)
            {
                Debug.Log("Entry 노드 참조 초기화");
                _graphView.entryNode = null;
            }

            // 내부 컬렉션 초기화
            _graphView.StateNodes.Clear();
            _transitionEdges.Clear();

            Debug.Log("그래프 완전 초기화 완료");
        }

        // 새 그래프 생성을 위한 메서드
        private void CreateNewGraph()
        {
            if (_graphView == null)
                return;

            Debug.Log("새 그래프 생성 시작");

            // AnyState 노드 생성 - 항상 새로 생성하도록 수정
            Debug.Log("AnyState 노드 생성 시작");
            _graphView.CreateAnyStateNode();
            _anyStateCreated = true;

            // 데이터에 AnyState가 없으면 추가
            if (!_dataAsset.StateEntries.Any(s => s.Id == Transition.ANY_STATE))
            {
                _dataAsset.StateEntries.Add(new StateEntry
                {
                    Id = Transition.ANY_STATE,
                    stateTypeName = "AnyState",
                    position = new Vector2(100, 100)
                });
                EditorUtility.SetDirty(_dataAsset);
            }

            // Entry 노드 생성
            Debug.Log("Entry 노드 생성 시작");
            _graphView.CreateEntryNode();

            if (_graphView.entryNode == null)
            {
                Debug.LogError("Entry 노드 생성 실패!");
                return;
            }

            // 상태 노드 생성
            for (int i = 0; i < _dataAsset.StateEntries.Count; i++)
            {
                var entry = _dataAsset.StateEntries[i];

                // 이미 생성된 AnyState는 건너뛰기
                if (entry.Id == Transition.ANY_STATE)
                {
                    Debug.Log("AnyState 노드가 이미 존재함 - 생성 건너뜀");
                    continue;
                }

                // 노드 생성
                var node = new StateNode(entry, i);

                // 위치와 크기 지정
                node.SetPosition(new Rect(entry.position, new Vector2(150, 100)));

                _graphView.AddElement(node);
                _graphView.StateNodes[entry.Id] = node;

                Debug.Log($"노드 생성됨: ID={entry.Id}, Type={entry.stateTypeName}");
            }

            // 트랜지션 엣지 생성
            foreach (var transition in _dataAsset.Transitions)
            {
                CreateEdgeFromTransition(transition);
            }

            // Entry 노드 연결 처리
            if (_graphView.entryNode != null)
            {
                var regularNodes = _graphView.StateNodes.Values
                    .OfType<StateNode>()
                    .Where(n => n.entry.Id != Transition.ANY_STATE)
                    .ToList();

                if (regularNodes.Count > 0)
                {
                    // 지정된 초기 상태 ID 또는 첫 번째 노드로 연결
                    int targetId = initialStateId;
                    if (targetId <= 0 || !regularNodes.Any(n => n.entry.Id == targetId))
                    {
                        targetId = regularNodes.OrderBy(n => n.entry.Id).First().entry.Id;
                        Debug.Log($"초기 상태 ID({initialStateId})가 유효하지 않아 ID {targetId}를 사용");
                    }
                    else
                    {
                        Debug.Log($"저장된 초기 상태 ID {targetId} 사용");
                    }

                    // Entry 연결
                    _graphView.ConnectEntryToState(targetId);

                    // initialStateId 업데이트
                    initialStateId = targetId;
                }
                else
                {
                    Debug.LogWarning("연결할 일반 노드가 없습니다.");
                }
            }
            else
            {
                Debug.LogError("Entry 노드가 null입니다!");
            }

            Debug.Log("새 그래프 생성 완료");
        }
        // 4. New FSM - 새 FSM 생성
        private void CreateNewFSM()
        {
            if (HasUnsavedChanges())
            {
                bool save = EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    "Current FSM has unsaved changes. Save before creating a new FSM?",
                    "Save", "Discard");

                if (save)
                    SaveFSM();
            }

            // ID 초기화
            _currentFsmId = "NewFSM";

            // 로드 상태 초기화
            _originalAsset = null;

            // 빈 에셋 생성
            CreateEmptyAsset();

            Debug.Log("Created new FSM");
        }

        // 5. Delete FSM - 파일 선택 대화상자로 FSM 삭제
        private void DeleteFSMFromFile()
        {
            string defaultPath = "Assets/_Project/AddressableResources/Remote/FSMData";
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Select FSM to Delete",
                defaultPath,
                new[] { "FSM Assets", "asset" });

            if (string.IsNullOrEmpty(path))
                return; // 사용자가 취소함

            // 프로젝트 경로로 변환
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            FSMDataAsset assetToDelete = AssetDatabase.LoadAssetAtPath<FSMDataAsset>(path);
            if (assetToDelete == null)
            {
                EditorUtility.DisplayDialog("Delete Error", "Failed to find FSM asset.", "OK");
                return;
            }

            // 에셋 이름에서 FSM ID 추출
            string assetId = System.IO.Path.GetFileNameWithoutExtension(path);

            bool confirm = EditorUtility.DisplayDialog(
                "Delete FSM",
                $"Are you sure you want to delete FSM '{assetId}'? This cannot be undone.",
                "Delete", "Cancel");

            if (!confirm)
                return;

            // CollectionSO에서 제거
            _dataCollection.RemoveFSMData(assetId);
            EditorUtility.SetDirty(_dataCollection);

            // 에셋 파일 삭제
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();

            // 현재 작업중인 FSM이 삭제된 것과 같으면 새 FSM 생성
            if (_originalAsset == assetToDelete)
            {
                _currentFsmId = "NewFSM";
                _originalAsset = null;
                CreateEmptyAsset();
            }

            Debug.Log($"Deleted FSM: {assetId}");
        }

        // OnEnable 메서드에서 호출
        void OnEnable()
        {
            // 툴바 설정
            SetupToolbar();

            // 메인 컨테이너 생성
            var mainContainer = new VisualElement();
            mainContainer.style.flexGrow = 1;
            rootVisualElement.Add(mainContainer);

            // 지연 초기화 설정
            mainContainer.RegisterCallback<GeometryChangedEvent>(evt => {
                if (evt.newRect.width > 0 && evt.newRect.height > 0 && _graphView == null)
                {
                    Debug.Log($"컨테이너 크기 확인됨: {evt.newRect.width}x{evt.newRect.height}");
                    InitializeGraphView(mainContainer);
                    InitializeWithCollection(); // 컬렉션 초기화 및 FSM 로드
                }
            });
        }

        private bool HasUnsavedChanges()
        {
            // If we don't have an original asset for comparison, check if this is a new FSM with changes
            if (_originalAsset == null)
            {
                // Only consider it changed if the user added states beyond the default AnyState
                bool hasCustomStates = _dataAsset.StateEntries.Count > 1 ||
                                      _dataAsset.StateEntries.Any(s => s.Id != Transition.ANY_STATE);
                bool hasTransitions = _dataAsset.Transitions.Count > 0;

                return hasCustomStates || hasTransitions;
            }

            // Compare with original asset to detect actual changes
            if (_dataAsset.StateEntries.Count != _originalAsset.StateEntries.Count ||
                _dataAsset.Transitions.Count != _originalAsset.Transitions.Count)
            {
                return true;
            }

            // Check if any state entries have changed
            for (int i = 0; i < _dataAsset.StateEntries.Count; i++)
            {
                var current = _dataAsset.StateEntries[i];
                var original = _originalAsset.StateEntries.FirstOrDefault(s => s.Id == current.Id);

                if (original == null ||
                    original.stateTypeName != current.stateTypeName ||
                    Vector2.Distance(original.position, current.position) > 0.1f ||
                    !AreParametersEqual(original.Parameters, current.Parameters))
                {
                    return true;
                }
            }

            // Check if any transitions have changed
            for (int i = 0; i < _dataAsset.Transitions.Count; i++)
            {
                var current = _dataAsset.Transitions[i];
                var original = _originalAsset.Transitions.FirstOrDefault(t =>
                    t.FromStateId == current.FromStateId && t.ToStateId == current.ToStateId);

                if (original == null ||
                    original.Priority != current.Priority ||
                    !AreTriggersEqual(original.RequiredTriggers, current.RequiredTriggers) ||
                    !AreTriggersEqual(original.IgnoreTriggers, current.IgnoreTriggers))
                {
                    return true;
                }
            }

            return false;
        }
        private bool AreParametersEqual(List<SerializableParameter> a, List<SerializableParameter> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Name != b[i].Name ||
                    a[i].Type != b[i].Type ||
                    a[i].StringValue != b[i].StringValue)
                {
                    return true;
                }
            }

            return true;
        }

        private bool AreTriggersEqual(Trigger[] a, Trigger[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            var aSet = new HashSet<Trigger>(a);
            var bSet = new HashSet<Trigger>(b);

            return aSet.SetEquals(bSet);
        }
        // FSM 컬렉션 로드 또는 생성
        private void LoadOrCreateCollection()
        {
            _dataCollection = AssetDatabase.LoadAssetAtPath<FSMDataCollection>(k_CollectionPath);
            if (_dataCollection == null)
            {
                // 디렉토리 확인 및 생성
                string directory = Path.GetDirectoryName(k_CollectionPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }

                _dataCollection = ScriptableObject.CreateInstance<FSMDataCollection>();
                AssetDatabase.CreateAsset(_dataCollection, k_CollectionPath);
                AssetDatabase.SaveAssets();
            }

        }

        

        // FSM ID 필드 설정
        private void SetupFSMIdField(Toolbar toolbar)
        {
            _fsmIdField = new TextField("ID:");
            _fsmIdField.style.width = 150;
            _fsmIdField.SetValueWithoutNotify(_currentFsmId ?? "NewFSM");

            _fsmIdField.RegisterValueChangedCallback(evt => {
                if (!string.IsNullOrWhiteSpace(evt.newValue))
                {
                    _currentFsmId = evt.newValue;
                }
                else
                {
                    _fsmIdField.SetValueWithoutNotify(_currentFsmId ?? "NewFSM"); // 빈 값 방지
                }
            });

            toolbar.Add(_fsmIdField);
        }
        private void InitializeWithCollection()
        {
            // FSM 컬렉션 로드/생성
            LoadOrCreateCollection();

            CreateEmptyAsset();
        }
        private void CreateEmptyAsset()
        {
            // 이미 로드된 데이터가 있으면 초기화
            if (_dataAsset != null)
            {
                _dataAsset.StateEntries.Clear();
                _dataAsset.Transitions.Clear();
            }
            else
            {
                _dataAsset = ScriptableObject.CreateInstance<FSMDataAsset>();
                _dataAsset.StateEntries = new List<StateEntry>();
                _dataAsset.Transitions = new List<TransitionEntry>();
            }

            // AnyState가 없는 경우에만 추가 (중복 생성 방지)
            if (!_dataAsset.StateEntries.Any(s => s.Id == Transition.ANY_STATE))
            {
                _dataAsset.StateEntries.Add(new StateEntry
                {
                    Id = Transition.ANY_STATE,
                    stateTypeName = "AnyState",
                    position = new Vector2(100, 100)
                });
            }

            _dataObject = new SerializedObject(_dataAsset);

            // 그래프 초기화
            if (_graphView != null)
            {
                _anyStateCreated = false; // AnyState 생성 플래그 리셋
                CleanupEntryTransitions();
                PopulateGraph();
            }

            // 선택 초기화
            _currentSelectionType = SelectionType.None;
            _currentSelectedItem = null;
            ClearInspector();
        }

        private void InitializeGraphView(VisualElement container)
        {
            Debug.Log("GraphView 초기화 시작");

            // GraphView 생성
            _graphView = new FSMGraphView();
            _graphView.style.flexGrow = 1;

            // Entry 노드 생성
            _graphView.CreateEntryNode();

            _graphView.OnMultiSelectionChanged = ShowMultiSelectionMessage;
            _graphView.OnSelectionCleared = ClearInspector;

            // 그래프 변경 이벤트 처리
            _graphView.graphViewChanged += OnGraphViewChanged;

            // 2패널 분할 생성
            var split = new TwoPaneSplitView(0, 1200, TwoPaneSplitViewOrientation.Horizontal);
            split.Add(_graphView);
            _inspector = new ScrollView();
            split.Add(_inspector);

            // 컨테이너에 추가
            container.Add(split);

            Debug.Log($"GraphView 추가됨, 레이아웃: {_graphView.layout}");

            // 레이아웃 업데이트 확인
            _graphView.RegisterCallback<GeometryChangedEvent>(evt => {
                Debug.Log($"GraphView 크기 업데이트: {evt.newRect.width}x{evt.newRect.height}");
            });
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange changes)
        {
            if (changes.edgesToCreate != null)
            {
                changes.edgesToCreate.RemoveAll(edge =>
                edge.output.node is EntryNode || // Entry에서 시작하는 모든 연결 제외
                edge.input.node is EntryNode     // Entry로 들어오는 모든 연결 제외
                );

                foreach (var edge in changes.edgesToCreate)
                {
                    if (edge.output.node is StateNode fromNode && edge.input.node is StateNode toNode)
                    {
                        CreateTransitionEntry(fromNode.entry.Id, toNode.entry.Id);
                    }
                }
            }

            if (changes.elementsToRemove != null)
            {
                // Entry 트랜지션 및 Entry 노드 삭제 방지
                var keep = new List<GraphElement>();
                var remove = new List<GraphElement>();

                foreach (var element in changes.elementsToRemove)
                {
                    // EntryNode나 EntryTransitionEdge는 제거하지 않음
                    if (element is EntryNode || element is EntryTransitionEdge)
                    {
                        remove.Add(element);
                        Debug.Log("Entry 노드나 트랜지션은 삭제가 불가능합니다.");
                    }
                    else if (element is StateNode stateNode)
                    {
                        // AnyState 처리 추가
                        if (stateNode.entry.Id == Transition.ANY_STATE)
                        {
                            remove.Add(element);
                            Debug.Log("AnyState 노드는 삭제가 불가능합니다.");
                        }
                        else
                        {
                            // 일반 노드 삭제 처리
                            if (_graphView.StateNodes.ContainsKey(stateNode.entry.Id))
                            {
                                _graphView.StateNodes.Remove(stateNode.entry.Id);
                                Debug.Log($"노드 제거됨: ID={stateNode.entry.Id}, Type={stateNode.entry.stateTypeName}");

                                // 현재 선택된 항목이 삭제된 노드라면 선택 초기화
                                if (_currentSelectionType == SelectionType.Node &&
                                    _currentSelectedItem == stateNode)
                                {
                                    _currentSelectionType = SelectionType.None;
                                    _currentSelectedItem = null;
                                    ClearInspector();
                                }
                            }

                            // 상태 노드 삭제 시 StateEntry도 제거
                            var entryToRemove = _dataAsset.StateEntries.FirstOrDefault(s => s.Id == stateNode.entry.Id);
                            if (entryToRemove != null)
                            {
                                _dataAsset.StateEntries.Remove(entryToRemove);
                                EditorUtility.SetDirty(_dataAsset);
                                Debug.Log($"StateEntry 제거됨: ID={stateNode.entry.Id}");
                            }

                            // 관련된 트랜지션 모두 삭제
                            var transitionsToRemove = _dataAsset.Transitions
                                .Where(t => t.FromStateId == stateNode.entry.Id || t.ToStateId == stateNode.entry.Id)
                                .ToList();

                            foreach (var transition in transitionsToRemove)
                            {
                                RemoveTransitionEntry(transition);
                                Debug.Log($"관련된 트랜지션 제거됨: FromID={transition.FromStateId}, ToID={transition.ToStateId}");
                            }

                            // 이 노드가 Entry와 연결되어 있던 노드였다면, 새로운 초기 상태 자동 설정
                            bool isEntryConnected = false;
                            if (_graphView.entryNode != null)
                            {
                                foreach (var connection in _graphView.entryNode.OutputPort.connections)
                                {
                                    if (connection.input.node == stateNode)
                                    {
                                        isEntryConnected = true;
                                        break;
                                    }
                                }
                            }

                            if (isEntryConnected)
                            {
                                // 삭제 후 남은 노드 중 ID가 가장 작은 일반 노드 찾기
                                var remainingNodes = _graphView.StateNodes.Values
                                    .OfType<StateNode>()
                                    .Where(n => n.entry.Id != Transition.ANY_STATE && n != stateNode)
                                    .ToList();

                                if (remainingNodes.Count > 0)
                                {
                                    // ID가 가장 작은 노드 찾기
                                    var newInitialNode = remainingNodes
                                        .OrderBy(n => n.entry.Id)
                                        .First();
                                    SetInitializeNode(newInitialNode.entry.Id);

                                    Debug.Log($"Entry 연결이 ID={newInitialNode.entry.Id} 노드로 자동 이동됨");
                                }
                                else
                                {
                                    // 더 이상 남은 일반 노드가 없으면 Entry 연결 제거
                                    EditorApplication.delayCall += () => {
                                        CleanupEntryTransitions();
                                    };

                                    // 초기 상태 ID 리셋
                                    initialStateId = 0;
                                }
                            }

                            keep.Add(element);
                        }
                    }
                    else if (element is TransitionEdge transitionEdge)
                    {
                        // 일반 트랜지션 또는 AnyState 트랜지션 처리
                        TransitionEntry transitionToRemove = null;

                        // AnyState에서 시작하는 트랜지션인지 확인
                        bool isFromAnyState = transitionEdge.FromStateId == Transition.ANY_STATE;

                        // 실제 트랜지션 데이터 찾기
                        transitionToRemove = _dataAsset.Transitions.FirstOrDefault(
                            t => t.FromStateId == transitionEdge.FromStateId &&
                                 t.ToStateId == transitionEdge.ToStateId);

                        if (transitionToRemove != null)
                        {
                            RemoveTransitionEntry(transitionToRemove);

                            // 현재 선택된 항목이 삭제된 엣지라면 선택 초기화
                            if (_currentSelectionType == SelectionType.Edge &&
                                _currentSelectedItem == transitionEdge)
                            {
                                _currentSelectionType = SelectionType.None;
                                _currentSelectedItem = null;
                                ClearInspector();
                            }

                            string edgeId = $"{transitionEdge.FromStateId}-{transitionEdge.ToStateId}";
                            if (_transitionEdges.ContainsKey(edgeId))
                            {
                                _transitionEdges.Remove(edgeId);
                            }

                            Debug.Log($"트랜지션 제거됨: FromID={transitionEdge.FromStateId}, ToID={transitionEdge.ToStateId}");

                            keep.Add(element);
                        }
                        else
                        {
                            remove.Add(element);
                        }
                    }
                    else
                    {
                        // 기타 요소는 정상 삭제
                        keep.Add(element);
                    }
                }

                // 삭제 목록 업데이트 - 제거된 요소만 남김
                changes.elementsToRemove.Clear();
                keep.ForEach(e => changes.elementsToRemove.Add(e));
            }

            return changes;
        }
        private void CleanupEntryTransitions()
        {
            if (_graphView == null || _graphView.entryNode == null)
                return;

            // Entry 노드의 모든 연결 제거
            var existingEdges = _graphView.entryNode.OutputPort.connections.ToList();
            foreach (var edge in existingEdges)
            {
                // Edge를 제거하기 전에 포트 연결 해제
                if (edge.output != null)
                {
                    edge.output.Disconnect(edge);
                }

                if (edge.input != null)
                {
                    edge.input.Disconnect(edge);
                }

                // 그래프에서 엣지 제거
                _graphView.RemoveElement(edge);
            }

            Debug.Log("Entry 트랜지션이 정리됨");
        }

        private void ClearInspector()
        {
            _inspector.Clear();

            var label = new Label("No selection");
            label.style.fontSize = 14;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop = 20;
            _inspector.Add(label);
        }

        void CreateTransitionEntry(int fromStateId, int toStateId)
        {
            var transition = new TransitionEntry
            {
                FromStateId = fromStateId,
                ToStateId = toStateId,
                RequiredTriggers = new Trigger[] { Trigger.None },
                IgnoreTriggers = new Trigger[0],
                Priority = 0
            };

            _dataAsset.Transitions.Add(transition);
            EditorUtility.SetDirty(_dataAsset);
            _dataObject.Update();

            // 새 트랜지션용 엣지 생성
            CreateEdgeFromTransition(transition);

            // 관련 노드의 인스펙터 갱신
            if (_currentSelectionType == SelectionType.Node)
            {
                var selectedNode = _currentSelectedItem as StateNode;
                if (selectedNode != null && selectedNode.entry.Id == fromStateId)
                {
                    ShowNodeInspector(selectedNode);
                }
            }
        }

        public void RemoveTransitionEntry(TransitionEntry transition)
        {
            _dataAsset.Transitions.Remove(transition);
            EditorUtility.SetDirty(_dataAsset);
            _dataObject.Update();

            // 관련 노드의 인스펙터 갱신
            if (_currentSelectionType == SelectionType.Node)
            {
                var selectedNode = _currentSelectedItem as StateNode;
                if (selectedNode != null &&
                    (selectedNode.entry.Id == transition.FromStateId ||
                     selectedNode.entry.Id == transition.ToStateId))
                {
                    ShowNodeInspector(selectedNode);
                }
            }
        }

        public void LoadOrCreateAsset()
        {
            // 원본 에셋 로드
            _originalAsset = AssetDatabase.LoadAssetAtPath<FSMDataAsset>(k_AssetPath);

            if (_originalAsset == null)
            {
                // 새로운 에셋 생성
                _dataAsset = ScriptableObject.CreateInstance<FSMDataAsset>();
                _dataAsset.StateEntries = new List<StateEntry>();
                _dataAsset.Transitions = new List<TransitionEntry>();

                // Any State는 기본으로 추가
                _dataAsset.StateEntries.Add(new StateEntry
                {
                    Id = Transition.ANY_STATE,
                    stateTypeName = "AnyState",
                    position = new Vector2(100, 100)
                });
            }
            else
            {
                // 원본 에셋의 복사본 생성
                _dataAsset = CloneFSMDataAsset(_originalAsset);
            }

            // 복사본에 대한 SerializedObject 생성
            _dataObject = new SerializedObject(_dataAsset);
        }

        // 그래프 갱신 - 전체 갱신 또는 특정 엣지만 갱신 옵션
        public void RefreshGraph(bool fullRefresh = true, List<TransitionEntry> edgesToRefresh = null)
        {
            if (_graphView == null) return;

            if (fullRefresh)
            {
                Debug.Log("그래프 전체 갱신 시작");

                // 선택 초기화
                _graphView.ClearSelection();
                _currentSelectionType = SelectionType.None;
                _currentSelectedItem = null;

                // 전체 초기화 (모든 노드와 엣지 삭제)
                _graphView.DeleteElements(_graphView.edges.ToList());
                _graphView.DeleteElements(_graphView.nodes.Where(n => !(n is StateNode node && node.entry.Id == Transition.ANY_STATE)).ToList());
                _graphView.StateNodes.Clear();
                _transitionEdges.Clear();

                // AnyState 노드 생성 - 한 번만 생성되도록 함
                if (!_anyStateCreated)
                {
                    _graphView.CreateAnyStateNode();
                    _anyStateCreated = true;

                    // 데이터에 AnyState가 없으면 추가
                    if (!_dataAsset.StateEntries.Any(s => s.Id == Transition.ANY_STATE))
                    {
                        _dataAsset.StateEntries.Add(new StateEntry
                        {
                            Id = Transition.ANY_STATE,
                            stateTypeName = "AnyState",
                            position = new Vector2(100, 100)
                        });
                        EditorUtility.SetDirty(_dataAsset);
                    }
                }
                else if (_graphView.AnyStateNode != null)
                {
                    // AnyState 노드를 딕셔너리에 등록 (다시 생성하지 않음)
                    _graphView.StateNodes[Transition.ANY_STATE] = _graphView.AnyStateNode;
                }

                if (_graphView.entryNode == null)
                {
                    _graphView.CreateEntryNode();
                }

                Debug.Log($"RefreshGraph - StateEntries count: {_dataAsset.StateEntries.Count}");

                // 상태 노드 생성
                for (int i = 0; i < _dataAsset.StateEntries.Count; i++)
                {
                    var entry = _dataAsset.StateEntries[i];

                    // 이미 생성된 AnyState는 건너뛰기
                    if (entry.Id == Transition.ANY_STATE && _graphView.AnyStateNode != null)
                    {
                        Debug.Log("AnyState 노드 이미 존재하여 생성 건너뜀");
                        continue;
                    }

                    // 노드 생성
                    var node = new StateNode(entry, i);

                    // 위치와 크기 지정
                    node.SetPosition(new Rect(entry.position, new Vector2(150, 100)));

                    // 선택 시 인스펙터에 표시
                    //node.OnInspectClicked = ShowNodeInspector;

                    _graphView.AddElement(node);
                    _graphView.StateNodes[entry.Id] = node;

                    Debug.Log($"노드 생성됨: ID={entry.Id}, Type={entry.stateTypeName}");
                }

                // 모든 트랜지션 엣지 생성
                foreach (var transition in _dataAsset.Transitions)
                {
                    CreateEdgeFromTransition(transition);
                }

                Debug.Log($"Entry 연결 시도: initialStateId = {initialStateId}");
                if (_graphView.entryNode != null)
                {
                    var regularNodes = _graphView.StateNodes.Values
                        .OfType<StateNode>()
                        .Where(n => n.entry.Id != Transition.ANY_STATE)
                        .ToList();

                    if (regularNodes.Count > 0)
                    {
                        // 지정된 초기 상태 ID 또는 첫 번째 노드로 연결
                        int targetId = initialStateId;
                        if (targetId <= 0 || !regularNodes.Any(n => n.entry.Id == targetId))
                        {
                            targetId = regularNodes.OrderBy(n => n.entry.Id).First().entry.Id;
                            Debug.Log($"초기 상태 ID({initialStateId})가 유효하지 않아 ID {targetId}를 사용");
                        }
                        else
                        {
                            Debug.Log($"저장된 초기 상태 ID {targetId} 사용");
                        }

                        // Entry 연결 (내부에서 기존 연결 정리)
                        _graphView.ConnectEntryToState(targetId);

                        // initialStateId 업데이트
                        initialStateId = targetId;
                    }
                    else
                    {
                        Debug.LogWarning("연결할 일반 노드가 없습니다.");
                        // 일반 노드가 없으면 기존 엔트리 연결 모두 삭제
                        CleanupEntryTransitions();
                    }
                }
                else
                {
                    Debug.LogError("Entry 노드가 null입니다!");
                }
            }
            else if (edgesToRefresh != null && edgesToRefresh.Count > 0)
            {
                Debug.Log($"특정 트랜지션만 갱신 - {edgesToRefresh.Count}개");

                // 특정 엣지들만 갱신
                foreach (var transition in edgesToRefresh)
                {
                    string edgeId = $"{transition.FromStateId}-{transition.ToStateId}";

                    // 기존 엣지가 있으면 삭제
                    if (_transitionEdges.TryGetValue(edgeId, out var oldEdge))
                    {
                        _graphView.RemoveElement(oldEdge);
                        _transitionEdges.Remove(edgeId);
                    }

                    // 새 엣지 생성
                    CreateEdgeFromTransition(transition);
                }
            }
            else
            {
                // 단순 라벨 업데이트인 경우 - 아무것도 하지 않음
                // 개별 트랜지션 업데이트는 UpdateTransitionLabel 메서드 사용
            }

            Debug.Log("그래프 갱신 완료");
        }

        public void PopulateGraph()
        {
            if (_graphView == null) return;

            Debug.Log("PopulateGraph 시작...");

            // 기존 노드와 엣지 제거
            _graphView.DeleteElements(_graphView.edges.ToList());
            _graphView.DeleteElements(_graphView.nodes.Where(n => !(n is EntryNode)).ToList());
            _graphView.StateNodes.Clear();
            _transitionEdges.Clear();

            // 데이터가 없으면 빈 그래프 표시
            if (_dataAsset == null)
            {
                Debug.LogWarning("FSM 데이터 애셋이 로드되지 않았습니다.");
                return;
            }

            // AnyState 노드 생성
            if (!_anyStateCreated)
            {
                _graphView.CreateAnyStateNode();
                _anyStateCreated = true;

                // 데이터에 AnyState가 없으면 추가
                if (!_dataAsset.StateEntries.Any(s => s.Id == Transition.ANY_STATE))
                {
                    _dataAsset.StateEntries.Add(new StateEntry
                    {
                        Id = Transition.ANY_STATE,
                        stateTypeName = "AnyState",
                        position = new Vector2(100, 100)
                    });
                    EditorUtility.SetDirty(_dataAsset);
                }
            }

            // Entry 노드가 없으면 생성
            if (_graphView.entryNode == null)
            {
                _graphView.CreateEntryNode();
            }

            Debug.Log($"PopulateGraph - StateEntries 개수: {_dataAsset.StateEntries.Count}");

            // 상태 노드 생성
            foreach (var entry in _dataAsset.StateEntries)
            {
                // 이미 생성된 AnyState는 건너뜀
                if (entry.Id == Transition.ANY_STATE && _anyStateCreated && _graphView.AnyStateNode != null)
                {
                    Debug.Log("AnyState 노드가 이미 존재함, 노드 딕셔너리에 등록");
                    _graphView.StateNodes[entry.Id] = _graphView.AnyStateNode;
                    continue;
                }

                // 노드 생성
                var node = new StateNode(entry, _dataAsset.StateEntries.IndexOf(entry));

                // 위치와 크기 설정
                node.SetPosition(new Rect(entry.position, new Vector2(150, 100)));

                _graphView.AddElement(node);
                _graphView.StateNodes[entry.Id] = node;

                Debug.Log($"노드 생성됨: ID={entry.Id}, Type={entry.stateTypeName}");
            }

            // 트랜지션 엣지 생성
            foreach (var transition in _dataAsset.Transitions)
            {
                CreateEdgeFromTransition(transition);
            }

            // Entry와 초기 상태 연결
            if (_graphView.entryNode != null && initialStateId >= 0 && _graphView.StateNodes.ContainsKey(initialStateId))
            {
                _graphView.ConnectEntryToState(initialStateId);
            }
            else if (_graphView.entryNode != null && _graphView.StateNodes.Count > 0)
            {
                // AnyState가 아닌 첫 번째 상태를 초기 상태로 설정
                var firstState = _graphView.StateNodes.Values
                    .FirstOrDefault(n => n is StateNode node && node.entry.Id != Transition.ANY_STATE);

                if (firstState != null)
                {
                    initialStateId = ((StateNode)firstState).entry.Id;
                    _graphView.ConnectEntryToState(initialStateId);
                }
            }

            Debug.Log("FSM 데이터로 그래프가 채워졌습니다.");
        }
        //노드 그래프 관련

        public void SetInitializeNode(int stateId)
        {
            // 유효한 상태인지 확인
            if (!_dataAsset.StateEntries.Any(s => s.Id == stateId) || stateId == Transition.ANY_STATE)
            {
                Debug.LogError($"유효하지 않은 초기화 상태 ID: {stateId}");
                return;
            }

            // 초기 상태 ID 업데이트
            initialStateId = stateId;

            // 그래프 뷰에서 연결 업데이트
            if (_graphView != null && _graphView.entryNode != null)
            {
                _graphView.ConnectEntryToState(stateId);
            }

            // 데이터 업데이트
            EditorUtility.SetDirty(_dataAsset);
        }


        public void CreateEdgeFromTransition(TransitionEntry transition)
        {
            StateNode fromNode;
            StateNode toNode;

            // From 노드 찾기
            if (transition.FromStateId == Transition.ANY_STATE)
            {
                fromNode = _graphView.AnyStateNode;

                if (fromNode == null)
                {
                    Debug.LogError("AnyState node is null, cannot create edge");
                    return;
                }
            }
            else if (!_graphView.StateNodes.TryGetValue(transition.FromStateId, out fromNode))
            {
                Debug.LogError($"Source node with ID {transition.FromStateId} not found");
                return;
            }

            // To 노드 찾기
            if (!_graphView.StateNodes.TryGetValue(transition.ToStateId, out toNode))
            {
                Debug.LogError($"Target node with ID {transition.ToStateId} not found");
                return;
            }

            // 엣지 생성
            var edge = new TransitionEdge(transition, fromNode.outputPort, toNode.inputPort);
            edge.output.Connect(edge);
            edge.input.Connect(edge);

            // 엣지에 트랜지션 데이터 연결
            string edgeId = $"{transition.FromStateId}-{transition.ToStateId}";
            _transitionEdges[edgeId] = edge;

            // 그래프 뷰에 추가
            _graphView.AddElement(edge);
        }

        // 노드 인스펙터 표시
        public void ShowNodeInspector(StateNode node)
        {
            // 선택 업데이트
            _currentSelectionType = SelectionType.Node;
            _currentSelectedItem = node;

            _inspector.Clear();
            var entry = node.entry;

            // SerializedObject를 이용해 프로퍼티 바인딩
            var so = new SerializedObject(_dataAsset);
            so.Update();

            // 리스트에서 해당 인덱스를 찾아 프로퍼티 가져오기
            var listProp = so.FindProperty("StateEntries");
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var elem = listProp.GetArrayElementAtIndex(i);
                if (elem.FindPropertyRelative("Id").intValue == entry.Id)
                {
                    // 헤더 추가
                    var header = new Label("State Properties");
                    header.style.fontSize = 16;
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.marginBottom = 10;
                    _inspector.Add(header);

                    // ID 필드 (읽기 전용)
                    var idProp = elem.FindPropertyRelative("Id");
                    var idField = new IntegerField("ID");
                    idField.SetValueWithoutNotify(idProp.intValue);
                    idField.SetEnabled(false); // 비활성화
                    _inspector.Add(idField);

                    // 타입 필드 (읽기 전용)
                    var typeNameProp = elem.FindPropertyRelative("stateTypeName");
                    var typeField = new TextField("Type");
                    typeField.SetValueWithoutNotify(typeNameProp.stringValue);
                    typeField.SetEnabled(false); // 비활성화
                    _inspector.Add(typeField);

                    // 상태 파라미터 인스펙터 추가
                    var stateInspector = new StateInspector(so, entry); 
                    stateInspector.BuildInspector(_inspector);

                    // 트랜지션 관련 UI 추가 (단순화된 버전)
                    if (entry.Id != Transition.ANY_STATE)
                    {
                        var transitionsLabel = new Label("Outgoing Transitions");
                        transitionsLabel.style.fontSize = 14;
                        transitionsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        transitionsLabel.style.marginTop = 10;
                        _inspector.Add(transitionsLabel);

                        var relatedTransitions = _dataAsset.Transitions
                            .Where(t => t.FromStateId == entry.Id)
                            .ToList();

                        if (relatedTransitions.Count == 0)
                        {
                            _inspector.Add(new Label("No outgoing transitions"));
                        }
                        else
                        {
                            foreach (var transition in relatedTransitions)
                            {
                                var targetState = _dataAsset.StateEntries
                                    .FirstOrDefault(s => s.Id == transition.ToStateId);

                                var targetName = targetState != null
                                    ? targetState.stateTypeName.Split('.').Last()
                                    : "Unknown";

                                var triggerNames = transition.RequiredTriggers != null && transition.RequiredTriggers.Length > 0
                                    ? string.Join(", ", transition.RequiredTriggers)
                                    : "None";

                                var transitionInfo = new Label($"→ {targetName} [{triggerNames}]");
                                transitionInfo.style.marginBottom = 5;
                                _inspector.Add(transitionInfo);
                            }
                        }
                    }

                    // 정보 설명
                    var infoLabel = new Label("(Drag from output port to create transition)");
                    infoLabel.style.fontSize = 10;
                    infoLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                    infoLabel.style.marginTop = 10;
                    _inspector.Add(infoLabel);

                    break;
                }
            }

            so.ApplyModifiedProperties();
        }

        // 트랜지션 인스펙터 표시 - 직접 편집 기능 포함
        public void ShowTransitionInspector(TransitionEdge edge)
        {
            // 선택 업데이트
            _currentSelectionType = SelectionType.Edge;
            _currentSelectedItem = edge;

            _inspector.Clear();

            // 헤더 추가
            var header = new Label("Transition Properties");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            _inspector.Add(header);

            var transition = edge.transitionData;

            // From 상태 (읽기 전용)
            var fromState = _dataAsset.StateEntries.FirstOrDefault(s => s.Id == transition.FromStateId);
            var fromStateName = fromState != null ? fromState.stateTypeName.Split('.').Last() : $"State {transition.FromStateId}";

            var fromField = new TextField("From");
            fromField.SetValueWithoutNotify(fromStateName);
            fromField.SetEnabled(false); // 비활성화
            _inspector.Add(fromField);

            // To 상태 (읽기 전용)
            var toState = _dataAsset.StateEntries.FirstOrDefault(s => s.Id == transition.ToStateId);
            var toStateName = toState != null ? toState.stateTypeName.Split('.').Last() : $"State {transition.ToStateId}";

            var toField = new TextField("To");
            toField.SetValueWithoutNotify(toStateName);
            toField.SetEnabled(false); // 비활성화
            _inspector.Add(toField);

            // 우선순위 (수정 가능)
            var priorityField = new IntegerField("Priority");
            priorityField.value = transition.Priority;
            priorityField.RegisterValueChangedCallback(evt => {
                transition.Priority = evt.newValue;
                EditorUtility.SetDirty(_dataAsset);
            });
            _inspector.Add(priorityField);



            Dictionary<Trigger, Toggle> requiredToggles = new Dictionary<Trigger, Toggle>();
            Dictionary<Trigger, Toggle> ignoreToggles = new Dictionary<Trigger, Toggle>();

            // 트리거 선택 UI
            _inspector.Add(new Label("Required Triggers:") { style = { marginTop = 10 } });
            var triggerValues = System.Enum.GetValues(typeof(Trigger));

            // 트리거 컨테이너 (그리드 레이아웃)
            var triggerContainer = new VisualElement();
            triggerContainer.style.flexDirection = FlexDirection.Row;
            triggerContainer.style.flexWrap = Wrap.Wrap;

            bool anyTriggerSelected = false;

            // --- Required Triggers UI 생성 ---
            foreach (Trigger trigger in triggerValues)
            {
                if (trigger == Trigger.None) continue;

                bool isSelected = (transition.RequiredTriggers != null &&
                                  transition.RequiredTriggers.Contains(trigger));

                bool isDisabled = (transition.IgnoreTriggers != null &&
                                  transition.IgnoreTriggers.Contains(trigger));

                if (isSelected) anyTriggerSelected = true;

                var toggle = new Toggle(trigger.ToString()) { value = isSelected };
                toggle.style.minWidth = 120;
                toggle.style.marginRight = 10;

                // 상대편에 이미 있으면 비활성화
                toggle.SetEnabled(!isDisabled);

                // 딕셔너리에 토글 저장
                requiredToggles[trigger] = toggle;

                toggle.RegisterValueChangedCallback(evt => {
                    if (evt.newValue)
                    {
                        // 트리거 추가
                        var triggers = transition.RequiredTriggers?.ToList() ?? new List<Trigger>();
                        if (!triggers.Contains(trigger))
                        {
                            triggers.Add(trigger);
                            transition.RequiredTriggers = triggers.ToArray();
                            EditorUtility.SetDirty(_dataAsset);

                            // 상대편 토글 비활성화
                            if (ignoreToggles.TryGetValue(trigger, out var ignoreToggle))
                            {
                                ignoreToggle.SetEnabled(false);
                                ignoreToggle.value = false;

                                // IgnoreTriggers에서도 제거
                                var ignoreTriggers = transition.IgnoreTriggers?.ToList() ?? new List<Trigger>();
                                if (ignoreTriggers.Contains(trigger))
                                {
                                    ignoreTriggers.Remove(trigger);
                                    transition.IgnoreTriggers = ignoreTriggers.ToArray();
                                    EditorUtility.SetDirty(_dataAsset);
                                }
                            }

                            // 엣지 라벨만 업데이트
                            UpdateTransitionLabel(transition);
                        }
                    }
                    else
                    {
                        // 트리거 제거
                        var triggers = transition.RequiredTriggers?.ToList() ?? new List<Trigger>();
                        if (triggers.Contains(trigger))
                        {
                            triggers.Remove(trigger);
                            transition.RequiredTriggers = triggers.ToArray();
                            EditorUtility.SetDirty(_dataAsset);

                            // 상대편 토글 활성화
                            if (ignoreToggles.TryGetValue(trigger, out var ignoreToggle))
                            {
                                ignoreToggle.SetEnabled(true);
                            }

                            // 엣지 라벨만 업데이트
                            UpdateTransitionLabel(transition);
                        }
                    }
                });

                triggerContainer.Add(toggle);
            }

            _inspector.Add(triggerContainer);

            if (!anyTriggerSelected)
            {
                var noneLabel = new Label("No triggers selected - transition always active");
                noneLabel.style.fontSize = 10;
                noneLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                noneLabel.style.marginTop = 5;
                _inspector.Add(noneLabel);
            }

            // --- Ignore Triggers UI 생성 ---
            _inspector.Add(new Label("Ignore Triggers:") { style = { marginTop = 10 } });

            // 무시 트리거 컨테이너 (그리드 레이아웃)
            var ignoreContainer = new VisualElement();
            ignoreContainer.style.flexDirection = FlexDirection.Row;
            ignoreContainer.style.flexWrap = Wrap.Wrap;

            foreach (Trigger trigger in triggerValues)
            {
                if (trigger == Trigger.None) continue;

                bool isSelected = (transition.IgnoreTriggers != null &&
                                  transition.IgnoreTriggers.Contains(trigger));

                bool isDisabled = (transition.RequiredTriggers != null &&
                                  transition.RequiredTriggers.Contains(trigger));

                var toggle = new Toggle(trigger.ToString()) { value = isSelected };
                toggle.style.minWidth = 120;
                toggle.style.marginRight = 10;

                // 상대편에 이미 있으면 비활성화
                toggle.SetEnabled(!isDisabled);

                // 딕셔너리에 토글 저장
                ignoreToggles[trigger] = toggle;

                toggle.RegisterValueChangedCallback(evt => {
                    if (evt.newValue)
                    {
                        // 무시 트리거 추가
                        var triggers = transition.IgnoreTriggers?.ToList() ?? new List<Trigger>();
                        if (!triggers.Contains(trigger))
                        {
                            triggers.Add(trigger);
                            transition.IgnoreTriggers = triggers.ToArray();
                            EditorUtility.SetDirty(_dataAsset);

                            // 상대편 토글 비활성화
                            if (requiredToggles.TryGetValue(trigger, out var requiredToggle))
                            {
                                requiredToggle.SetEnabled(false);
                                requiredToggle.value = false;

                                // RequiredTriggers에서도 제거
                                var requiredTriggers = transition.RequiredTriggers?.ToList() ?? new List<Trigger>();
                                if (requiredTriggers.Contains(trigger))
                                {
                                    requiredTriggers.Remove(trigger);
                                    transition.RequiredTriggers = requiredTriggers.ToArray();
                                    EditorUtility.SetDirty(_dataAsset);

                                    // 엣지 라벨 업데이트 (RequiredTriggers가 변경됨)
                                    UpdateTransitionLabel(transition);
                                }
                            }
                        }
                    }
                    else
                    {
                        // 무시 트리거 제거
                        var triggers = transition.IgnoreTriggers?.ToList() ?? new List<Trigger>();
                        if (triggers.Contains(trigger))
                        {
                            triggers.Remove(trigger);
                            transition.IgnoreTriggers = triggers.ToArray();
                            EditorUtility.SetDirty(_dataAsset);

                            // 상대편 토글 활성화
                            if (requiredToggles.TryGetValue(trigger, out var requiredToggle))
                            {
                                requiredToggle.SetEnabled(true);
                            }
                        }
                    }
                });

                ignoreContainer.Add(toggle);
            }

            _inspector.Add(ignoreContainer);

            // 삭제 버튼
            var deleteButton = new Button(() => {
                if (EditorUtility.DisplayDialog("Delete Transition",
                    $"Delete transition from {fromStateName} to {toStateName}?",
                    "Delete", "Cancel"))
                {
                    RemoveTransitionEntry(transition);

                    // 삭제 후 그래프에서도 제거
                    string edgeId = $"{transition.FromStateId}-{transition.ToStateId}";
                    if (_transitionEdges.ContainsKey(edgeId))
                    {
                        var edgeToRemove = _transitionEdges[edgeId];
                        _graphView.RemoveElement(edgeToRemove);
                        _transitionEdges.Remove(edgeId);
                    }

                    // 선택 초기화
                    _currentSelectionType = SelectionType.None;
                    _currentSelectedItem = null;
                    ClearInspector();
                }
            })
            { text = "Delete Transition" };

            deleteButton.style.marginTop = 20;

            _inspector.Add(deleteButton);
        }

        // FSM 데이터 복제 유틸리티 메서드들
        private FSMDataAsset CloneFSMDataAsset(FSMDataAsset original)
        {
            if (original == null) return null;

            var clone = ScriptableObject.CreateInstance<FSMDataAsset>();

            // StateEntries 복사
            clone.StateEntries = new List<StateEntry>();
            if (original.StateEntries != null)
            {
                foreach (var entry in original.StateEntries)
                {
                    clone.StateEntries.Add(CloneStateEntry(entry));
                }
            }

            // Transitions 복사
            clone.Transitions = new List<TransitionEntry>();
            if (original.Transitions != null)
            {
                foreach (var transition in original.Transitions)
                {
                    clone.Transitions.Add(CloneTransitionEntry(transition));
                }
            }

            return clone;
        }

        private StateEntry CloneStateEntry(StateEntry original)
        {
            if (original == null) return null;

            var clone = new StateEntry
            {
                Id = original.Id,
                stateTypeName = original.stateTypeName.Split('.').Last(),
                position = original.position
            };

            // Parameters 복사
            if (original.Parameters != null)
            {
                clone.Parameters = new List<SerializableParameter>();
                foreach (var param in original.Parameters)
                {
                    clone.Parameters.Add(CloneSerializableParameter(param));
                }
            }

            return clone;
        }

        private SerializableParameter CloneSerializableParameter(SerializableParameter original)
        {
            if (original == null) return null;

            return new SerializableParameter
            {
                Name = original.Name,
                Type = original.Type,
                StringValue = original.StringValue,
                ServiceInterfaceType = original.ServiceInterfaceType,   
                ServiceImplementationType = original.ServiceImplementationType
            };
        }

        private TransitionEntry CloneTransitionEntry(TransitionEntry original)
        {
            if (original == null) return null;

            var clone = new TransitionEntry
            {
                FromStateId = original.FromStateId,
                ToStateId = original.ToStateId,
                Priority = original.Priority
            };

            // RequiredTriggers 복사
            if (original.RequiredTriggers != null)
            {
                clone.RequiredTriggers = new Trigger[original.RequiredTriggers.Length];
                Array.Copy(original.RequiredTriggers, clone.RequiredTriggers, original.RequiredTriggers.Length);
            }

            // IgnoreTriggers 복사
            if (original.IgnoreTriggers != null)
            {
                clone.IgnoreTriggers = new Trigger[original.IgnoreTriggers.Length];
                Array.Copy(original.IgnoreTriggers, clone.IgnoreTriggers, original.IgnoreTriggers.Length);
            }

            return clone;
        }
        public void UpdateTransitionLabel(TransitionEntry transition)
        {
            // 트랜지션 엣지 찾기
            string edgeId = $"{transition.FromStateId}-{transition.ToStateId}";
            if (_transitionEdges.TryGetValue(edgeId, out var edge))
            {
                // 기존 라벨 찾기
                var existingLabels = edge.Query<Label>().ToList();
                Label infoLabel = null;

                // 두 번째 라벨이 트리거 정보 라벨 (첫 번째는 화살표 라벨)
                if (existingLabels.Count >= 2)
                {
                    infoLabel = existingLabels[1];
                }

                // 라벨 내용 업데이트
                if (infoLabel != null)
                {
                    if (transition.RequiredTriggers != null && transition.RequiredTriggers.Length > 0)
                    {
                        infoLabel.text = string.Join(", ", transition.RequiredTriggers);
                    }
                    else
                    {
                        infoLabel.text = "No Triggers";
                    }

                    Debug.Log($"트랜지션 라벨 업데이트됨: {transition.FromStateId} -> {transition.ToStateId}");
                }
            }
        }
        // 다중 선택 시 메시지 표시
        private void ShowMultiSelectionMessage()
        {
            // 선택 업데이트
            _currentSelectionType = SelectionType.Multiple;
            _currentSelectedItem = null;

            _inspector.Clear();

            var messageLabel = new Label("다중선택은 인스펙터 지원이 안됩니다");
            messageLabel.style.fontSize = 14;
            messageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            messageLabel.style.marginTop = 20;

            _inspector.Add(messageLabel);
        }

        public void CreateNode(Vector2 pos, string typeName)
        {
            // 고유 ID 생성 - 기존 ID와 충돌 방지
            int newId = 0;
            while (_dataAsset.StateEntries.Any(s => s.Id == newId))
            {
                newId++;
            }

            var newEntry = new StateEntry
            {
                Id = newId,
                stateTypeName = typeName,
                position = pos
            };

            // 데이터에 StateEntry 추가
            _dataAsset.StateEntries.Add(newEntry);
            EditorUtility.SetDirty(_dataAsset);
            _dataObject.Update();

            // 새 노드 생성 후 그래프에 직접 추가 (전체 그래프 리로드 없이 최적화)
            var node = new StateNode(newEntry, _dataAsset.StateEntries.Count - 1);
            node.SetPosition(new Rect(pos, new Vector2(150, 100)));

            _graphView.AddElement(node);
            _graphView.StateNodes[newId] = node;

            // 첫 번째 생성된 일반 노드(AnyState 제외)인 경우 Entry 자동 연결
            var regularNodes = _graphView.StateNodes.Values
                .OfType<StateNode>()
                .Where(n => n.entry.Id != Transition.ANY_STATE)
                .ToList();

            if (regularNodes.Count == 1 && _graphView.entryNode != null)
            {
                // 이 노드가 첫 번째 일반 노드라면 Entry에 자동 연결
                _graphView.ConnectEntryToState(newId);
                initialStateId = newId;
                Debug.Log($"첫 번째 일반 노드 생성됨 - Entry와 자동 연결: ID={newId}");
            }

            // 노드 추가 후 자동으로 선택
            _graphView.ClearSelection();
            _graphView.AddToSelection(node);
            ShowNodeInspector(node);

            Debug.Log($"새 노드 생성됨: ID={newId}, Type={typeName}");
        }
        public void UpdateNodePosition(int nodeId, Vector2 newPosition)
        {
            // 데이터에서 해당 노드 찾기
            var stateEntry = _dataAsset.StateEntries.FirstOrDefault(e => e.Id == nodeId);

            if (stateEntry != null)
            {
                // 위치 업데이트
                stateEntry.position = newPosition;

                // 데이터 변경 마킹
                EditorUtility.SetDirty(_dataAsset);
            }
        }

        void GenerateStateFactory()
        {
            var path = "Assets/_Project/Scripts/KFSM/Generated/StateFactory.cs";
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var sb = new StringBuilder();
            sb.AppendLine("// 자동 생성된 StateFactory - 수정 금지");
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Kylin.FSM");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class StateFactory");
            sb.AppendLine("    {");
            sb.AppendLine("        // 필드 캐시 - 매번 리플렉션을 사용하지 않도록 성능 최적화");
            sb.AppendLine("        private static Dictionary<string, Dictionary<string, FieldInfo>> _typeFieldsCache =");
            sb.AppendLine("            new Dictionary<string, Dictionary<string, FieldInfo>>();");
            sb.AppendLine();
            sb.AppendLine("        public static StateBase CreateState(StateEntry stateEntry)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (stateEntry == null || string.IsNullOrEmpty(stateEntry.stateTypeName))");
            sb.AppendLine("                return null;");
            sb.AppendLine();
            sb.AppendLine("            StateBase state = null;");
            sb.AppendLine();
            sb.AppendLine("            switch (stateEntry.stateTypeName)");
            sb.AppendLine("            {");
            // 리플렉션으로 모든 StateBase 파생 구체 타입 검색
            foreach (var type in TypeCache.GetTypesDerivedFrom<StateBase>()
                                         .Where(t => !t.IsAbstract && t.IsClass))
            {
                var shortName = type.Name;
                sb.AppendLine($"                case \"{shortName}\": state = new {shortName}(); break;");
            }
            sb.AppendLine("                default:");
            sb.AppendLine("                    Debug.LogError($\"Unknown stateType: {stateEntry.stateTypeName}\");");
            sb.AppendLine("                    return null;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            state.SetID(stateEntry.Id);");
            sb.AppendLine("            // 파라미터 값 설정");
            sb.AppendLine("            if (stateEntry.Parameters != null && stateEntry.Parameters.Count > 0)");
            sb.AppendLine("            {");
            sb.AppendLine("                InitializeStateParameters(state, stateEntry);");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            return state;");
            sb.AppendLine("        }");
            sb.AppendLine();
            // InitializeStateParameters 그대로 복사
            sb.AppendLine("        private static void InitializeStateParameters(StateBase state, StateEntry stateEntry)");
            sb.AppendLine("        {");
            sb.AppendLine("            Type stateType = state.GetType();");
            sb.AppendLine("            string typeName = stateType.FullName;");
            sb.AppendLine();
            sb.AppendLine("            if (!_typeFieldsCache.TryGetValue(typeName, out var fieldsDict))");
            sb.AppendLine("            {");
            sb.AppendLine("                fieldsDict = new Dictionary<string, FieldInfo>();");
            sb.AppendLine("                var currentType = stateType;");
            sb.AppendLine("                while (currentType != null && currentType != typeof(StateBase) && currentType != typeof(object))");
            sb.AppendLine("                {");
            sb.AppendLine("                    var fields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);");
            sb.AppendLine("                    foreach (var field in fields)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        if (field.GetCustomAttribute<SerializeField>() != null || field.IsPublic)");
            sb.AppendLine("                            fieldsDict[field.Name] = field;");
            sb.AppendLine("                    }");
            sb.AppendLine("                    currentType = currentType.BaseType;");
            sb.AppendLine("                }");
            sb.AppendLine("                _typeFieldsCache[typeName] = fieldsDict;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            foreach (var param in stateEntry.Parameters)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (fieldsDict.TryGetValue(param.Name, out var fieldInfo))");
            sb.AppendLine("                {");
            sb.AppendLine("                    try");
            sb.AppendLine("                    {");
            sb.AppendLine("                        var value = param.GetValue();");
            sb.AppendLine("                        fieldInfo.SetValue(state, value);");
            sb.AppendLine("                    }");
            sb.AppendLine("                    catch (Exception ex)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        Debug.LogError($\"Failed to set parameter {param.Name} on state {typeName}: {ex.Message}\");");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            // CreateStates 메서드 복사
            sb.AppendLine("        public static StateBase[] CreateStates(FSMDataAsset dataAsset)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (dataAsset.StateEntries == null || dataAsset.StateEntries.Count == 0)");
            sb.AppendLine("                return new StateBase[0];");
            sb.AppendLine();
            sb.AppendLine("            var list = new List<StateBase>();");
            sb.AppendLine("            foreach (var entry in dataAsset.StateEntries)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (entry.Id == Transition.ANY_STATE) continue;");
            sb.AppendLine("                var state = CreateState(entry);");
            sb.AppendLine("                list.Add(state);");
            sb.AppendLine("            }");
            sb.AppendLine("            return list.ToArray();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"Generated StateFactory at {path}");
        }
    }
}