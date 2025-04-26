using UnityEngine;
using UnityEditor;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEditorEventType = UnityEngine.EventType;

public class EnemyPlacementTool : EditorWindow
{
    private int mapId = 0;
    private Vector2 scrollPosition;
    private EnemyCell[,] cells = new EnemyCell[9, 12]; // 9x12 그리드
    private Vector2Int selectedCell = new Vector2Int(-1, -1);

    // 셀 데이터 클래스
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
        // 셀 초기화
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                cells[y, x] = new EnemyCell();
            }
        }

        // 기존 데이터 불러오기
        //LoadDataIfExists();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        // 왼쪽 패널 - MapID 입력 영역
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

        // 약간의 공간 추가
        GUILayout.Space(10);

        // 중앙 패널 - 그리드 배치 영역
        EditorGUILayout.BeginVertical(GUILayout.MinWidth(500), GUILayout.ExpandHeight(true));
        DrawGrid();
        EditorGUILayout.EndVertical();

        // 약간의 공간 추가
        GUILayout.Space(10);

        // 오른쪽 패널 - 에너미 선택 영역
        EditorGUILayout.BeginVertical(GUILayout.Width(320), GUILayout.ExpandHeight(true));

        if (selectedCell.x >= 0 && selectedCell.y >= 0)
        {
            DrawEnemySelection();
        }
        else
        {
            EditorGUILayout.HelpBox("셀을 선택하면 여기에 에너미 목록이 표시됩니다.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid()
    {

        GUILayout.Space(50);


        EditorGUILayout.LabelField("배치 그리드", EditorStyles.boldLabel);

        // 이 부분이 중요합니다 - 그리드를 그리기 위한 영역을 미리 확보
        float cellSize = 40f; // 더 큰 셀 크기
        Rect layoutRect = GUILayoutUtility.GetRect(12 * cellSize + 40, 9 * cellSize + 60);

        // 그리드의 시작 위치를 layoutRect 기준으로 설정
        Rect gridRect = new Rect(layoutRect.x + 30, layoutRect.y + 20, cellSize * 12, cellSize * 9);

        // 그리드 배경 (흰색)
        EditorGUI.DrawRect(gridRect, Color.white);

        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                Rect cellRect = new Rect(gridRect.x + x * cellSize, gridRect.y + y * cellSize, cellSize, cellSize);

                // 셀 배경 색상 (비어 있으면 투명, 에너미가 있으면 회색)
                if (!cells[y, x].isEmpty)
                {
                    // 에너미가 있는 셀은 회색으로 채우기
                    EditorGUI.DrawRect(cellRect, new Color(0.7f, 0.7f, 0.7f));
                }

                // 테두리 그리기 (검은색)
                Rect borderRect = new Rect(cellRect.x, cellRect.y, cellRect.width, 1); // 위쪽 테두리
                EditorGUI.DrawRect(borderRect, Color.black);

                borderRect = new Rect(cellRect.x, cellRect.y, 1, cellRect.height); // 왼쪽 테두리
                EditorGUI.DrawRect(borderRect, Color.black);

                borderRect = new Rect(cellRect.x, cellRect.y + cellRect.height - 1, cellRect.width, 1); // 아래쪽 테두리
                EditorGUI.DrawRect(borderRect, Color.black);

                borderRect = new Rect(cellRect.x + cellRect.width - 1, cellRect.y, 1, cellRect.height); // 오른쪽 테두리
                EditorGUI.DrawRect(borderRect, Color.black);

                // 셀 좌표 표시 (셀 좌측 상단에 더 눈에 띄게)
                GUIStyle coordStyle = new GUIStyle(GUI.skin.label);
                coordStyle.fontSize = 10;
                coordStyle.fontStyle = FontStyle.Bold;
                coordStyle.normal.textColor = Color.black;
                coordStyle.alignment = TextAnchor.UpperLeft;
                EditorGUI.LabelField(new Rect(cellRect.x + 2, cellRect.y + 2, cellRect.width, 16), $"{x},{y}", coordStyle);

                // 셀 클릭 처리
                if (Event.current.type == UnityEditorEventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    selectedCell = new Vector2Int(x, y);
                    Repaint();
                }
            }
        }

        // 선택된 셀 표시
        if (selectedCell.x >= 0 && selectedCell.y >= 0)
        {
            Rect selectedRect = new Rect(
                gridRect.x + selectedCell.x * cellSize,
                gridRect.y + selectedCell.y * cellSize,
                cellSize,
                cellSize
            );

            // 선택된 셀 테두리 강조 (빨간색)
            Rect borderRect = new Rect(selectedRect.x - 2, selectedRect.y - 2, selectedRect.width + 4, 2); // 위쪽 테두리
            EditorGUI.DrawRect(borderRect, Color.red);

            borderRect = new Rect(selectedRect.x - 2, selectedRect.y - 2, 2, selectedRect.height + 4); // 왼쪽 테두리
            EditorGUI.DrawRect(borderRect, Color.red);

            borderRect = new Rect(selectedRect.x - 2, selectedRect.y + selectedRect.height, selectedRect.width + 4, 2); // 아래쪽 테두리
            EditorGUI.DrawRect(borderRect, Color.red);

            borderRect = new Rect(selectedRect.x + selectedRect.width, selectedRect.y - 2, 2, selectedRect.height + 4); // 오른쪽 테두리
            EditorGUI.DrawRect(borderRect, Color.red);
        }

        // 좌표축 레이블 표시
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

        EditorGUILayout.LabelField("에너미 선택", EditorStyles.boldLabel);

        // 선택된 셀 정보 표시
        EditorGUILayout.LabelField($"선택된 셀: ({selectedCell.x}, {selectedCell.y})");

        // 구분선
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 현재 셀의 에너미 정보 표시
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("현재 에너미:", GUILayout.Width(80));

        if (!cells[selectedCell.y, selectedCell.x].isEmpty)
        {
            EditorGUILayout.LabelField(cells[selectedCell.y, selectedCell.x].enemy.f_name);

            if (GUILayout.Button("제거", GUILayout.Width(60)))
            {
                cells[selectedCell.y, selectedCell.x].enemy = null;
            }
        }
        else
        {
            EditorGUILayout.LabelField("없음");
        }
        EditorGUILayout.EndHorizontal();

        // 구분선
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 에너미 목록 헤더
        EditorGUILayout.LabelField("에너미 목록:");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.EndHorizontal();

        // 에너미 목록 스크롤 영역
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

        // BGDatabase에서 모든 EnemyData 불러오기
        List<D_EnemyData> allEnemies = new List<D_EnemyData>();
        D_EnemyData.ForEachEntity(enemy => allEnemies.Add(enemy));

        // 에너미 이름순으로 정렬
        allEnemies.Sort((a, b) => a.f_name.CompareTo(b.f_name));

        foreach (var enemy in allEnemies)
        {
            EditorGUILayout.BeginHorizontal();

            // 에너미 선택 버튼
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
        // 모든 셀 초기화
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                cells[y, x].enemy = null;
            }
        }

        // BGDatabase에서 맵 데이터 불러오기
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

            EditorUtility.DisplayDialog("로드 완료", $"맵 ID {mapId}의 에너미 배치 데이터를 불러왔습니다.", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("알림", $"맵 ID {mapId}에 대한 데이터가 없습니다. 새로운 맵을 생성합니다.", "확인");
        }
    }

    private void SaveData()
    {
        // 기존 데이터가 있는지 확인
        D_EnemyPlacementData placement = D_EnemyPlacementData.FindEntity(p => p.f_mapID == mapId);

        // 없으면 새로 생성
        if (placement == null)
        {
            placement = D_EnemyPlacementData.NewEntity();
            placement.f_name = "Map_" + mapId;
            placement.f_mapID = mapId;
        }

        // 기존 셀 데이터 클리어
        placement.f_cellData.Clear();

        // 새로운 셀 데이터 추가
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

        EditorUtility.DisplayDialog("저장 완료", $"맵 ID {mapId}의 에너미 배치 데이터가 저장되었습니다.", "확인");
    }
}