using BGDatabaseEnum;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 유닛 타일 UI 요소를 관리하고 드래그 앤 드롭으로 맵에 유닛을 배치하는 클래스
public class UnitCardObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private GameObject tileImageLayout; // TileImage_Layout 게임오브젝트
    [SerializeField] private GameObject tileImagePrefab; // Tile_Image 프리팹

  
    [SerializeField] private List<Image> tileImages = new List<Image>();
    [SerializeField] private TMP_Text cardCostText;

    //프리뷰 유닛들을 관리, 합성 여부 판단, 드래그 도중 상태 복원 및 업데이트

    //TileData의 프리뷰 유닛
    private Dictionary<int, UnitController> originalPreviews = new Dictionary<int, UnitController>();

    //현재 화면에 보여지는 프리뷰 유닛, 합성 여부에 따라 기존 프리뷰 교체, 위치 변경, 머터리얼 업데이트
    private Dictionary<int, UnitController> currentPreviews = new Dictionary<int, UnitController>(); 

    // 기준점 (0,0)으로부터의 오프셋값들(다중타일을 위함)
    private List<Vector2> tileOffsets;
    private Vector2 previousTilePosition;
    private string tileShapeName;
    private int cardCost;
    private bool isDragging = false;
    private bool hasDragged = false;  // 실제 드래그 여부를 체크하는 변수 추가
    private bool canPlace = false;

    // 활성화되었던 이미지의 인덱스를 저장할 리스트 추가
    private List<int> activeImageIndices = new List<int>();

    // 카드가 사용되었을 때 발생하는 이벤트 (사용된 카드덱을 매개변수로 전달)
    public static event System.Action<GameObject> OnCardUsed;


    // 카드 초기화: 타일 정보, 코스트, 프리뷰 설정
    public void InitializeCardInform(D_TileShpeData unitData)
    {
        CleanUp();

        tileShapeName = unitData.f_name;
        cardCost = unitData.f_Cost;
        cardCostText.text = cardCost.ToString();

        InitializeTileShape();
        UpdateCostTextColor();
        CreateTilePreview(unitData);

        //cost 사용 이벤트 구독 -> cost 추가될때 tile Cost Text 업데이트
        GameManager.Instance.OnCostAdd += OnCostChanged;

        StageManager.Instance.OnWaveFinish += CancelDragging;

    }

    // 타일 모양 데이터를 기반으로 오프셋 위치 초기화
    private void InitializeTileShape()
    {
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            tileOffsets = new List<Vector2>();
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                // 원본 좌표
                Vector2 originalPos = tile.f_TilePos.f_TilePos;

                // Y축 반전 - (0,0)이 기준점일 때 (0,1)은 위쪽에 배치되도록
                Vector2 correctedPos = new Vector2(originalPos.x, -originalPos.y);
                tileOffsets.Add(correctedPos);
            }
        }
    }

    //unitCard 드래그했을시 안보여야됨
    private void SetUIVisibility(bool visible)
    {
        foreach (int index in activeImageIndices)
        {
            if (index < tileImages.Count && tileImages[index] != null)
            {
                tileImages[index].gameObject.SetActive(visible);
            }
        }
        cardCostText.gameObject.SetActive(visible);
    }

    // 코스트 변경시 호출될 메서드
    private void OnCostChanged()
    {
        UpdateCostTextColor();
    }

    //cost 색 -> 변경 가능 : 하얀색, 불가능 : 빨간색
    public void UpdateCostTextColor()
    {
        cardCostText.color = GameManager.Instance.GetSystemStat(StatName.Cost) >= cardCost ? Color.white : Color.red;
    }

    // 드래그 시작: 코스트 체크 후 프리뷰 인스턴스 생성
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.Instance.GetSystemStat(StatName.Cost) < cardCost) return;

        isDragging = true;
        canPlace = false;
        hasDragged = false;  // 드래그 시작 시 false로 초기화
        SetUIVisibility(false);  // UI 숨기기

        // 마우스 커서의 월드 좌표 계산

        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);
        CreatePreviewInstances(tilePos);
        UpdatePreviewInstancesPosition(tilePos);
    }

    // 드래그 중: 프리뷰 위치 업데이트 및 배치 가능 여부 표시
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;


        hasDragged = true;  // 드래그가 발생했음을 표시

        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        if (tilePos != previousTilePosition)
        {
            TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0.1f));

            //canPlace에 따라 배치 가능불가능 판정
            canPlace = TileMapManager.Instance.CanPlaceObject(tilePos, tileOffsets, originalPreviews);
            UpdatePreviewInstancesPosition(tilePos);
            previousTilePosition = tilePos;
        }
        
    }

    // 드래그 종료: 배치 가능 여부 확인 후 유닛 배치 또는 취소
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        //배치 타일 투명화
        TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0));

        //배치 가능한지 체크
        canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, tileOffsets, originalPreviews);

        if (hasDragged && canPlace)
        {
            PlaceUnits();
        }
        else
        {
            SetUIVisibility(true);  // 배치 실패시 UI 다시 보이게
            CancelPlacement();
        }

        isDragging = false;
    }

    // 프리뷰 인스턴스 생성: 각 타일 위치에 대한 프리뷰 유닛 생성
    private void CreatePreviewInstances(Vector3 tilePos)
    {

        //유닛의 worldPos
        Vector3 worldPos = TileMapManager.Instance.GetTileToWorldPosition(tilePos);

        var tileShapeData = D_TileShpeData.GetEntity(tileShapeName);

        for (int i = 0; i < tileOffsets.Count; i++)
        {
            var unitBuildData = tileShapeData.f_unitBuildData[i];
            var unitPoolinKey = unitBuildData.f_unitData.f_UnitPoolingKey;
            
            GameObject previewInstance = PoolingManager.Instance.GetObject(unitPoolinKey.f_PoolObjectAddressableKey, worldPos, (int)ObjectLayer.Player);

            UnitController previewUnit = previewInstance.GetComponent<UnitController>();

            if (previewUnit != null)
            {
                previewUnit.Initialize();
                previewUnit.InitializeUnitInfo(unitBuildData.f_unitData);
                originalPreviews[i] = previewUnit;
                currentPreviews[i] = previewUnit;
            }
        }
    }



    // 프리뷰 위치 업데이트: 현재 마우스 위치에 따라 프리뷰 위치 조정, 배치불가에 따라 머테리얼 변경
    private void UpdatePreviewInstancesPosition(Vector2 basePosition)
    {

        //드래그 도중 합성된 유닛이 있다면 원래 상태로 변경해야됨

        // 합성된 유닛이 있다면 원래 상태로 복원
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            if (currentPreviews[i] != originalPreviews[i])
            {
                // 해당 위치의 기존 배치된 유닛의 별 다시 활성화
                Vector2 position = previousTilePosition + tileOffsets[i];
                var tileData = TileMapManager.Instance.GetTileData(position);

                foreach (var star in tileData.placedUnit.starObjects)
                {
                    star.SetActive(true);
                }

                PoolingManager.Instance.ReturnObject(currentPreviews[i].gameObject);
                currentPreviews[i] = originalPreviews[i];
                currentPreviews[i].gameObject.SetActive(true);
            }
        }

        //차례대로 타일 읽어오기
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            Vector2 previewPosition = basePosition + tileOffsets[i];

            var tileData = TileMapManager.Instance.GetTileData(previewPosition);

            var currentPreview = currentPreviews[i];

            // 타일에 유닛이 있고 합성이 가능한 경우(유닛타입이 똑같고 다음 업그레이드할 유닛이 있는경우)
            if (tileData?.placedUnit != null &&
                currentPreview.unitType == tileData.placedUnit.unitType &&
                 currentPreview.GetStat(StatName.UnitStarLevel) == tileData.placedUnit.GetStat(StatName.UnitStarLevel) &&
                tileData.placedUnit.GetStat(StatName.UnitStarLevel) < 3)
            {
                foreach (var star in tileData.placedUnit.starObjects)
                {
                    star.SetActive(false);
                }

                Vector3 mergedPosition = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);

                // 기존 프리뷰 비활성화
                currentPreview.gameObject.SetActive(false);

                // 새로운 합성 유닛 생성 및 설정
                GameObject mergedPreview = PoolingManager.Instance.GetObject(currentPreview.unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey, mergedPosition, (int)ObjectLayer.Player);
                var mergedUnit = mergedPreview.GetComponent<UnitController>();
                mergedUnit.Initialize();
                mergedUnit.InitializeUnitInfo(currentPreview.unitData);
                

                int newStarLevel = (int)tileData.placedUnit.GetStat(StatName.UnitStarLevel) + 1;
                mergedUnit.UpGradeUnitLevel(newStarLevel);
                
                mergedUnit.SetPreviewMaterial(canPlace);
                mergedUnit.unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
                // 현재 프리뷰를 합성된 것으로 교체
                currentPreviews[i] = mergedUnit;

            }
            else
            {
           


                currentPreview.gameObject.SetActive(true);
                currentPreview.transform.position = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);
                currentPreview.SetPreviewMaterial(canPlace);
            }
        }
    }


    // 유닛 배치: 프리뷰를 실제 유닛으로 전환하고 게임 상태 업데이트
    private void PlaceUnits()
    {
        // 먼저 합성이 일어날 위치의 기존 유닛들만 제거
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            // 현재 프리뷰가 원본과 다르다면 합성이 일어난 위치
            if (currentPreviews[i] != originalPreviews[i])
            {
                Vector2 position = previousTilePosition + tileOffsets[i];
                var tileData = TileMapManager.Instance.GetTileData(position);

                if (tileData?.placedUnit != null)
                {
                    UnitManager.Instance.UnregisterUnit(tileData.placedUnit);
                    
                }
            }
        }


        // 그 다음 현재 프리뷰들을 배치
        foreach (var pair in currentPreviews)
        {
            var unitInstance = pair.Value;

            unitInstance.DestroyPreviewUnit();
            UnitManager.Instance.RegisterUnit(unitInstance);
        }

        //타일 배치 불가상태로 변경, 코스트 사용
        TileMapManager.Instance.OccupyTiles(previousTilePosition, tileOffsets, currentPreviews);

        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = cardCost * -1, // 음수로 소모
            multiply = 1f
        });
        
        //enemy path 업데이트
        EnemyManager.Instance.UpdateEnemiesPath();

        // 사용된 카드 제거 -> 이벤트 통해서
        OnCardUsed?.Invoke(transform.parent.gameObject);

        originalPreviews.Clear();
        currentPreviews.Clear();
        Destroy(gameObject);
    }

    // 배치 취소시 호출 메서드: 프리뷰 인스턴스 정리
    private void CancelPlacement()
    {
        foreach (var instance in currentPreviews.Values)
        {
            PoolingManager.Instance.ReturnObject(instance.gameObject);
        }
        originalPreviews.Clear();
        currentPreviews.Clear();

        GameManager.Instance.OnCostAdd -= OnCostChanged;
    }

    // 카드 UI에 타일 프리뷰 생성
    private void CreateTilePreview(D_TileShpeData tileShapeData)
    {
        // 기존 타일 이미지 모두 제거
        ClearTileImages();

        // 타일 데이터에서 최소/최대 x, y 좌표 찾기
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var buildData in tileShapeData.f_unitBuildData)
        {
            Vector2 tilePos = buildData.f_TilePos.f_TilePos;
            minX = Mathf.Min(minX, (int)tilePos.x);
            maxX = Mathf.Max(maxX, (int)tilePos.x);
            minY = Mathf.Min(minY, (int)tilePos.y);
            maxY = Mathf.Max(maxY, (int)tilePos.y);
        }

        // 그리드 크기 계산
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

     
        // tileImageLayout의 RectTransform
        RectTransform layoutRect = tileImageLayout.GetComponent<RectTransform>();

        // 전체 레이아웃 크기 설정 (타일 크기 * 그리드 크기 + 간격)
        float tileSize = 100f;
        float spacing = 10f; // 0.1 * tileSize
        layoutRect.sizeDelta = new Vector2(width * (tileSize + spacing) - spacing, height * (tileSize + spacing) - spacing);

        // 유닛 데이터를 그리드 위치에 매핑
        Dictionary<Vector2Int, D_unitBuildData> unitPositionMap = new Dictionary<Vector2Int, D_unitBuildData>();

        foreach (var buildData in tileShapeData.f_unitBuildData)
        {
            Vector2Int normalizedPos = new Vector2Int(
                (int)buildData.f_TilePos.f_TilePos.x - minX,
                (int)buildData.f_TilePos.f_TilePos.y - minY
            );
            unitPositionMap[normalizedPos] = buildData;
        }

        // 전체 그리드를 순회하며 이미지 생성 (빈 칸 포함)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Y축 반전 적용 (아래에서 위로 증가하는 좌표계)
                Vector2Int gridPos = new Vector2Int(x, height - 1 - y);

                // 타일 프리팹 생성
                GameObject tileImageObj = Instantiate(tileImagePrefab, tileImageLayout.transform);

                // UnitTileObject 컴포넌트 가져오기
                UnitTileObject tileObject = tileImageObj.GetComponent<UnitTileObject>();

                if (tileObject != null)
                {
                    // 위치 설정
                    float posX = (x - (width - 1) / 2f) * (tileSize + spacing);
                    float posY = ((height - 1) / 2f - y) * (tileSize + spacing);
                    tileObject.SetPosition(posX, posY);

                    // 이미지 리스트에 타일 이미지 컴포넌트 추가 (UnitTileObject의 unitImage 참조)
                    Image tileImage = tileObject.unitImage;

                    if (tileImage != null)
                    {
                        tileImages.Add(tileImage);
                        activeImageIndices.Add(tileImages.Count - 1);
                    }

                    // 해당 위치에 유닛 데이터가 있는지 확인
                    if (unitPositionMap.TryGetValue(gridPos, out var buildData))
                    {
                        // 유닛 스프라이트 가져오기
                        GameObject tempUnit = PoolingManager.Instance.GetObject(buildData.f_unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey);
                        UnitController unitController = tempUnit.GetComponent<UnitController>();

                        if (unitController != null && unitController.unitSprite != null)
                        {
                            // SkillAttackType 체크
                            bool isBase = (buildData.f_unitData.f_SkillAttackType == SkillAttackType.None);

                            Sprite unitSprite = null;

                            // base가 아닌 경우에만 스프라이트 설정
                            if (!isBase)
                            {
                                unitSprite = unitController.unitSprite.sprite;
                            }
                            // 타일 초기화 (base 정보 전달)
                            tileObject.InitUnitImage(unitSprite, isBase);
                        }

                        PoolingManager.Instance.ReturnObject(tempUnit);
                    }
                    else
                    {
                        // 빈 타일 초기화
                        tileObject.InitUnitImage(null, false);
                    }
                }

            }
        }

        Debug.Log($"타일 프리뷰 생성 완료: 총 {tileShapeData.f_unitBuildData.Count}개의 유닛, 그리드 크기 {width}x{height}");
    }

    // 기존 타일 이미지 모두 제거하는 메서드
    private void ClearTileImages()
    {
        activeImageIndices.Clear();

        // 리스트에 있는 이미지 컴포넌트 삭제
        foreach (var image in tileImages)
        {
            if (image != null && image.gameObject != null)
            {
                Destroy(image.gameObject);
            }
        }

        // 리스트 초기화
        tileImages.Clear();

        // 혹시 남아있는 자식 오브젝트도 모두 제거
        for (int i = tileImageLayout.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(tileImageLayout.transform.GetChild(i).gameObject);
        }
    }


    private void CancelDragging()
    {
        if (isDragging)
        {
            SetUIVisibility(true);
            CancelPlacement();
            isDragging = false;
        }
    }

    private void CleanUp()
    {
        ClearTileImages(); // 타일 이미지 제거

        originalPreviews.Clear();
        currentPreviews.Clear();
        activeImageIndices.Clear();

        // 이벤트 구독 해제
        GameManager.Instance.OnCostAdd -= OnCostChanged;
        StageManager.Instance.OnWaveFinish -= CancelDragging;

    }


}