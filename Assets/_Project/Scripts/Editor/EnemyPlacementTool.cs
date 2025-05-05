using UnityEngine;
using UnityEditor;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEditorEventType = UnityEngine.EventType;
using Kylin.FSM;

public class EnemyPlacementTool : EditorWindow
{
    private int mapId = 0;
    private Vector2 mainScrollPosition; // ��ü â�� ���� ��ũ�� ��ġ
    private Vector2 enemyScrollPosition; // ���ʹ� ��� ��ũ�� ��ġ
    private Vector2 eventScrollPosition; // �̺�Ʈ ��� ��ũ�� ��ġ
    private EnemyCell[,] cells = new EnemyCell[10, 12]; // 10x12 �׸���
    private Vector2Int selectedCell = new Vector2Int(-1, -1);

    // �� ������ Ŭ����
    [System.Serializable]
    private class EnemyCell
    {
        public D_EnemyData enemy;
        public List<D_EventDummyData> events = new List<D_EventDummyData>(); // �̺�Ʈ ��� �߰�
        public bool isEmpty => enemy == null;
    }

    [MenuItem("Window/Custom Tools/Enemy Placement Tool", false, 100)]
    public static void Open()
    {
        var window = GetWindow<EnemyPlacementTool>("Enemy Placement Tool");
    }

    private void OnEnable()
    {
        // �� �ʱ�ȭ
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                cells[y, x] = new EnemyCell();
            }
        }

        // ���� ������ �ҷ�����
        //LoadDataIfExists();
    }

    private void OnGUI()
    {
        // ��ü â�� ���� ��ũ�� ����
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        // ���� �г� - MapID �Է� ����
        DrawLeftPanel();

        // �ణ�� ���� �߰�
        GUILayout.Space(10);

        // �߾� �г� - �׸��� ��ġ ����
        EditorGUILayout.BeginVertical(GUILayout.MinWidth(500), GUILayout.ExpandHeight(true));
        DrawGrid();
        EditorGUILayout.EndVertical();

        // �ణ�� ���� �߰�
        GUILayout.Space(10);

        // ������ �г� - ���ʹ� ���� �� �̺�Ʈ ���� ����
        EditorGUILayout.BeginVertical(GUILayout.Width(600), GUILayout.ExpandHeight(true));

        if (selectedCell.x >= 0 && selectedCell.y >= 0)
        {
            DrawEnemySelection();

            // ���ʹ̰� ���õ� ��쿡�� �̺�Ʈ ���� UI ǥ��
            if (!cells[selectedCell.y, selectedCell.x].isEmpty)
            {
                DrawEventSelection();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("���� �����ϸ� ���⿡ ���ʹ� ����� ǥ�õ˴ϴ�.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // �߰� ���� (�ϴ� ����)
        GUILayout.Space(20);

        // ��ü ��ũ�� ����
        EditorGUILayout.EndScrollView();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(180), GUILayout.ExpandHeight(true));

        GUILayout.Label("Enemy Placement Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(20);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Map ID:", GUILayout.Width(60));
        mapId = EditorGUILayout.IntField(mapId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Load Map"))
        {
            LoadDataIfExists();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Save Data"))
        {
            SaveData();
        }

        // �߰� ���� ��ư��
        EditorGUILayout.Space(20);

        if (GUILayout.Button("��� �� �ʱ�ȭ"))
        {
            if (EditorUtility.DisplayDialog("�ʱ�ȭ Ȯ��", "��� ���� �����͸� �ʱ�ȭ�Ͻðڽ��ϱ�?", "��", "�ƴϿ�"))
            {
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 12; x++)
                    {
                        cells[y, x].enemy = null;
                        cells[y, x].events.Clear();
                    }
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawGrid()
    {
        GUILayout.Label("��ġ �׸���", EditorStyles.boldLabel);

        // �� �κ��� �߿��մϴ� - �׸��带 �׸��� ���� ������ �̸� Ȯ��
        float cellSize = 40f; // �� ũ��
        Rect layoutRect = GUILayoutUtility.GetRect(12 * cellSize + 40, 10 * cellSize + 60);

        // �׸����� ���� ��ġ�� layoutRect �������� ����
        Rect gridRect = new Rect(layoutRect.x + 30, layoutRect.y + 20, cellSize * 12, cellSize * 10);

        // �׸��� ��� (���)
        EditorGUI.DrawRect(gridRect, Color.white);

        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                Rect cellRect = new Rect(gridRect.x + x * cellSize, gridRect.y + y * cellSize, cellSize, cellSize);

                // �� ��� ���� (��� ������ ����, ���ʹ̰� ������ ȸ��)
                if (!cells[y, x].isEmpty)
                {
                    // ���ʹ̰� �ִ� ���� ȸ������ ä���
                    EditorGUI.DrawRect(cellRect, new Color(0.7f, 0.7f, 0.7f));

                    // �̺�Ʈ�� �ִ� ���� �׵θ��� �ٸ� ������ ǥ��
                    if (cells[y, x].events != null && cells[y, x].events.Count > 0)
                    {
                        Rect eventIndicatorRect = new Rect(cellRect.x + 2, cellRect.y + 2, cellRect.width - 4, cellRect.height - 4);
                        EditorGUI.DrawRect(eventIndicatorRect, new Color(0.9f, 0.6f, 0.3f, 0.3f)); // �̺�Ʈ ���� ǥ��
                    }
                }

                // �׵θ� �׸��� (������)
                Rect borderRect = new Rect(cellRect.x, cellRect.y, cellRect.width, 1); // ���� �׵θ�
                EditorGUI.DrawRect(borderRect, Color.black);

                borderRect = new Rect(cellRect.x, cellRect.y, 1, cellRect.height); // ���� �׵θ�
                EditorGUI.DrawRect(borderRect, Color.black);

                borderRect = new Rect(cellRect.x, cellRect.y + cellRect.height - 1, cellRect.width, 1); // �Ʒ��� �׵θ�
                EditorGUI.DrawRect(borderRect, Color.black);

                borderRect = new Rect(cellRect.x + cellRect.width - 1, cellRect.y, 1, cellRect.height); // ������ �׵θ�
                EditorGUI.DrawRect(borderRect, Color.black);

                // �� ��ǥ ǥ�� (�� ���� ��ܿ� �� ���� ���)
                GUIStyle coordStyle = new GUIStyle(GUI.skin.label);
                coordStyle.fontSize = 10;
                coordStyle.fontStyle = FontStyle.Bold;
                coordStyle.normal.textColor = Color.black;
                coordStyle.alignment = TextAnchor.UpperLeft;
                EditorGUI.LabelField(new Rect(cellRect.x + 2, cellRect.y + 2, cellRect.width, 16), $"{x},{y}", coordStyle);

                // ���ʹ̰� �ִ� ��� �� �߾ӿ� ���ʹ� ���� ǥ��
                if (!cells[y, x].isEmpty)
                {
                    GUIStyle enemyStyle = new GUIStyle(GUI.skin.label);
                    enemyStyle.fontSize = 9;
                    enemyStyle.alignment = TextAnchor.MiddleCenter;
                    enemyStyle.normal.textColor = Color.black;
                    enemyStyle.wordWrap = true;

                    // ���ʹ� �̸� ǥ�� (ª�� ����)
                    string enemyName = cells[y, x].enemy.f_name;
                    if (enemyName.Length > 10)
                    {
                        enemyName = enemyName.Substring(0, 8) + "..";
                    }

                    EditorGUI.LabelField(
                        new Rect(cellRect.x, cellRect.y + 16, cellRect.width, cellRect.height - 16),
                        enemyName,
                        enemyStyle
                    );

                    // �̺�Ʈ ���� ǥ��
                    if (cells[y, x].events.Count > 0)
                    {
                        GUIStyle eventStyle = new GUIStyle(GUI.skin.label);
                        eventStyle.fontSize = 9;
                        eventStyle.fontStyle = FontStyle.Bold;
                        eventStyle.alignment = TextAnchor.LowerRight;
                        eventStyle.normal.textColor = new Color(0.8f, 0.4f, 0.0f);

                        EditorGUI.LabelField(
                            cellRect,
                            $"E:{cells[y, x].events.Count}",
                            eventStyle
                        );
                    }
                }

                // �� Ŭ�� ó��
                if (Event.current.type == UnityEditorEventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    selectedCell = new Vector2Int(x, y);
                    Repaint();
                    Event.current.Use(); // �̺�Ʈ �Һ� (�ٸ� ��Ʈ�ѿ� ���� ����)
                }
            }
        }

        // ���õ� �� ǥ��
        if (selectedCell.x >= 0 && selectedCell.y >= 0)
        {
            Rect selectedRect = new Rect(
                gridRect.x + selectedCell.x * cellSize,
                gridRect.y + selectedCell.y * cellSize,
                cellSize,
                cellSize
            );

            // ���õ� �� �׵θ� ���� (������)
            Rect borderRect = new Rect(selectedRect.x - 2, selectedRect.y - 2, selectedRect.width + 4, 2); // ���� �׵θ�
            EditorGUI.DrawRect(borderRect, Color.red);

            borderRect = new Rect(selectedRect.x - 2, selectedRect.y - 2, 2, selectedRect.height + 4); // ���� �׵θ�
            EditorGUI.DrawRect(borderRect, Color.red);

            borderRect = new Rect(selectedRect.x - 2, selectedRect.y + selectedRect.height, selectedRect.width + 4, 2); // �Ʒ��� �׵θ�
            EditorGUI.DrawRect(borderRect, Color.red);

            borderRect = new Rect(selectedRect.x + selectedRect.width, selectedRect.y - 2, 2, selectedRect.height + 4); // ������ �׵θ�
            EditorGUI.DrawRect(borderRect, Color.red);
        }

        // ��ǥ�� ���̺� ǥ��
        GUIStyle axisStyle = new GUIStyle(GUI.skin.label);
        axisStyle.fontStyle = FontStyle.Bold;
        axisStyle.alignment = TextAnchor.MiddleCenter;

        for (int x = 0; x < 12; x++)
        {
            Rect labelRect = new Rect(gridRect.x + x * cellSize, gridRect.y - 20, cellSize, 20);
            EditorGUI.LabelField(labelRect, x.ToString(), axisStyle);
        }

        for (int y = 0; y < 10; y++)
        {
            Rect labelRect = new Rect(gridRect.x - 20, gridRect.y + y * cellSize, 20, cellSize);
            EditorGUI.LabelField(labelRect, y.ToString(), axisStyle);
        }
    }

    private void DrawEnemySelection()
    {
        EditorGUILayout.LabelField("���ʹ� ����", EditorStyles.boldLabel);

        // ���õ� �� ���� ǥ��
        EditorGUILayout.LabelField($"���õ� ��: ({selectedCell.x}, {selectedCell.y})");

        // ���м�
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // ���� ���� ���ʹ� ���� ǥ��
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("���� ���ʹ�:", GUILayout.Width(80));

        if (!cells[selectedCell.y, selectedCell.x].isEmpty)
        {
            EditorGUILayout.LabelField(cells[selectedCell.y, selectedCell.x].enemy.f_name);

            if (GUILayout.Button("����", GUILayout.Width(60)))
            {
                cells[selectedCell.y, selectedCell.x].enemy = null;
                cells[selectedCell.y, selectedCell.x].events.Clear(); // ���ʹ� ���Ž� �̺�Ʈ�� �Բ� ����
            }
        }
        else
        {
            EditorGUILayout.LabelField("����");
        }
        EditorGUILayout.EndHorizontal();

        // ���м�
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // ���ʹ� ��� ���
        EditorGUILayout.LabelField("���ʹ� ���:");

        // ���ʹ� �˻� �ʵ� �߰�
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("�˻�:", GUILayout.Width(40));
        string searchText = EditorGUILayout.TextField(GUI.tooltip); // �˻��� �Է� �ʵ�
        EditorGUILayout.EndHorizontal();

        // ���ʹ� ��� ��ũ�� ����
        enemyScrollPosition = EditorGUILayout.BeginScrollView(enemyScrollPosition, GUILayout.Height(150));

        // BGDatabase���� ��� EnemyData �ҷ�����
        List<D_EnemyData> allEnemies = new List<D_EnemyData>();
        D_EnemyData.ForEachEntity(enemy => allEnemies.Add(enemy));

        // ���ʹ� �̸������� ����
        allEnemies.Sort((a, b) => a.f_name.CompareTo(b.f_name));

        // �˻��� ���͸�
        string searchLower = searchText?.ToLower() ?? "";
        foreach (var enemy in allEnemies)
        {
            // �˻�� ������ ���͸�
            if (!string.IsNullOrEmpty(searchText) && !enemy.f_name.ToLower().Contains(searchLower))
                continue;

            EditorGUILayout.BeginHorizontal();

            // ���ʹ� ���� ��ư
            if (GUILayout.Button(enemy.f_name, GUILayout.Width(150)))
            {
                cells[selectedCell.y, selectedCell.x].enemy = enemy;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEventSelection()
    {
        // ���м�
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // �̺�Ʈ ���� ���
        EditorGUILayout.LabelField("�̺�Ʈ ����", EditorStyles.boldLabel);

        // ���� ������ �̺�Ʈ ��� ǥ��
        EditorGUILayout.LabelField("���� �̺�Ʈ:");

        if (cells[selectedCell.y, selectedCell.x].events.Count == 0)
        {
            EditorGUILayout.HelpBox("�Ҵ�� �̺�Ʈ�� �����ϴ�.", MessageType.Info);
        }
        else
        {
            // ���� �Ҵ�� �̺�Ʈ ��� ǥ��
            for (int i = 0; i < cells[selectedCell.y, selectedCell.x].events.Count; i++)
            {
                var eventData = cells[selectedCell.y, selectedCell.x].events[i];
                EditorGUILayout.BeginHorizontal();

                string eventType = eventData is D_SpawnEnemyEventData ? "���� �̺�Ʈ" :
                                   eventData is D_DropItemEventData ? "������ ��� �̺�Ʈ" : "��Ÿ �̺�Ʈ";

                string triggerType = eventData.f_eventTriggerType.ToString();

                EditorGUILayout.LabelField($"{i + 1}. {eventData.f_name}");
                EditorGUILayout.LabelField($"[{eventType}]", GUILayout.Width(90));
                EditorGUILayout.LabelField($"[{triggerType}]", GUILayout.Width(80));

                if (GUILayout.Button("����", GUILayout.Width(60)))
                {
                    cells[selectedCell.y, selectedCell.x].events.RemoveAt(i);
                    break; // ����Ʈ�� ����Ǿ����Ƿ� ���� ����
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // �̺�Ʈ �˻� �ʵ�
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("�̺�Ʈ �˻�:", GUILayout.Width(70));
        string eventSearchText = EditorGUILayout.TextField(GUI.tooltip);
        EditorGUILayout.EndHorizontal();

        // �� �̺�Ʈ �߰� ����
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("�̺�Ʈ �߰�:");

        // �̺�Ʈ ��� ��ũ�� ����
        eventScrollPosition = EditorGUILayout.BeginScrollView(eventScrollPosition, GUILayout.Height(150));

        string searchLower = eventSearchText?.ToLower() ?? "";

        // ���� �̺�Ʈ ��� ǥ��
        EditorGUILayout.LabelField("���� �̺�Ʈ:", EditorStyles.boldLabel);
        List<D_SpawnEnemyEventData> spawnEvents = new List<D_SpawnEnemyEventData>();
        D_SpawnEnemyEventData.ForEachEntity(e => spawnEvents.Add(e));
        spawnEvents.Sort((a, b) => a.f_name.CompareTo(b.f_name));

        foreach (var spawnEvent in spawnEvents)
        {
            // �˻��� ���͸�
            if (!string.IsNullOrEmpty(eventSearchText) && !spawnEvent.f_name.ToLower().Contains(searchLower))
                continue;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(spawnEvent.f_name, GUILayout.Width(150)))
            {
                // �ߺ� �˻�
                if (!cells[selectedCell.y, selectedCell.x].events.Contains(spawnEvent))
                {
                    cells[selectedCell.y, selectedCell.x].events.Add(spawnEvent);
                }
                else
                {
                    EditorUtility.DisplayDialog("�̺�Ʈ �ߺ�", "�̹� �߰��� �̺�Ʈ�Դϴ�.", "Ȯ��");
                }
            }

            EditorGUILayout.LabelField($"Ʈ����: {spawnEvent.f_eventTriggerType}");
            EditorGUILayout.LabelField($"��: {spawnEvent.f_enemy.f_name}, ����: {spawnEvent.f_spawnCount}");

            EditorGUILayout.EndHorizontal();
        }

        // ������ ��� �̺�Ʈ ��� ǥ��
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("������ ��� �̺�Ʈ:", EditorStyles.boldLabel);
        List<D_DropItemEventData> dropEvents = new List<D_DropItemEventData>();
        D_DropItemEventData.ForEachEntity(e => dropEvents.Add(e));
        dropEvents.Sort((a, b) => a.f_name.CompareTo(b.f_name));

        foreach (var dropEvent in dropEvents)
        {
            // �˻��� ���͸�
            if (!string.IsNullOrEmpty(eventSearchText) && !dropEvent.f_name.ToLower().Contains(searchLower))
                continue;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(dropEvent.f_name, GUILayout.Width(150)))
            {
                // �ߺ� �˻�
                if (!cells[selectedCell.y, selectedCell.x].events.Contains(dropEvent))
                {
                    cells[selectedCell.y, selectedCell.x].events.Add(dropEvent);
                }
                else
                {
                    EditorUtility.DisplayDialog("�̺�Ʈ �ߺ�", "�̹� �߰��� �̺�Ʈ�Դϴ�.", "Ȯ��");
                }
            }

            EditorGUILayout.LabelField($"Ʈ����: {dropEvent.f_eventTriggerType}");
            EditorGUILayout.LabelField($"������ ��� �̺�Ʈ (����: {dropEvent.f_count})");

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void LoadDataIfExists()
    {
        // ��� �� �ʱ�ȭ
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                cells[y, x].enemy = null;
                cells[y, x].events.Clear();
            }
        }

        // BGDatabase���� �� ������ �ҷ�����
        D_EnemyPlacementData placement = D_EnemyPlacementData.FindEntity(p => p.f_mapID == mapId);

        if (placement != null)
        {
            foreach (var cellData in placement.f_cellData)
            {
                int x = (int)cellData.f_position.x;
                int y = (int)cellData.f_position.y;

                if (x >= 0 && x < 12 && y >= 0 && y < 10)
                {
                    cells[y, x].enemy = cellData.f_enemy;

                    // ViewRelationMultiple�� ������ �̺�Ʈ ������ �ε�
                    if (cellData.f_events != null)
                    {
                        // f_events�� ViewRelationMultiple �ʵ��, ���� DB�� ����� �̺�Ʈ ������ ���
                        cells[y, x].events = new List<D_EventDummyData>(cellData.f_events);
                    }
                }
            }

            EditorUtility.DisplayDialog("�ε� �Ϸ�", $"�� ID {mapId}�� ���ʹ� ��ġ �����͸� �ҷ��Խ��ϴ�.", "Ȯ��");
        }
        else
        {
            EditorUtility.DisplayDialog("�˸�", $"�� ID {mapId}�� ���� �����Ͱ� �����ϴ�. ���ο� ���� �����մϴ�.", "Ȯ��");
        }
    }

    private void SaveData()
    {
        // ���� �����Ͱ� �ִ��� Ȯ��
        D_EnemyPlacementData placement = D_EnemyPlacementData.FindEntity(p => p.f_mapID == mapId);

        // ������ ���� ����
        if (placement == null)
        {
            placement = D_EnemyPlacementData.NewEntity();
            placement.f_name = "Map_" + mapId;
            placement.f_mapID = mapId;
        }
        else
        {
            // �ٸ� mapId�� ���� ��� ��ƼƼ ã��
            List<D_EnemyPlacementData> allPlacements = new List<D_EnemyPlacementData>();
            D_EnemyPlacementData.ForEachEntity(p => allPlacements.Add(p));

            // ���� �ʰ� ���õ� ��ƼƼ�� ����
            List<D_EnemyPlacementData> toRemove = allPlacements.FindAll(p => p.f_mapID == mapId);

            // ��� ���� ��ƼƼ �����ϰ� ���� ����
            foreach (var p in toRemove)
            {
                p.Delete();
            }

            // �� ��ƼƼ ����
            placement = D_EnemyPlacementData.NewEntity();
            placement.f_name = "Map_" + mapId;
            placement.f_mapID = mapId;
        }

        // ���ο� �� ������ �߰�
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                if (cells[y, x] != null && !cells[y, x].isEmpty)
                {
                    // D_cellData Ŭ������ ���
                    var cellData = D_cellData.NewEntity(placement);
                    cellData.f_name = $"Cell_{x}_{y}";
                    cellData.f_position = new Vector2(x, y); // Vector2 Ÿ�� ���
                    cellData.f_enemy = cells[y, x].enemy;

                    if (cells[y, x].events != null && cells[y, x].events.Count > 0)
                    {
                        try
                        {
                            // ���� ���� �̺�Ʈ ��� ����
                            List<D_EventDummyData> eventsList = new List<D_EventDummyData>(cells[y, x].events);

                            // �� �������� events �ʵ忡 ����
                            cellData.f_events = eventsList;
                        }
                        catch (System.Exception e)
                        {
                            // ������ �α׿� ���
                            Debug.LogError($"�̺�Ʈ ������ ���� �� ���� �߻�: {e.Message}");
                        }
                    }
                }
            }
        }

        // ������� ����
        BGRepo.I.Save();

        EditorUtility.DisplayDialog("���� �Ϸ�", $"�� ID {mapId}�� ���ʹ� ��ġ �����Ͱ� ����Ǿ����ϴ�.", "Ȯ��");
    }
}