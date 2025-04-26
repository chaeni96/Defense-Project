using UnityEngine;
using UnityEditor;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEditorEventType = UnityEngine.EventType;

public class EnemyPlacementTool : EditorWindow
{
    private int mapId = 0;
    private Vector2 scrollPosition;
    private EnemyCell[,] cells = new EnemyCell[9, 12]; // 9x12 �׸���
    private Vector2Int selectedCell = new Vector2Int(-1, -1);

    // �� ������ Ŭ����
    [System.Serializable]
    private class EnemyCell
    {
        public D_EnemyData enemy;
        public bool isEmpty => enemy == null;
    }

    [MenuItem("Tools/Enemy Placement Tool")]
    public static void ShowWindow()
    {
        GetWindow<EnemyPlacementTool>("Enemy Placement Tool");
    }

    private void OnEnable()
    {
        // �� �ʱ�ȭ
        for (int y = 0; y < 9; y++)
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
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        // ���� �г� - MapID �Է� ����
        EditorGUILayout.BeginVertical(GUILayout.Width(180), GUILayout.ExpandHeight(true));

        GUILayout.Label("Enemy Placement Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(70);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Map ID:", GUILayout.Width(60));
        mapId = EditorGUILayout.IntField(mapId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Load Map"))
        {
            LoadDataIfExists();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Save Data"))
        {
            SaveData();
        }

        EditorGUILayout.EndVertical();

        // �ణ�� ���� �߰�
        GUILayout.Space(10);

        // �߾� �г� - �׸��� ��ġ ����
        EditorGUILayout.BeginVertical(GUILayout.MinWidth(500), GUILayout.ExpandHeight(true));
        DrawGrid();
        EditorGUILayout.EndVertical();

        // �ణ�� ���� �߰�
        GUILayout.Space(10);

        // ������ �г� - ���ʹ� ���� ����
        EditorGUILayout.BeginVertical(GUILayout.Width(320), GUILayout.ExpandHeight(true));

        if (selectedCell.x >= 0 && selectedCell.y >= 0)
        {
            DrawEnemySelection();
        }
        else
        {
            EditorGUILayout.HelpBox("���� �����ϸ� ���⿡ ���ʹ� ����� ǥ�õ˴ϴ�.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid()
    {

        GUILayout.Space(50);


        EditorGUILayout.LabelField("��ġ �׸���", EditorStyles.boldLabel);

        // �� �κ��� �߿��մϴ� - �׸��带 �׸��� ���� ������ �̸� Ȯ��
        float cellSize = 40f; // �� ū �� ũ��
        Rect layoutRect = GUILayoutUtility.GetRect(12 * cellSize + 40, 9 * cellSize + 60);

        // �׸����� ���� ��ġ�� layoutRect �������� ����
        Rect gridRect = new Rect(layoutRect.x + 30, layoutRect.y + 20, cellSize * 12, cellSize * 9);

        // �׸��� ��� (���)
        EditorGUI.DrawRect(gridRect, Color.white);

        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                Rect cellRect = new Rect(gridRect.x + x * cellSize, gridRect.y + y * cellSize, cellSize, cellSize);

                // �� ��� ���� (��� ������ ����, ���ʹ̰� ������ ȸ��)
                if (!cells[y, x].isEmpty)
                {
                    // ���ʹ̰� �ִ� ���� ȸ������ ä���
                    EditorGUI.DrawRect(cellRect, new Color(0.7f, 0.7f, 0.7f));
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

                // �� Ŭ�� ó��
                if (Event.current.type == UnityEditorEventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    selectedCell = new Vector2Int(x, y);
                    Repaint();
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

        for (int y = 0; y < 9; y++)
        {
            Rect labelRect = new Rect(gridRect.x - 20, gridRect.y + y * cellSize, 20, cellSize);
            EditorGUI.LabelField(labelRect, y.ToString(), axisStyle);
        }
    }

    private void DrawEnemySelection()
    {
        GUILayout.Space(50);

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
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.EndHorizontal();

        // ���ʹ� ��� ��ũ�� ����
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

        // BGDatabase���� ��� EnemyData �ҷ�����
        List<D_EnemyData> allEnemies = new List<D_EnemyData>();
        D_EnemyData.ForEachEntity(enemy => allEnemies.Add(enemy));

        // ���ʹ� �̸������� ����
        allEnemies.Sort((a, b) => a.f_name.CompareTo(b.f_name));

        foreach (var enemy in allEnemies)
        {
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

    private void LoadDataIfExists()
    {
        // ��� �� �ʱ�ȭ
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                cells[y, x].enemy = null;
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

                if (x >= 0 && x < 12 && y >= 0 && y < 9)
                {
                    cells[y, x].enemy = cellData.f_enemy;
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

        // ���� �� ������ Ŭ����
        placement.f_cellData.Clear();

        // ���ο� �� ������ �߰�
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                if (!cells[y, x].isEmpty)
                {
                    //var cellData = D_EnemyPlacementCellData.NewEntity(placement);
                    //cellData.f_name = $"Cell_{x}_{y}";
                    //cellData.f_Position = new Vector2Int(x, y);
                    //cellData.f_Enemy = cells[y, x].enemy;
                }
            }
        }

        EditorUtility.DisplayDialog("���� �Ϸ�", $"�� ID {mapId}�� ���ʹ� ��ġ �����Ͱ� ����Ǿ����ϴ�.", "Ȯ��");
    }
}