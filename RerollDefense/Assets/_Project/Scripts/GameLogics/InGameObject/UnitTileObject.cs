using BGDatabaseEnum;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 유닛 타일 UI 요소를 관리하고 드래그 앤 드롭으로 맵에 유닛을 배치하는 클래스
public class UnitTileObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{


    //UI요소중 9개의 타일 이미지 저장하는 리스트
    [SerializeField] private List<Image> tileImages = new List<Image>();

    [SerializeField] private TMP_Text cardCostText;

    // 드래그 중에 보여질 프리뷰 유닛들을 관리
    private List<UnitController> previewInstances = new List<UnitController>();

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
        tileShapeName = unitData.f_name;
        cardCost = unitData.f_Cost;
        cardCostText.text = cardCost.ToString();

        InitializeTileShape();
        CreateTilePreview(unitData);
        //cost 사용 이벤트 구독 -> cost 추가될때 tile Cost Text 업데이트
        GameManager.Instance.OnCostAdd += OnCostChanged;

        UpdateCostTextColor();
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
                tileOffsets.Add(tile.f_TilePos.f_TilePos);
            }
        }
    }

    //unitCard 드래그했을시 안보여야됨
    private void SetUIVisibility(bool visible)
    {
        foreach (int index in activeImageIndices)
        {
            tileImages[index].gameObject.SetActive(visible);
        }
        cardCostText.gameObject.SetActive(visible);
    }

    // 코스트 변경시 호출될 메서드
    private void OnCostChanged()
    {
        UpdateCostTextColor();
    }

    //cost 색 -> 변경 가능 : 하얀색, 불가능 : 빨간색
    private void UpdateCostTextColor()
    {
        cardCostText.color = GameManager.Instance.CurrentCost >= cardCost ? Color.white : Color.red;
    }

    // 드래그 시작: 코스트 체크 후 프리뷰 인스턴스 생성
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.Instance.CurrentCost < cardCost) return;

        isDragging = true;
        canPlace = false;
        hasDragged = false;  // 드래그 시작 시 false로 초기화
        SetUIVisibility(false);  // UI 숨기기

        // 마우스 커서의 월드 좌표 계산

        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);
        Vector3 centerPos = TileMapManager.Instance.GetTileToWorldPosition(tilePos);
        CreatePreviewInstances(centerPos);
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
            canPlace = TileMapManager.Instance.CanPlaceObject(tilePos, tileOffsets, previewInstances);
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
        canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, tileOffsets, previewInstances);

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
    private void CreatePreviewInstances(Vector3 pointerPosition)
    {
        var tileShapeData = D_TileShpeData.GetEntity(tileShapeName);

        for (int i = 0; i < tileOffsets.Count; i++)
        {
            var unitBuildData = tileShapeData.f_unitBuildData[i];
            var unitPoolinKey = unitBuildData.f_unitData.f_UnitPoolingKey;
            
            GameObject previewInstance = PoolingManager.Instance.GetObject(unitPoolinKey.f_PoolObjectAddressableKey, pointerPosition);

            UnitController previewUnit = previewInstance.GetComponent<UnitController>();

            if (previewUnit != null)
            {
                previewUnit.Initialize();
                previewUnit.InitializeUnitData(unitBuildData.f_unitData);
                previewInstances.Add(previewUnit);
            }
        }
    }



    // 프리뷰 위치 업데이트: 현재 마우스 위치에 따라 프리뷰 위치 조정, 배치불가에 따라 머테리얼 변경
    private UnitController temporaryMergedUnit; // 임시 합성 유닛을 저장하는 변수

    private void UpdatePreviewInstancesPosition(Vector2 basePosition)
    {
        // 기존 합성 유닛 반환
        if (temporaryMergedUnit != null)
        {
            PoolingManager.Instance.ReturnObject(temporaryMergedUnit.gameObject);
            temporaryMergedUnit = null;
        }

        for (int i = 0; i < previewInstances.Count; i++)
        {
            Vector2 previewPosition = basePosition + tileOffsets[i];
            var tileData = TileMapManager.Instance.GetTileData(previewPosition);
            var previewUnit = previewInstances[i];

            // 합성 가능한 경우 처리
            if (tileData?.placedUnit != null)
            {
                var placedUnit = tileData.placedUnit;

                if (previewUnit.upgradeUnitType == placedUnit.upgradeUnitType &&
                    previewUnit.upgradeUnitType != UpgradeUnitType.None)
                {
                    var unitData = D_UnitData.GetEntityByKeyUpgradeUnitKey(previewUnit.upgradeUnitType);
                    var poolingKey = unitData.f_UpgradePoolingKey;
                    var objectPool = poolingKey.f_PoolObjectAddressableKey;

                    // 정확한 타일 위치로 이동
                    Vector3 mergedUnitPosition = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);

                    GameObject newPreview = PoolingManager.Instance.GetObject(objectPool, mergedUnitPosition);
                    temporaryMergedUnit = newPreview.GetComponent<UnitController>();

                    if (temporaryMergedUnit != null)
                    {
                        temporaryMergedUnit.Initialize();
                        temporaryMergedUnit.InitializeUnitData(unitData);
                        temporaryMergedUnit.SetPreviewMaterial(canPlace);

                    }

                    // 기존 프리뷰 유닛 숨김
                    previewUnit.gameObject.SetActive(false);
                    continue;
                }
            }

            // 기본 프리뷰 유닛 업데이트
            previewUnit.transform.position = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);
            previewUnit.SetPreviewMaterial(canPlace);
            previewUnit.gameObject.SetActive(true); // 숨겨졌던 유닛 다시 활성화


        }
    }


    // 유닛 배치: 프리뷰를 실제 유닛으로 전환하고 게임 상태 업데이트
    private void PlaceUnits()
    {
        // 합성된 유닛이 있는 경우
        if (temporaryMergedUnit != null)
        {
            Vector3Int tilePosition = TileMapManager.Instance.tileMap.WorldToCell(temporaryMergedUnit.transform.position);
            Vector2 unitTilePos = new Vector2(tilePosition.x, tilePosition.y);

            // 기존 배치된 유닛 제거 (합성 대상)
            var tileData = TileMapManager.Instance.GetTileData(unitTilePos);
            if (tileData?.placedUnit != null)
            {
                var placedUnit = tileData.placedUnit;
                UnitManager.Instance.UnregisterUnit(placedUnit);
                PoolingManager.Instance.ReturnObject(placedUnit.gameObject);
            }

            // 합성된 유닛 등록
            temporaryMergedUnit.DestroyPreviewUnit(); // 머티리얼 복원
            temporaryMergedUnit.InitializeTilePos(unitTilePos);
            UnitManager.Instance.RegisterUnit(temporaryMergedUnit);

            // 타일 데이터 업데이트
            TileMapManager.Instance.SetTileData(new TileData(unitTilePos)
            {
                isAvailable = false,
                placedUnit = temporaryMergedUnit
            });

            temporaryMergedUnit = null; // 참조 초기화
        }

        // 나머지 기본 유닛 배치 처리
        foreach (var previewUnit in previewInstances)
        {
            Vector3Int cellPosition = TileMapManager.Instance.tileMap.WorldToCell(previewUnit.transform.position);
            Vector2 tilePosition = new Vector2(cellPosition.x, cellPosition.y);

            // 타일 데이터 가져오기
            var tileData = TileMapManager.Instance.GetTileData(tilePosition);

            // 해당 타일에 다른 유닛이 있으면 스킵 (이미 처리된 합성된 유닛)
            if (tileData?.placedUnit != null) continue;

            // 기본 유닛 등록
            previewUnit.DestroyPreviewUnit(); // 머티리얼 복원
            previewUnit.InitializeTilePos(tilePosition);
            UnitManager.Instance.RegisterUnit(previewUnit);

            // 타일 데이터 업데이트
            TileMapManager.Instance.SetTileData(new TileData(tilePosition)
            {
                isAvailable = false,
                placedUnit = previewUnit
            });
        }

        // 코스트 소모 처리 및 UI 제거
        GameManager.Instance.UseCost(cardCost);
        EnemyManager.Instance.UpdateEnemiesPath();

        // 이벤트를 통해 카드 사용 알림
        OnCardUsed?.Invoke(transform.parent.gameObject);

        previewInstances.Clear();
        Destroy(gameObject);
    }


    // 배치 취소: 프리뷰 인스턴스 정리
    private void CancelPlacement()
    {
        foreach (var instance in previewInstances)
        {
            PoolingManager.Instance.ReturnObject(instance.gameObject);
        }
        previewInstances.Clear();
    }

    // 카드 UI에 타일 프리뷰 생성
    private void CreateTilePreview(D_TileShpeData tileShapeData)
    {
        foreach (var image in tileImages)
        {
            image.gameObject.SetActive(false);
        }

        activeImageIndices.Clear();  // 기존 인덱스 초기화


        foreach (var buildData in tileShapeData.f_unitBuildData)
        {
            Vector2 tilePos = buildData.f_TilePos.f_TilePos;

            // 3x3 그리드에서 중앙을 (0,0)으로 보고 인덱스 계산
            int x = Mathf.RoundToInt(tilePos.x) + 1;
            int y = Mathf.RoundToInt(-tilePos.y) + 1;
            int index = y * 3 + x;

            if (index >= 0 && index < tileImages.Count)
            {
                GameObject tempUnit = PoolingManager.Instance.GetObject(buildData.f_unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey);

                UnitController unitController = tempUnit.GetComponent<UnitController>();

                if (unitController != null && unitController.unitSprite != null)
                {
                    tileImages[index].gameObject.SetActive(true);

                    if (buildData.f_unitData.f_SkillAttackType != SkillAttackType.None)
                    {
                        tileImages[index].sprite = unitController.unitSprite.sprite;
                    }
                    
                    activeImageIndices.Add(index);  // 활성화된 인덱스 저장
                }

                PoolingManager.Instance.ReturnObject(tempUnit);
            }
        }
    }

    // 리소스 정리
    private void OnDestroy()
    {
        if (isDragging)
        {
            CancelPlacement();
        }

        GameManager.Instance.OnCostAdd -= OnCostChanged;

    }

}