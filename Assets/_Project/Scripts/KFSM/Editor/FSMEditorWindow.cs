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

            // �巡�� Ȯ�� ��� �� ���� ����
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());


            // ���� ���� �� ȣ��Ǵ� �̺�Ʈ ó��
            this.RegisterCallback<MouseUpEvent>(e =>
            {
                // ���� ī��Ʈ�� ���� �ٸ� ó��
                if (selection.Count > 1)
                {
                    // ���� ���� ó��
                    Debug.Log($"���� ���õ� - �׸� ��: {selection.Count}");
                    OnMultiSelectionChanged?.Invoke();
                    return;
                }
                if(selection.Count <= 0)
                {
                    //��� Ŭ���� �ν����� �ʱ�ȭ
                    OnSelectionCleared?.Invoke();
                }
                
            });
            // �׸��� ��� �߰�
            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);
        }
        public void CreateEntryNode()
        {
            // ���� Entry ��尡 ������ ����
            if (entryNode != null)
            {
                Debug.Log("���� Entry ��� ����");
                RemoveElement(entryNode);
                entryNode = null;
            }

            // �� Entry ��� ����
            entryNode = new EntryNode();
            entryNode.SetPosition(new Rect(300, 300, 100, 80));
            AddElement(entryNode);

            Debug.Log("�� Entry ��� ������");
        }
        public void ConnectEntryToState(int stateId)
        {
            Debug.Log($"ConnectEntryToState ȣ���: stateId={stateId}");

            if (entryNode == null)
            {
                Debug.LogError("Entry ��尡 �����ϴ�. ���� Entry ��带 �����ϼ���.");
                CreateEntryNode();
                if (entryNode == null)
                {
                    Debug.LogError("Entry ��� ���� ����");
                    return;
                }
            }

            if (!StateNodes.TryGetValue(stateId, out var targetNode))
            {
                Debug.LogError($"���� ID {stateId}�� ��带 ã�� �� �����ϴ�");
                return;
            }

            Debug.Log($"Entry �� ���� {stateId} ���� ����");

            try
            {
                // ���� Entry ���� ��� ����
                var existingEdges = entryNode.OutputPort.connections.ToList();
                foreach (var edge in existingEdges)
                {
                    if (edge.output != null)
                        edge.output.Disconnect(edge);

                    if (edge.input != null)
                        edge.input.Disconnect(edge);

                    RemoveElement(edge);
                    Debug.Log("���� Entry ���� ���ŵ�");
                }

                // �� ���� ����
                var entryEdge = new EntryTransitionEdge(entryNode.OutputPort, targetNode.inputPort);

                // ��Ʈ�� ����
                entryNode.OutputPort.Connect(entryEdge);
                targetNode.inputPort.Connect(entryEdge);

                // �׷����� �߰�
                AddElement(entryEdge);

                // �ʱ� ���� ID ������Ʈ
                entryNode.InitialStateId = stateId;

                Debug.Log($"Entry�� ���� ID {stateId}�� ���������� �����");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Entry ���� �� ���� �߻�: {ex.Message}\n{ex.StackTrace}");
            }
        }
        // AnyState ��� ���� - �̹� �����ϸ� �ƹ��͵� ���� ����
        public void CreateAnyStateNode()
        {
            // ���� AnyState ��尡 ������ ����
            if (AnyStateNode != null)
            {
                Debug.Log("���� AnyState ��� ����");
                RemoveElement(AnyStateNode);

                // StateNodes ��ųʸ������� ����
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
            AnyStateNode.capabilities &= ~Capabilities.Deletable; // ���� �Ұ����ϰ� ����
            AnyStateNode.style.backgroundColor = new StyleColor(new Color(0.6f, 0.4f, 0.7f, 0.8f)); // Ư���� ���� ����

            AddElement(AnyStateNode);
            StateNodes[Transition.ANY_STATE] = AnyStateNode;

            Debug.Log("AnyState ��� ������");
        }
        private void CleanupEntryConnections()
        {
            if (entryNode == null) return;

            Debug.Log("Entry ���� ���� ����");

            // ���� ���� ��� ã��
            var existingEdges = entryNode.OutputPort.connections.ToList();

            foreach (var edge in existingEdges)
            {
                // ��Ʈ ���� ����
                if (edge.output != null)
                {
                    edge.output.Disconnect(edge);
                }

                if (edge.input != null)
                {
                    edge.input.Disconnect(edge);
                }

                // �׷������� ���� ����
                RemoveElement(edge);
                Debug.Log("���� Entry ���� ���ŵ�");
            }

            // ��� ��Ʈ�� �������� Ȯ��
            if (entryNode.OutputPort.connections.Count() > 0)
            {
                Debug.LogWarning($"Entry ��� ��Ʈ�� ������ {entryNode.OutputPort.connections.Count()} ���� ������ �ֽ��ϴ�!");
            }
            else
            {
                Debug.Log("Entry ��� ��Ʈ�� ����ϰ� �����Ǿ����ϴ�.");
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            // Entry ����� OutputPort���� �����ϴ� ��� - �̹� ����� �׸��� ������ �� ����Ʈ ��ȯ
            if (startPort.node is EntryNode && startPort.direction == Direction.Output)
            {
                // �̹� ������ ������ �� ����Ʈ ��ȯ (�� �̻� ���� �Ұ���)
                if (startPort.connections.Any())
                {
                    return compatiblePorts; // �� ����Ʈ ��ȯ
                }
            }

            // EntryTransitionEdge�� �巡�׵Ǵ� ��� ����
            foreach (var connection in startPort.connections)
            {
                if (connection is EntryTransitionEdge)
                {
                    return compatiblePorts; // ��Ʈ�� Ʈ�������� ��� �巡�� ����
                }
            }

            // Entry ������ ���� �õ��� ��� (� ��忡���� Entry ���δ� ���� �Ұ���)
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
            else // Input ��Ʈ���� �����ϴ� ���
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
                // State Ÿ�� ��������
                var stateTypes = TypeCache.GetTypesDerivedFrom<StateBase>()
                    .Where(t => !t.IsAbstract && t != typeof(StateBase))
                    .ToArray();

                if (stateTypes.Length == 0)
                {
                    evt.menu.AppendAction("No State Types Found", _ => { }, DropdownMenuAction.Status.Disabled);
                }
                else
                {
                    // �޴� ��κ� Ÿ�� ������ ���� ����
                    var menuPathToTypes = new Dictionary<string, List<Type>>();

                    // �⺻ ��� (��Ʈ����Ʈ�� ���� Ÿ�Կ�)
                    const string defaultPath = "Create/State/Other";
                    menuPathToTypes[defaultPath] = new List<Type>();

                    foreach (var t in stateTypes)
                    {
                        // FSMContextFolder ��Ʈ����Ʈ ��������
                        var attr = t.GetCustomAttribute<FSMContextFolderAttribute>();
                        string menuPath = attr != null ? attr.MenuPath : defaultPath;

                        // �޴� ��ο� Ÿ�� �߰�
                        if (!menuPathToTypes.TryGetValue(menuPath, out var typeList))
                        {
                            typeList = new List<Type>();
                            menuPathToTypes[menuPath] = typeList;
                        }

                        typeList.Add(t);
                    }

                    // ��� �޴� ��θ� �����Ͽ� ���ĺ� ������ ǥ��
                    var sortedPaths = menuPathToTypes.Keys.OrderBy(path => path).ToList();

                    foreach (var menuPath in sortedPaths)
                    {
                        var types = menuPathToTypes[menuPath];

                        // �� ��ο� ���Ե� Ÿ�Ե��� ����
                        types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

                        foreach (var t in types)
                        {
                            // ��� ���� �׼� �߰� 
                            Vector2 position = evt.mousePosition; // ���콺 ��ġ�� ��� ����
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

            // �θ� �޼��� ȣ���Ͽ� �⺻ ���ؽ�Ʈ �޴� �׸� �߰�
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

        // ���� ������ ��� ����
        private enum SelectionType { None, Node, Edge, Multiple }
        private SelectionType _currentSelectionType = SelectionType.None;
        private object _currentSelectedItem = null;

        private FSMDataCollection _dataCollection;
        private string _currentFsmId = "NewFSM"; // ���� ���� ���� FSM�� ID
        private TextField _fsmIdField; // FSM ID�� ǥ���ϰ� ������ �� �ִ� �ʵ�
        private DropdownField _fsmSelector; // �����ϴ� FSM �߿��� ������ �� �ִ� ��Ӵٿ�
        private const string k_CollectionPath = "Assets/_Project/AddressableResources/Remote/FSMData/FSMDataCollection.asset";

        public int initialStateId;

        [MenuItem("Window/Custom Tools/Visual FSM Editor", false, 100)]
        public static void Open()
        {
            var window = GetWindow<FSMEditorWindow>("FSM Editor");
        }

        private void SetupToolbar()
        {
            // ���� ����
            var toolbar = new Toolbar();

            // 1. Save FSM ��ư
            toolbar.Add(new ToolbarButton(SaveFSM) { text = "Save FSM" });

            // 2. Save As ��ư (�ٸ� �̸����� ����)
            toolbar.Add(new ToolbarButton(SaveAsNewFSM) { text = "Save As" });

            // 3. Load FSM ��ư
            toolbar.Add(new ToolbarButton(LoadFSMFromFile) { text = "Load FSM" });

            // 4. New FSM ��ư
            toolbar.Add(new ToolbarButton(CreateNewFSM) { text = "New FSM" });

            // 5. Delete FSM ��ư
            toolbar.Add(new ToolbarButton(DeleteFSMFromFile) { text = "Delete FSM" });

            // 6. Generate State Factory ��ư
            toolbar.Add(new ToolbarButton(GenerateStateFactory) { text = "Generate Factory" });

            // ���ٸ� ��Ʈ ��ҿ� �߰�
            rootVisualElement.Add(toolbar);
        }

        // 1. Save FSM - ���� FSM ����
        private void SaveFSM()
        {
            if (_originalAsset != null)
            {
                // �̹� �ε�� Asset�� ������ �ٷ� ����
                SaveToExistingAsset(_originalAsset);
            }
            else
            {
                // �ű� �Ǵ� �ε���� ���� ��� �ͽ��÷η��� ���� ��ġ ����
                SaveAsNewFSM();
            }
        }

        // 2. Save As - �ٸ� �̸����� ����
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
                return; // ����ڰ� �����

            // ���� �̸����� FSM ID ����
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            _currentFsmId = fileName;

            // CollectionSO�� �̹� ���� ID�� �ִ��� Ȯ��
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

            // �� Asset ���� �� ����
            FSMDataAsset newAsset = ScriptableObject.CreateInstance<FSMDataAsset>();

            // ���� ������ ����
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

            // �ʱ� ���� ID ����
            newAsset.InitialStateId = initialStateId;

            // ���� ����
            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();

            // �� ������ ���� �ε�� �������� ����
            _originalAsset = newAsset;

            // FSM �÷��ǿ� �߰�/����
            _dataCollection.AddFSMData(_currentFsmId, newAsset);
            EditorUtility.SetDirty(_dataCollection);
            AssetDatabase.SaveAssets();

            Debug.Log($"FSM saved as: {path}");
        }

        // ���� ���¿� �����ϴ� ���� �޼���
        private void SaveToExistingAsset(FSMDataAsset asset)
        {
            // �ڿ� ����
            //GenerateTransitionConstants();

            // Asset �ʱ�ȭ
            asset.StateEntries.Clear();
            asset.Transitions.Clear();

            // ���� ������ ����
            foreach (var entry in _dataAsset.StateEntries)
            {
                asset.StateEntries.Add(CloneStateEntry(entry));
            }

            foreach (var transition in _dataAsset.Transitions)
            {
                asset.Transitions.Add(CloneTransitionEntry(transition));
            }

            // �ʱ� ���� ID ����
            asset.InitialStateId = initialStateId;

            // ���� ����
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            // CollectionSO ������Ʈ
            _dataCollection.AddFSMData(_currentFsmId, asset);
            EditorUtility.SetDirty(_dataCollection);
            AssetDatabase.SaveAssets();

            Debug.Log($"FSM '{_currentFsmId}' ���� �Ϸ�");
        }


        // 3. Load FSM - ���� ���� ��ȭ���ڷ� FSM �ҷ�����
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
                return; // ����ڰ� �����

            // ������Ʈ ��η� ��ȯ
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

            // FSM ID ����
            string assetName = System.IO.Path.GetFileNameWithoutExtension(path);
            _currentFsmId = assetName;

            // �߿�: �׷��� ��� ���� �ʱ�ȭ
            ResetGraphView();

            // �ε�� ������ ����
            _originalAsset = loadedAsset;
            _dataAsset = CloneFSMDataAsset(loadedAsset);
            _dataObject = new SerializedObject(_dataAsset);

            // �ʱ� ���� ID ����
            initialStateId = loadedAsset.InitialStateId;
            Debug.Log($"Loaded FSM Initial State ID: {initialStateId}");

            // AnyState ���� �÷��� ����
            _anyStateCreated = false;

            // �׷��� ���� ���� ����
            CreateNewGraph();

            Debug.Log($"Loaded FSM from: {path}");
        }
        private void ClearEntireGraph()
        {
            if (_graphView == null)
                return;

            Debug.Log("�׷��� ���� �ʱ�ȭ ����");

            // ���� ���� �ʱ�ȭ
            _graphView.ClearSelection();
            _currentSelectionType = SelectionType.None;
            _currentSelectedItem = null;
            ClearInspector();

            // �׷��� ������Ʈ ���Ÿ� ���� ���� ���纻 �����
            var allElements = new List<GraphElement>();

            // ���� ��� ���� �߰�
            allElements.AddRange(_graphView.edges.ToList());

            // �� ���� ��� ��� �߰�
            allElements.AddRange(_graphView.nodes.ToList());

            // ��� ������Ʈ ���� �õ�
            _graphView.DeleteElements(allElements);

            // Entry ��� ���� �ʱ�ȭ
            _graphView.entryNode = null;

            // ���� �÷��� �ʱ�ȭ
            _graphView.StateNodes.Clear();
            _transitionEdges.Clear();

            Debug.Log("�׷��� ���� �ʱ�ȭ �Ϸ�");
        }
        private void ResetGraphView()
        {
            if (_graphView == null)
                return;

            Debug.Log("�׷��� ���� �ʱ�ȭ ����");

            // ���� ���� �ʱ�ȭ
            _graphView.ClearSelection();
            _currentSelectionType = SelectionType.None;
            _currentSelectedItem = null;
            ClearInspector();

            // ��� �������� ���� (��庸�� ���� �����ؾ� ��)
            foreach (var edge in _graphView.edges.ToList())
            {
                _graphView.RemoveElement(edge);
            }

            // ��� ��� ���� (Entry ��� ����)
            foreach (var node in _graphView.nodes.ToList())
            {
                _graphView.RemoveElement(node);
            }

            // Entry ��� ���� ��������� �ʱ�ȭ
            if (_graphView.entryNode != null)
            {
                Debug.Log("Entry ��� ���� �ʱ�ȭ");
                _graphView.entryNode = null;
            }

            // ���� �÷��� �ʱ�ȭ
            _graphView.StateNodes.Clear();
            _transitionEdges.Clear();

            Debug.Log("�׷��� ���� �ʱ�ȭ �Ϸ�");
        }

        // �� �׷��� ������ ���� �޼���
        private void CreateNewGraph()
        {
            if (_graphView == null)
                return;

            Debug.Log("�� �׷��� ���� ����");

            // AnyState ��� ���� - �׻� ���� �����ϵ��� ����
            Debug.Log("AnyState ��� ���� ����");
            _graphView.CreateAnyStateNode();
            _anyStateCreated = true;

            // �����Ϳ� AnyState�� ������ �߰�
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

            // Entry ��� ����
            Debug.Log("Entry ��� ���� ����");
            _graphView.CreateEntryNode();

            if (_graphView.entryNode == null)
            {
                Debug.LogError("Entry ��� ���� ����!");
                return;
            }

            // ���� ��� ����
            for (int i = 0; i < _dataAsset.StateEntries.Count; i++)
            {
                var entry = _dataAsset.StateEntries[i];

                // �̹� ������ AnyState�� �ǳʶٱ�
                if (entry.Id == Transition.ANY_STATE)
                {
                    Debug.Log("AnyState ��尡 �̹� ������ - ���� �ǳʶ�");
                    continue;
                }

                // ��� ����
                var node = new StateNode(entry, i);

                // ��ġ�� ũ�� ����
                node.SetPosition(new Rect(entry.position, new Vector2(150, 100)));

                _graphView.AddElement(node);
                _graphView.StateNodes[entry.Id] = node;

                Debug.Log($"��� ������: ID={entry.Id}, Type={entry.stateTypeName}");
            }

            // Ʈ������ ���� ����
            foreach (var transition in _dataAsset.Transitions)
            {
                CreateEdgeFromTransition(transition);
            }

            // Entry ��� ���� ó��
            if (_graphView.entryNode != null)
            {
                var regularNodes = _graphView.StateNodes.Values
                    .OfType<StateNode>()
                    .Where(n => n.entry.Id != Transition.ANY_STATE)
                    .ToList();

                if (regularNodes.Count > 0)
                {
                    // ������ �ʱ� ���� ID �Ǵ� ù ��° ���� ����
                    int targetId = initialStateId;
                    if (targetId <= 0 || !regularNodes.Any(n => n.entry.Id == targetId))
                    {
                        targetId = regularNodes.OrderBy(n => n.entry.Id).First().entry.Id;
                        Debug.Log($"�ʱ� ���� ID({initialStateId})�� ��ȿ���� �ʾ� ID {targetId}�� ���");
                    }
                    else
                    {
                        Debug.Log($"����� �ʱ� ���� ID {targetId} ���");
                    }

                    // Entry ����
                    _graphView.ConnectEntryToState(targetId);

                    // initialStateId ������Ʈ
                    initialStateId = targetId;
                }
                else
                {
                    Debug.LogWarning("������ �Ϲ� ��尡 �����ϴ�.");
                }
            }
            else
            {
                Debug.LogError("Entry ��尡 null�Դϴ�!");
            }

            Debug.Log("�� �׷��� ���� �Ϸ�");
        }
        // 4. New FSM - �� FSM ����
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

            // ID �ʱ�ȭ
            _currentFsmId = "NewFSM";

            // �ε� ���� �ʱ�ȭ
            _originalAsset = null;

            // �� ���� ����
            CreateEmptyAsset();

            Debug.Log("Created new FSM");
        }

        // 5. Delete FSM - ���� ���� ��ȭ���ڷ� FSM ����
        private void DeleteFSMFromFile()
        {
            string defaultPath = "Assets/_Project/AddressableResources/Remote/FSMData";
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Select FSM to Delete",
                defaultPath,
                new[] { "FSM Assets", "asset" });

            if (string.IsNullOrEmpty(path))
                return; // ����ڰ� �����

            // ������Ʈ ��η� ��ȯ
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

            // ���� �̸����� FSM ID ����
            string assetId = System.IO.Path.GetFileNameWithoutExtension(path);

            bool confirm = EditorUtility.DisplayDialog(
                "Delete FSM",
                $"Are you sure you want to delete FSM '{assetId}'? This cannot be undone.",
                "Delete", "Cancel");

            if (!confirm)
                return;

            // CollectionSO���� ����
            _dataCollection.RemoveFSMData(assetId);
            EditorUtility.SetDirty(_dataCollection);

            // ���� ���� ����
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();

            // ���� �۾����� FSM�� ������ �Ͱ� ������ �� FSM ����
            if (_originalAsset == assetToDelete)
            {
                _currentFsmId = "NewFSM";
                _originalAsset = null;
                CreateEmptyAsset();
            }

            Debug.Log($"Deleted FSM: {assetId}");
        }

        // OnEnable �޼��忡�� ȣ��
        void OnEnable()
        {
            // ���� ����
            SetupToolbar();

            // ���� �����̳� ����
            var mainContainer = new VisualElement();
            mainContainer.style.flexGrow = 1;
            rootVisualElement.Add(mainContainer);

            // ���� �ʱ�ȭ ����
            mainContainer.RegisterCallback<GeometryChangedEvent>(evt => {
                if (evt.newRect.width > 0 && evt.newRect.height > 0 && _graphView == null)
                {
                    Debug.Log($"�����̳� ũ�� Ȯ�ε�: {evt.newRect.width}x{evt.newRect.height}");
                    InitializeGraphView(mainContainer);
                    InitializeWithCollection(); // �÷��� �ʱ�ȭ �� FSM �ε�
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
        // FSM �÷��� �ε� �Ǵ� ����
        private void LoadOrCreateCollection()
        {
            _dataCollection = AssetDatabase.LoadAssetAtPath<FSMDataCollection>(k_CollectionPath);
            if (_dataCollection == null)
            {
                // ���丮 Ȯ�� �� ����
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

        

        // FSM ID �ʵ� ����
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
                    _fsmIdField.SetValueWithoutNotify(_currentFsmId ?? "NewFSM"); // �� �� ����
                }
            });

            toolbar.Add(_fsmIdField);
        }
        private void InitializeWithCollection()
        {
            // FSM �÷��� �ε�/����
            LoadOrCreateCollection();

            CreateEmptyAsset();
        }
        private void CreateEmptyAsset()
        {
            // �̹� �ε�� �����Ͱ� ������ �ʱ�ȭ
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

            // AnyState�� ���� ��쿡�� �߰� (�ߺ� ���� ����)
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

            // �׷��� �ʱ�ȭ
            if (_graphView != null)
            {
                _anyStateCreated = false; // AnyState ���� �÷��� ����
                CleanupEntryTransitions();
                PopulateGraph();
            }

            // ���� �ʱ�ȭ
            _currentSelectionType = SelectionType.None;
            _currentSelectedItem = null;
            ClearInspector();
        }

        private void InitializeGraphView(VisualElement container)
        {
            Debug.Log("GraphView �ʱ�ȭ ����");

            // GraphView ����
            _graphView = new FSMGraphView();
            _graphView.style.flexGrow = 1;

            // Entry ��� ����
            _graphView.CreateEntryNode();

            _graphView.OnMultiSelectionChanged = ShowMultiSelectionMessage;
            _graphView.OnSelectionCleared = ClearInspector;

            // �׷��� ���� �̺�Ʈ ó��
            _graphView.graphViewChanged += OnGraphViewChanged;

            // 2�г� ���� ����
            var split = new TwoPaneSplitView(0, 1200, TwoPaneSplitViewOrientation.Horizontal);
            split.Add(_graphView);
            _inspector = new ScrollView();
            split.Add(_inspector);

            // �����̳ʿ� �߰�
            container.Add(split);

            Debug.Log($"GraphView �߰���, ���̾ƿ�: {_graphView.layout}");

            // ���̾ƿ� ������Ʈ Ȯ��
            _graphView.RegisterCallback<GeometryChangedEvent>(evt => {
                Debug.Log($"GraphView ũ�� ������Ʈ: {evt.newRect.width}x{evt.newRect.height}");
            });
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange changes)
        {
            if (changes.edgesToCreate != null)
            {
                changes.edgesToCreate.RemoveAll(edge =>
                edge.output.node is EntryNode || // Entry���� �����ϴ� ��� ���� ����
                edge.input.node is EntryNode     // Entry�� ������ ��� ���� ����
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
                // Entry Ʈ������ �� Entry ��� ���� ����
                var keep = new List<GraphElement>();
                var remove = new List<GraphElement>();

                foreach (var element in changes.elementsToRemove)
                {
                    // EntryNode�� EntryTransitionEdge�� �������� ����
                    if (element is EntryNode || element is EntryTransitionEdge)
                    {
                        remove.Add(element);
                        Debug.Log("Entry ��峪 Ʈ�������� ������ �Ұ����մϴ�.");
                    }
                    else if (element is StateNode stateNode)
                    {
                        // AnyState ó�� �߰�
                        if (stateNode.entry.Id == Transition.ANY_STATE)
                        {
                            remove.Add(element);
                            Debug.Log("AnyState ���� ������ �Ұ����մϴ�.");
                        }
                        else
                        {
                            // �Ϲ� ��� ���� ó��
                            if (_graphView.StateNodes.ContainsKey(stateNode.entry.Id))
                            {
                                _graphView.StateNodes.Remove(stateNode.entry.Id);
                                Debug.Log($"��� ���ŵ�: ID={stateNode.entry.Id}, Type={stateNode.entry.stateTypeName}");

                                // ���� ���õ� �׸��� ������ ����� ���� �ʱ�ȭ
                                if (_currentSelectionType == SelectionType.Node &&
                                    _currentSelectedItem == stateNode)
                                {
                                    _currentSelectionType = SelectionType.None;
                                    _currentSelectedItem = null;
                                    ClearInspector();
                                }
                            }

                            // ���� ��� ���� �� StateEntry�� ����
                            var entryToRemove = _dataAsset.StateEntries.FirstOrDefault(s => s.Id == stateNode.entry.Id);
                            if (entryToRemove != null)
                            {
                                _dataAsset.StateEntries.Remove(entryToRemove);
                                EditorUtility.SetDirty(_dataAsset);
                                Debug.Log($"StateEntry ���ŵ�: ID={stateNode.entry.Id}");
                            }

                            // ���õ� Ʈ������ ��� ����
                            var transitionsToRemove = _dataAsset.Transitions
                                .Where(t => t.FromStateId == stateNode.entry.Id || t.ToStateId == stateNode.entry.Id)
                                .ToList();

                            foreach (var transition in transitionsToRemove)
                            {
                                RemoveTransitionEntry(transition);
                                Debug.Log($"���õ� Ʈ������ ���ŵ�: FromID={transition.FromStateId}, ToID={transition.ToStateId}");
                            }

                            // �� ��尡 Entry�� ����Ǿ� �ִ� ��忴�ٸ�, ���ο� �ʱ� ���� �ڵ� ����
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
                                // ���� �� ���� ��� �� ID�� ���� ���� �Ϲ� ��� ã��
                                var remainingNodes = _graphView.StateNodes.Values
                                    .OfType<StateNode>()
                                    .Where(n => n.entry.Id != Transition.ANY_STATE && n != stateNode)
                                    .ToList();

                                if (remainingNodes.Count > 0)
                                {
                                    // ID�� ���� ���� ��� ã��
                                    var newInitialNode = remainingNodes
                                        .OrderBy(n => n.entry.Id)
                                        .First();
                                    SetInitializeNode(newInitialNode.entry.Id);

                                    Debug.Log($"Entry ������ ID={newInitialNode.entry.Id} ���� �ڵ� �̵���");
                                }
                                else
                                {
                                    // �� �̻� ���� �Ϲ� ��尡 ������ Entry ���� ����
                                    EditorApplication.delayCall += () => {
                                        CleanupEntryTransitions();
                                    };

                                    // �ʱ� ���� ID ����
                                    initialStateId = 0;
                                }
                            }

                            keep.Add(element);
                        }
                    }
                    else if (element is TransitionEdge transitionEdge)
                    {
                        // �Ϲ� Ʈ������ �Ǵ� AnyState Ʈ������ ó��
                        TransitionEntry transitionToRemove = null;

                        // AnyState���� �����ϴ� Ʈ���������� Ȯ��
                        bool isFromAnyState = transitionEdge.FromStateId == Transition.ANY_STATE;

                        // ���� Ʈ������ ������ ã��
                        transitionToRemove = _dataAsset.Transitions.FirstOrDefault(
                            t => t.FromStateId == transitionEdge.FromStateId &&
                                 t.ToStateId == transitionEdge.ToStateId);

                        if (transitionToRemove != null)
                        {
                            RemoveTransitionEntry(transitionToRemove);

                            // ���� ���õ� �׸��� ������ ������� ���� �ʱ�ȭ
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

                            Debug.Log($"Ʈ������ ���ŵ�: FromID={transitionEdge.FromStateId}, ToID={transitionEdge.ToStateId}");

                            keep.Add(element);
                        }
                        else
                        {
                            remove.Add(element);
                        }
                    }
                    else
                    {
                        // ��Ÿ ��Ҵ� ���� ����
                        keep.Add(element);
                    }
                }

                // ���� ��� ������Ʈ - ���ŵ� ��Ҹ� ����
                changes.elementsToRemove.Clear();
                keep.ForEach(e => changes.elementsToRemove.Add(e));
            }

            return changes;
        }
        private void CleanupEntryTransitions()
        {
            if (_graphView == null || _graphView.entryNode == null)
                return;

            // Entry ����� ��� ���� ����
            var existingEdges = _graphView.entryNode.OutputPort.connections.ToList();
            foreach (var edge in existingEdges)
            {
                // Edge�� �����ϱ� ���� ��Ʈ ���� ����
                if (edge.output != null)
                {
                    edge.output.Disconnect(edge);
                }

                if (edge.input != null)
                {
                    edge.input.Disconnect(edge);
                }

                // �׷������� ���� ����
                _graphView.RemoveElement(edge);
            }

            Debug.Log("Entry Ʈ�������� ������");
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

            // �� Ʈ�����ǿ� ���� ����
            CreateEdgeFromTransition(transition);

            // ���� ����� �ν����� ����
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

            // ���� ����� �ν����� ����
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
            // ���� ���� �ε�
            _originalAsset = AssetDatabase.LoadAssetAtPath<FSMDataAsset>(k_AssetPath);

            if (_originalAsset == null)
            {
                // ���ο� ���� ����
                _dataAsset = ScriptableObject.CreateInstance<FSMDataAsset>();
                _dataAsset.StateEntries = new List<StateEntry>();
                _dataAsset.Transitions = new List<TransitionEntry>();

                // Any State�� �⺻���� �߰�
                _dataAsset.StateEntries.Add(new StateEntry
                {
                    Id = Transition.ANY_STATE,
                    stateTypeName = "AnyState",
                    position = new Vector2(100, 100)
                });
            }
            else
            {
                // ���� ������ ���纻 ����
                _dataAsset = CloneFSMDataAsset(_originalAsset);
            }

            // ���纻�� ���� SerializedObject ����
            _dataObject = new SerializedObject(_dataAsset);
        }

        // �׷��� ���� - ��ü ���� �Ǵ� Ư�� ������ ���� �ɼ�
        public void RefreshGraph(bool fullRefresh = true, List<TransitionEntry> edgesToRefresh = null)
        {
            if (_graphView == null) return;

            if (fullRefresh)
            {
                Debug.Log("�׷��� ��ü ���� ����");

                // ���� �ʱ�ȭ
                _graphView.ClearSelection();
                _currentSelectionType = SelectionType.None;
                _currentSelectedItem = null;

                // ��ü �ʱ�ȭ (��� ���� ���� ����)
                _graphView.DeleteElements(_graphView.edges.ToList());
                _graphView.DeleteElements(_graphView.nodes.Where(n => !(n is StateNode node && node.entry.Id == Transition.ANY_STATE)).ToList());
                _graphView.StateNodes.Clear();
                _transitionEdges.Clear();

                // AnyState ��� ���� - �� ���� �����ǵ��� ��
                if (!_anyStateCreated)
                {
                    _graphView.CreateAnyStateNode();
                    _anyStateCreated = true;

                    // �����Ϳ� AnyState�� ������ �߰�
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
                    // AnyState ��带 ��ųʸ��� ��� (�ٽ� �������� ����)
                    _graphView.StateNodes[Transition.ANY_STATE] = _graphView.AnyStateNode;
                }

                if (_graphView.entryNode == null)
                {
                    _graphView.CreateEntryNode();
                }

                Debug.Log($"RefreshGraph - StateEntries count: {_dataAsset.StateEntries.Count}");

                // ���� ��� ����
                for (int i = 0; i < _dataAsset.StateEntries.Count; i++)
                {
                    var entry = _dataAsset.StateEntries[i];

                    // �̹� ������ AnyState�� �ǳʶٱ�
                    if (entry.Id == Transition.ANY_STATE && _graphView.AnyStateNode != null)
                    {
                        Debug.Log("AnyState ��� �̹� �����Ͽ� ���� �ǳʶ�");
                        continue;
                    }

                    // ��� ����
                    var node = new StateNode(entry, i);

                    // ��ġ�� ũ�� ����
                    node.SetPosition(new Rect(entry.position, new Vector2(150, 100)));

                    // ���� �� �ν����Ϳ� ǥ��
                    //node.OnInspectClicked = ShowNodeInspector;

                    _graphView.AddElement(node);
                    _graphView.StateNodes[entry.Id] = node;

                    Debug.Log($"��� ������: ID={entry.Id}, Type={entry.stateTypeName}");
                }

                // ��� Ʈ������ ���� ����
                foreach (var transition in _dataAsset.Transitions)
                {
                    CreateEdgeFromTransition(transition);
                }

                Debug.Log($"Entry ���� �õ�: initialStateId = {initialStateId}");
                if (_graphView.entryNode != null)
                {
                    var regularNodes = _graphView.StateNodes.Values
                        .OfType<StateNode>()
                        .Where(n => n.entry.Id != Transition.ANY_STATE)
                        .ToList();

                    if (regularNodes.Count > 0)
                    {
                        // ������ �ʱ� ���� ID �Ǵ� ù ��° ���� ����
                        int targetId = initialStateId;
                        if (targetId <= 0 || !regularNodes.Any(n => n.entry.Id == targetId))
                        {
                            targetId = regularNodes.OrderBy(n => n.entry.Id).First().entry.Id;
                            Debug.Log($"�ʱ� ���� ID({initialStateId})�� ��ȿ���� �ʾ� ID {targetId}�� ���");
                        }
                        else
                        {
                            Debug.Log($"����� �ʱ� ���� ID {targetId} ���");
                        }

                        // Entry ���� (���ο��� ���� ���� ����)
                        _graphView.ConnectEntryToState(targetId);

                        // initialStateId ������Ʈ
                        initialStateId = targetId;
                    }
                    else
                    {
                        Debug.LogWarning("������ �Ϲ� ��尡 �����ϴ�.");
                        // �Ϲ� ��尡 ������ ���� ��Ʈ�� ���� ��� ����
                        CleanupEntryTransitions();
                    }
                }
                else
                {
                    Debug.LogError("Entry ��尡 null�Դϴ�!");
                }
            }
            else if (edgesToRefresh != null && edgesToRefresh.Count > 0)
            {
                Debug.Log($"Ư�� Ʈ�����Ǹ� ���� - {edgesToRefresh.Count}��");

                // Ư�� �����鸸 ����
                foreach (var transition in edgesToRefresh)
                {
                    string edgeId = $"{transition.FromStateId}-{transition.ToStateId}";

                    // ���� ������ ������ ����
                    if (_transitionEdges.TryGetValue(edgeId, out var oldEdge))
                    {
                        _graphView.RemoveElement(oldEdge);
                        _transitionEdges.Remove(edgeId);
                    }

                    // �� ���� ����
                    CreateEdgeFromTransition(transition);
                }
            }
            else
            {
                // �ܼ� �� ������Ʈ�� ��� - �ƹ��͵� ���� ����
                // ���� Ʈ������ ������Ʈ�� UpdateTransitionLabel �޼��� ���
            }

            Debug.Log("�׷��� ���� �Ϸ�");
        }

        public void PopulateGraph()
        {
            if (_graphView == null) return;

            Debug.Log("PopulateGraph ����...");

            // ���� ���� ���� ����
            _graphView.DeleteElements(_graphView.edges.ToList());
            _graphView.DeleteElements(_graphView.nodes.Where(n => !(n is EntryNode)).ToList());
            _graphView.StateNodes.Clear();
            _transitionEdges.Clear();

            // �����Ͱ� ������ �� �׷��� ǥ��
            if (_dataAsset == null)
            {
                Debug.LogWarning("FSM ������ �ּ��� �ε���� �ʾҽ��ϴ�.");
                return;
            }

            // AnyState ��� ����
            if (!_anyStateCreated)
            {
                _graphView.CreateAnyStateNode();
                _anyStateCreated = true;

                // �����Ϳ� AnyState�� ������ �߰�
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

            // Entry ��尡 ������ ����
            if (_graphView.entryNode == null)
            {
                _graphView.CreateEntryNode();
            }

            Debug.Log($"PopulateGraph - StateEntries ����: {_dataAsset.StateEntries.Count}");

            // ���� ��� ����
            foreach (var entry in _dataAsset.StateEntries)
            {
                // �̹� ������ AnyState�� �ǳʶ�
                if (entry.Id == Transition.ANY_STATE && _anyStateCreated && _graphView.AnyStateNode != null)
                {
                    Debug.Log("AnyState ��尡 �̹� ������, ��� ��ųʸ��� ���");
                    _graphView.StateNodes[entry.Id] = _graphView.AnyStateNode;
                    continue;
                }

                // ��� ����
                var node = new StateNode(entry, _dataAsset.StateEntries.IndexOf(entry));

                // ��ġ�� ũ�� ����
                node.SetPosition(new Rect(entry.position, new Vector2(150, 100)));

                _graphView.AddElement(node);
                _graphView.StateNodes[entry.Id] = node;

                Debug.Log($"��� ������: ID={entry.Id}, Type={entry.stateTypeName}");
            }

            // Ʈ������ ���� ����
            foreach (var transition in _dataAsset.Transitions)
            {
                CreateEdgeFromTransition(transition);
            }

            // Entry�� �ʱ� ���� ����
            if (_graphView.entryNode != null && initialStateId >= 0 && _graphView.StateNodes.ContainsKey(initialStateId))
            {
                _graphView.ConnectEntryToState(initialStateId);
            }
            else if (_graphView.entryNode != null && _graphView.StateNodes.Count > 0)
            {
                // AnyState�� �ƴ� ù ��° ���¸� �ʱ� ���·� ����
                var firstState = _graphView.StateNodes.Values
                    .FirstOrDefault(n => n is StateNode node && node.entry.Id != Transition.ANY_STATE);

                if (firstState != null)
                {
                    initialStateId = ((StateNode)firstState).entry.Id;
                    _graphView.ConnectEntryToState(initialStateId);
                }
            }

            Debug.Log("FSM �����ͷ� �׷����� ä�������ϴ�.");
        }
        //��� �׷��� ����

        public void SetInitializeNode(int stateId)
        {
            // ��ȿ�� �������� Ȯ��
            if (!_dataAsset.StateEntries.Any(s => s.Id == stateId) || stateId == Transition.ANY_STATE)
            {
                Debug.LogError($"��ȿ���� ���� �ʱ�ȭ ���� ID: {stateId}");
                return;
            }

            // �ʱ� ���� ID ������Ʈ
            initialStateId = stateId;

            // �׷��� �信�� ���� ������Ʈ
            if (_graphView != null && _graphView.entryNode != null)
            {
                _graphView.ConnectEntryToState(stateId);
            }

            // ������ ������Ʈ
            EditorUtility.SetDirty(_dataAsset);
        }


        public void CreateEdgeFromTransition(TransitionEntry transition)
        {
            StateNode fromNode;
            StateNode toNode;

            // From ��� ã��
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

            // To ��� ã��
            if (!_graphView.StateNodes.TryGetValue(transition.ToStateId, out toNode))
            {
                Debug.LogError($"Target node with ID {transition.ToStateId} not found");
                return;
            }

            // ���� ����
            var edge = new TransitionEdge(transition, fromNode.outputPort, toNode.inputPort);
            edge.output.Connect(edge);
            edge.input.Connect(edge);

            // ������ Ʈ������ ������ ����
            string edgeId = $"{transition.FromStateId}-{transition.ToStateId}";
            _transitionEdges[edgeId] = edge;

            // �׷��� �信 �߰�
            _graphView.AddElement(edge);
        }

        // ��� �ν����� ǥ��
        public void ShowNodeInspector(StateNode node)
        {
            // ���� ������Ʈ
            _currentSelectionType = SelectionType.Node;
            _currentSelectedItem = node;

            _inspector.Clear();
            var entry = node.entry;

            // SerializedObject�� �̿��� ������Ƽ ���ε�
            var so = new SerializedObject(_dataAsset);
            so.Update();

            // ����Ʈ���� �ش� �ε����� ã�� ������Ƽ ��������
            var listProp = so.FindProperty("StateEntries");
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var elem = listProp.GetArrayElementAtIndex(i);
                if (elem.FindPropertyRelative("Id").intValue == entry.Id)
                {
                    // ��� �߰�
                    var header = new Label("State Properties");
                    header.style.fontSize = 16;
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.marginBottom = 10;
                    _inspector.Add(header);

                    // ID �ʵ� (�б� ����)
                    var idProp = elem.FindPropertyRelative("Id");
                    var idField = new IntegerField("ID");
                    idField.SetValueWithoutNotify(idProp.intValue);
                    idField.SetEnabled(false); // ��Ȱ��ȭ
                    _inspector.Add(idField);

                    // Ÿ�� �ʵ� (�б� ����)
                    var typeNameProp = elem.FindPropertyRelative("stateTypeName");
                    var typeField = new TextField("Type");
                    typeField.SetValueWithoutNotify(typeNameProp.stringValue);
                    typeField.SetEnabled(false); // ��Ȱ��ȭ
                    _inspector.Add(typeField);

                    // ���� �Ķ���� �ν����� �߰�
                    var stateInspector = new StateInspector(so, entry); 
                    stateInspector.BuildInspector(_inspector);

                    // Ʈ������ ���� UI �߰� (�ܼ�ȭ�� ����)
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

                                var transitionInfo = new Label($"�� {targetName} [{triggerNames}]");
                                transitionInfo.style.marginBottom = 5;
                                _inspector.Add(transitionInfo);
                            }
                        }
                    }

                    // ���� ����
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

        // Ʈ������ �ν����� ǥ�� - ���� ���� ��� ����
        public void ShowTransitionInspector(TransitionEdge edge)
        {
            // ���� ������Ʈ
            _currentSelectionType = SelectionType.Edge;
            _currentSelectedItem = edge;

            _inspector.Clear();

            // ��� �߰�
            var header = new Label("Transition Properties");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            _inspector.Add(header);

            var transition = edge.transitionData;

            // From ���� (�б� ����)
            var fromState = _dataAsset.StateEntries.FirstOrDefault(s => s.Id == transition.FromStateId);
            var fromStateName = fromState != null ? fromState.stateTypeName.Split('.').Last() : $"State {transition.FromStateId}";

            var fromField = new TextField("From");
            fromField.SetValueWithoutNotify(fromStateName);
            fromField.SetEnabled(false); // ��Ȱ��ȭ
            _inspector.Add(fromField);

            // To ���� (�б� ����)
            var toState = _dataAsset.StateEntries.FirstOrDefault(s => s.Id == transition.ToStateId);
            var toStateName = toState != null ? toState.stateTypeName.Split('.').Last() : $"State {transition.ToStateId}";

            var toField = new TextField("To");
            toField.SetValueWithoutNotify(toStateName);
            toField.SetEnabled(false); // ��Ȱ��ȭ
            _inspector.Add(toField);

            // �켱���� (���� ����)
            var priorityField = new IntegerField("Priority");
            priorityField.value = transition.Priority;
            priorityField.RegisterValueChangedCallback(evt => {
                transition.Priority = evt.newValue;
                EditorUtility.SetDirty(_dataAsset);
            });
            _inspector.Add(priorityField);



            Dictionary<Trigger, Toggle> requiredToggles = new Dictionary<Trigger, Toggle>();
            Dictionary<Trigger, Toggle> ignoreToggles = new Dictionary<Trigger, Toggle>();

            // Ʈ���� ���� UI
            _inspector.Add(new Label("Required Triggers:") { style = { marginTop = 10 } });
            var triggerValues = System.Enum.GetValues(typeof(Trigger));

            // Ʈ���� �����̳� (�׸��� ���̾ƿ�)
            var triggerContainer = new VisualElement();
            triggerContainer.style.flexDirection = FlexDirection.Row;
            triggerContainer.style.flexWrap = Wrap.Wrap;

            bool anyTriggerSelected = false;

            // --- Required Triggers UI ���� ---
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

                // ����� �̹� ������ ��Ȱ��ȭ
                toggle.SetEnabled(!isDisabled);

                // ��ųʸ��� ��� ����
                requiredToggles[trigger] = toggle;

                toggle.RegisterValueChangedCallback(evt => {
                    if (evt.newValue)
                    {
                        // Ʈ���� �߰�
                        var triggers = transition.RequiredTriggers?.ToList() ?? new List<Trigger>();
                        if (!triggers.Contains(trigger))
                        {
                            triggers.Add(trigger);
                            transition.RequiredTriggers = triggers.ToArray();
                            EditorUtility.SetDirty(_dataAsset);

                            // ����� ��� ��Ȱ��ȭ
                            if (ignoreToggles.TryGetValue(trigger, out var ignoreToggle))
                            {
                                ignoreToggle.SetEnabled(false);
                                ignoreToggle.value = false;

                                // IgnoreTriggers������ ����
                                var ignoreTriggers = transition.IgnoreTriggers?.ToList() ?? new List<Trigger>();
                                if (ignoreTriggers.Contains(trigger))
                                {
                                    ignoreTriggers.Remove(trigger);
                                    transition.IgnoreTriggers = ignoreTriggers.ToArray();
                                    EditorUtility.SetDirty(_dataAsset);
                                }
                            }

                            // ���� �󺧸� ������Ʈ
                            UpdateTransitionLabel(transition);
                        }
                    }
                    else
                    {
                        // Ʈ���� ����
                        var triggers = transition.RequiredTriggers?.ToList() ?? new List<Trigger>();
                        if (triggers.Contains(trigger))
                        {
                            triggers.Remove(trigger);
                            transition.RequiredTriggers = triggers.ToArray();
                            EditorUtility.SetDirty(_dataAsset);

                            // ����� ��� Ȱ��ȭ
                            if (ignoreToggles.TryGetValue(trigger, out var ignoreToggle))
                            {
                                ignoreToggle.SetEnabled(true);
                            }

                            // ���� �󺧸� ������Ʈ
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

            // --- Ignore Triggers UI ���� ---
            _inspector.Add(new Label("Ignore Triggers:") { style = { marginTop = 10 } });

            // ���� Ʈ���� �����̳� (�׸��� ���̾ƿ�)
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

                // ����� �̹� ������ ��Ȱ��ȭ
                toggle.SetEnabled(!isDisabled);

                // ��ųʸ��� ��� ����
                ignoreToggles[trigger] = toggle;

                toggle.RegisterValueChangedCallback(evt => {
                    if (evt.newValue)
                    {
                        // ���� Ʈ���� �߰�
                        var triggers = transition.IgnoreTriggers?.ToList() ?? new List<Trigger>();
                        if (!triggers.Contains(trigger))
                        {
                            triggers.Add(trigger);
                            transition.IgnoreTriggers = triggers.ToArray();
                            EditorUtility.SetDirty(_dataAsset);

                            // ����� ��� ��Ȱ��ȭ
                            if (requiredToggles.TryGetValue(trigger, out var requiredToggle))
                            {
                                requiredToggle.SetEnabled(false);
                                requiredToggle.value = false;

                                // RequiredTriggers������ ����
                                var requiredTriggers = transition.RequiredTriggers?.ToList() ?? new List<Trigger>();
                                if (requiredTriggers.Contains(trigger))
                                {
                                    requiredTriggers.Remove(trigger);
                                    transition.RequiredTriggers = requiredTriggers.ToArray();
                                    EditorUtility.SetDirty(_dataAsset);

                                    // ���� �� ������Ʈ (RequiredTriggers�� �����)
                                    UpdateTransitionLabel(transition);
                                }
                            }
                        }
                    }
                    else
                    {
                        // ���� Ʈ���� ����
                        var triggers = transition.IgnoreTriggers?.ToList() ?? new List<Trigger>();
                        if (triggers.Contains(trigger))
                        {
                            triggers.Remove(trigger);
                            transition.IgnoreTriggers = triggers.ToArray();
                            EditorUtility.SetDirty(_dataAsset);

                            // ����� ��� Ȱ��ȭ
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

            // ���� ��ư
            var deleteButton = new Button(() => {
                if (EditorUtility.DisplayDialog("Delete Transition",
                    $"Delete transition from {fromStateName} to {toStateName}?",
                    "Delete", "Cancel"))
                {
                    RemoveTransitionEntry(transition);

                    // ���� �� �׷��������� ����
                    string edgeId = $"{transition.FromStateId}-{transition.ToStateId}";
                    if (_transitionEdges.ContainsKey(edgeId))
                    {
                        var edgeToRemove = _transitionEdges[edgeId];
                        _graphView.RemoveElement(edgeToRemove);
                        _transitionEdges.Remove(edgeId);
                    }

                    // ���� �ʱ�ȭ
                    _currentSelectionType = SelectionType.None;
                    _currentSelectedItem = null;
                    ClearInspector();
                }
            })
            { text = "Delete Transition" };

            deleteButton.style.marginTop = 20;

            _inspector.Add(deleteButton);
        }

        // FSM ������ ���� ��ƿ��Ƽ �޼����
        private FSMDataAsset CloneFSMDataAsset(FSMDataAsset original)
        {
            if (original == null) return null;

            var clone = ScriptableObject.CreateInstance<FSMDataAsset>();

            // StateEntries ����
            clone.StateEntries = new List<StateEntry>();
            if (original.StateEntries != null)
            {
                foreach (var entry in original.StateEntries)
                {
                    clone.StateEntries.Add(CloneStateEntry(entry));
                }
            }

            // Transitions ����
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

            // Parameters ����
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

            // RequiredTriggers ����
            if (original.RequiredTriggers != null)
            {
                clone.RequiredTriggers = new Trigger[original.RequiredTriggers.Length];
                Array.Copy(original.RequiredTriggers, clone.RequiredTriggers, original.RequiredTriggers.Length);
            }

            // IgnoreTriggers ����
            if (original.IgnoreTriggers != null)
            {
                clone.IgnoreTriggers = new Trigger[original.IgnoreTriggers.Length];
                Array.Copy(original.IgnoreTriggers, clone.IgnoreTriggers, original.IgnoreTriggers.Length);
            }

            return clone;
        }
        public void UpdateTransitionLabel(TransitionEntry transition)
        {
            // Ʈ������ ���� ã��
            string edgeId = $"{transition.FromStateId}-{transition.ToStateId}";
            if (_transitionEdges.TryGetValue(edgeId, out var edge))
            {
                // ���� �� ã��
                var existingLabels = edge.Query<Label>().ToList();
                Label infoLabel = null;

                // �� ��° ���� Ʈ���� ���� �� (ù ��°�� ȭ��ǥ ��)
                if (existingLabels.Count >= 2)
                {
                    infoLabel = existingLabels[1];
                }

                // �� ���� ������Ʈ
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

                    Debug.Log($"Ʈ������ �� ������Ʈ��: {transition.FromStateId} -> {transition.ToStateId}");
                }
            }
        }
        // ���� ���� �� �޽��� ǥ��
        private void ShowMultiSelectionMessage()
        {
            // ���� ������Ʈ
            _currentSelectionType = SelectionType.Multiple;
            _currentSelectedItem = null;

            _inspector.Clear();

            var messageLabel = new Label("���߼����� �ν����� ������ �ȵ˴ϴ�");
            messageLabel.style.fontSize = 14;
            messageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            messageLabel.style.marginTop = 20;

            _inspector.Add(messageLabel);
        }

        public void CreateNode(Vector2 pos, string typeName)
        {
            // ���� ID ���� - ���� ID�� �浹 ����
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

            // �����Ϳ� StateEntry �߰�
            _dataAsset.StateEntries.Add(newEntry);
            EditorUtility.SetDirty(_dataAsset);
            _dataObject.Update();

            // �� ��� ���� �� �׷����� ���� �߰� (��ü �׷��� ���ε� ���� ����ȭ)
            var node = new StateNode(newEntry, _dataAsset.StateEntries.Count - 1);
            node.SetPosition(new Rect(pos, new Vector2(150, 100)));

            _graphView.AddElement(node);
            _graphView.StateNodes[newId] = node;

            // ù ��° ������ �Ϲ� ���(AnyState ����)�� ��� Entry �ڵ� ����
            var regularNodes = _graphView.StateNodes.Values
                .OfType<StateNode>()
                .Where(n => n.entry.Id != Transition.ANY_STATE)
                .ToList();

            if (regularNodes.Count == 1 && _graphView.entryNode != null)
            {
                // �� ��尡 ù ��° �Ϲ� ����� Entry�� �ڵ� ����
                _graphView.ConnectEntryToState(newId);
                initialStateId = newId;
                Debug.Log($"ù ��° �Ϲ� ��� ������ - Entry�� �ڵ� ����: ID={newId}");
            }

            // ��� �߰� �� �ڵ����� ����
            _graphView.ClearSelection();
            _graphView.AddToSelection(node);
            ShowNodeInspector(node);

            Debug.Log($"�� ��� ������: ID={newId}, Type={typeName}");
        }
        public void UpdateNodePosition(int nodeId, Vector2 newPosition)
        {
            // �����Ϳ��� �ش� ��� ã��
            var stateEntry = _dataAsset.StateEntries.FirstOrDefault(e => e.Id == nodeId);

            if (stateEntry != null)
            {
                // ��ġ ������Ʈ
                stateEntry.position = newPosition;

                // ������ ���� ��ŷ
                EditorUtility.SetDirty(_dataAsset);
            }
        }

        void GenerateStateFactory()
        {
            var path = "Assets/_Project/Scripts/KFSM/Generated/StateFactory.cs";
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var sb = new StringBuilder();
            sb.AppendLine("// �ڵ� ������ StateFactory - ���� ����");
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Kylin.FSM");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class StateFactory");
            sb.AppendLine("    {");
            sb.AppendLine("        // �ʵ� ĳ�� - �Ź� ���÷����� ������� �ʵ��� ���� ����ȭ");
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
            // ���÷������� ��� StateBase �Ļ� ��ü Ÿ�� �˻�
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
            sb.AppendLine("            // �Ķ���� �� ����");
            sb.AppendLine("            if (stateEntry.Parameters != null && stateEntry.Parameters.Count > 0)");
            sb.AppendLine("            {");
            sb.AppendLine("                InitializeStateParameters(state, stateEntry);");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            return state;");
            sb.AppendLine("        }");
            sb.AppendLine();
            // InitializeStateParameters �״�� ����
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
            // CreateStates �޼��� ����
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