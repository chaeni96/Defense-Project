using UnityEngine;
using UnityEditor;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEditorEventType = UnityEngine.EventType;
using Kylin.FSM;

public class EnemyPlacementTool : EditorWindow
{
    private int mapId = 0; // 자동으로 설정될 예정
    private string mapName;
    private Vector2 mainScrollPosition; // 전체 창에 대한 스크롤 위치
    private Vector2 enemyScrollPosition; // 에너미 목록 스크롤 위치
    private EnemyCell[,] cells = new EnemyCell[8, 8]; // 7x7 그리드
    private Vector2Int selectedCell = new Vector2Int(-1, -1);

    // 셀 데이터 클래스
    [System.Serializable]
    private class EnemyCell
    {
        public D_EnemyData enemy;
        public List<D_EventDummyData> events = new List<D_EventDummyData>(); // 이벤트 목록 추가
        public bool isEmpty => enemy == null;
    }

    [MenuItem("Window/Custom Tools/Enemy Placement Tool", false, 100)]
    public static void Open()
    {
        var window = GetWindow<EnemyPlacementTool>("Enemy Placement Tool");
    }

    private void OnEnable()
    {
        // 셀 초기화
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                cells[y, x] = new EnemyCell();
            }
        }

        // 자동으로 다음 사용 가능한 MapID 찾기
        FindNextAvailableMapId();
    }

    // 다음 사용 가능한 MapID를 찾는 메서드
    private void FindNextAvailableMapId()
    {
        int highestMapId = -1;

        // BGDatabase에서 모든 EnemyPlacementData 불러와서 최대 mapID 찾기
        List<D_EnemyPlacementData> allPlacements = new List<D_EnemyPlacementData>();
        D_EnemyPlacementData.ForEachEntity(p => allPlacements.Add(p));

        foreach (var placement in allPlacements)
        {
            if (placement.f_mapID > highestMapId)
            {
                highestMapId = placement.f_mapID;
            }
        }

        // 다음 MapID 설정 (최소값은 0)
        mapId = highestMapId + 1;
    }

    private void OnGUI()
    {
        // 전체 창에 대한 스크롤 시작
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        // 왼쪽 패널 - 맵 정보 영역
        DrawLeftPanel();

        // 약간의 공간 추가
        GUILayout.Space(10);

        // 중앙 패널 - 그리드 배치 영역
        EditorGUILayout.BeginVertical(GUILayout.MinWidth(500), GUILayout.ExpandHeight(true));
        DrawGrid();
        EditorGUILayout.EndVertical();

        // 약간의 공간 추가
        GUILayout.Space(10);

        // 오른쪽 패널 - 에너미 선택 및 이벤트 지정 영역
        EditorGUILayout.BeginVertical(GUILayout.Width(600), GUILayout.ExpandHeight(true));

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

        // 추가 공간 (하단 여백)
        GUILayout.Space(20);

        // 전체 스크롤 종료
        EditorGUILayout.EndScrollView();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(180), GUILayout.ExpandHeight(true));

        GUILayout.Label("Enemy Placement Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(20);

        // 다음 MapID 표시 (수정 불가)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("다음 Map ID:", GUILayout.Width(80));
        EditorGUILayout.LabelField(mapId.ToString(), EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 맵 이름 입력 필드
        EditorGUILayout.LabelField("Map Name:", GUILayout.Width(80));
        mapName = EditorGUILayout.TextField(mapName);

        EditorGUILayout.Space(10);

        // 저장 버튼 (이전 Load 버튼 제거)
        EditorGUILayout.Space();
        if (GUILayout.Button("새 맵 저장하기"))
        {
            if (string.IsNullOrEmpty(mapName))
            {
                EditorUtility.DisplayDialog("경고", "맵 이름을 입력해주세요.", "확인");
            }
            else
            {
                SaveData();

                // 저장 후 맵 초기화 및 다음 맵 ID 설정
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        cells[y, x].enemy = null;
                        cells[y, x].events.Clear();
                    }
                }

                // 다음 사용 가능한 MapID 다시 찾기
                FindNextAvailableMapId();

                // 맵 이름 초기화 (선택적)
                mapName = "";
            }
        }

        // 추가 도구 버튼들
        EditorGUILayout.Space(20);

        if (GUILayout.Button("모든 셀 초기화"))
        {
            if (EditorUtility.DisplayDialog("초기화 확인", "모든 셀의 데이터를 초기화하시겠습니까?", "예", "아니오"))
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
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
        GUILayout.Label("배치 그리드", EditorStyles.boldLabel);

        // 이 부분이 중요합니다 - 그리드를 그리기 위한 영역을 미리 확보
        float cellSize = 40f; // 셀 크기
        Rect layoutRect = GUILayoutUtility.GetRect(8 * cellSize + 40, 8 * cellSize + 60);

        // 그리드의 시작 위치를 layoutRect 기준으로 설정
        Rect gridRect = new Rect(layoutRect.x + 30, layoutRect.y + 20, cellSize * 8, cellSize * 8);

        // 그리드 배경 (흰색)
        EditorGUI.DrawRect(gridRect, Color.white);

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Rect cellRect = new Rect(gridRect.x + x * cellSize, gridRect.y + y * cellSize, cellSize, cellSize);

                // 셀 배경 색상 (비어 있으면 투명, 에너미가 있으면 회색)
                if (!cells[y, x].isEmpty)
                {
                    // 에너미가 있는 셀은 회색으로 채우기
                    EditorGUI.DrawRect(cellRect, new Color(0.7f, 0.7f, 0.7f));

                    // 이벤트가 있는 셀은 테두리를 다른 색으로 표시
                    if (cells[y, x].events != null && cells[y, x].events.Count > 0)
                    {
                        Rect eventIndicatorRect = new Rect(cellRect.x + 2, cellRect.y + 2, cellRect.width - 4, cellRect.height - 4);
                        EditorGUI.DrawRect(eventIndicatorRect, new Color(0.9f, 0.6f, 0.3f, 0.3f)); // 이벤트 있음 표시
                    }
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

                // 에너미가 있는 경우 셀 중앙에 에너미 정보 표시
                if (!cells[y, x].isEmpty)
                {
                    GUIStyle enemyStyle = new GUIStyle(GUI.skin.label);
                    enemyStyle.fontSize = 9;
                    enemyStyle.alignment = TextAnchor.MiddleCenter;
                    enemyStyle.normal.textColor = Color.black;
                    enemyStyle.wordWrap = true;

                    // 에너미 이름 표시 (짧게 줄임)
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

                    // 이벤트 개수 표시
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

                // 셀 클릭 처리
                if (Event.current.type == UnityEditorEventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    selectedCell = new Vector2Int(x, y);
                    Repaint();
                    Event.current.Use(); // 이벤트 소비 (다른 컨트롤에 전파 방지)
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

        for (int x = 0; x < 8; x++)
        {
            Rect labelRect = new Rect(gridRect.x + x * cellSize, gridRect.y - 20, cellSize, 20);
            EditorGUI.LabelField(labelRect, x.ToString(), axisStyle);
        }

        for (int y = 0; y < 8; y++)
        {
            Rect labelRect = new Rect(gridRect.x - 20, gridRect.y + y * cellSize, 20, cellSize);
            EditorGUI.LabelField(labelRect, y.ToString(), axisStyle);
        }
    }

    private void DrawEnemySelection()
    {
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
                cells[selectedCell.y, selectedCell.x].events.Clear(); // 에너미 제거시 이벤트도 함께 제거
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

        // 에너미 검색 필드 추가
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("검색:", GUILayout.Width(40));
        string searchText = EditorGUILayout.TextField(GUI.tooltip); // 검색어 입력 필드
        EditorGUILayout.EndHorizontal();

        // 에너미 목록 스크롤 영역
        enemyScrollPosition = EditorGUILayout.BeginScrollView(enemyScrollPosition, GUILayout.Height(150));

        // BGDatabase에서 모든 EnemyData 불러오기
        List<D_EnemyData> allEnemies = new List<D_EnemyData>();
        D_EnemyData.ForEachEntity(enemy => allEnemies.Add(enemy));

        // 에너미 이름순으로 정렬
        allEnemies.Sort((a, b) => a.f_name.CompareTo(b.f_name));

        // 검색어 필터링
        string searchLower = searchText?.ToLower() ?? "";
        foreach (var enemy in allEnemies)
        {
            // 검색어가 있으면 필터링
            if (!string.IsNullOrEmpty(searchText) && !enemy.f_name.ToLower().Contains(searchLower))
                continue;

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

    private void SaveData()
    {
        // 새 엔티티 생성
        D_EnemyPlacementData placement = D_EnemyPlacementData.NewEntity();
        placement.f_name = mapName + mapId; // 사용자가 입력한 이름 사용
        placement.f_mapID = mapId;  // 자동으로 설정된 다음 맵 ID 사용

        // 셀 데이터 추가
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (cells[y, x] != null && !cells[y, x].isEmpty)
                {
                    // D_cellData 클래스명 사용
                    var cellData = D_cellData.NewEntity(placement);
                    cellData.f_name = $"Cell_{x}_{y}";
                    cellData.f_position = new Vector2(x, y); // Vector2 타입 사용
                    cellData.f_enemy = cells[y, x].enemy;

                    if (cells[y, x].events != null && cells[y, x].events.Count > 0)
                    {
                        try
                        {
                            // 현재 셀의 이벤트 목록 복사
                            List<D_EventDummyData> eventsList = new List<D_EventDummyData>(cells[y, x].events);
                        }
                        catch (System.Exception e)
                        {
                            // 에러를 로그에 기록
                            Debug.LogError($"이벤트 데이터 저장 중 오류 발생: {e.Message}");
                        }
                    }
                }
            }
        }

        // 변경사항 저장
        BGRepo.I.Save();

        EditorUtility.DisplayDialog("저장 완료", $"맵 '{mapName}'이(가) 맵 ID {mapId}로 저장되었습니다.", "확인");
    }
}