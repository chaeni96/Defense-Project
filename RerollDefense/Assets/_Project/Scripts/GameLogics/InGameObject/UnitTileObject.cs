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
    private List<Vector3Int> tileOffsets;
    private Vector3Int previousTilePosition;
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
        UpdateCostTextColor();
        CreateTilePreview(unitData);

        //cost 사용 이벤트 구독 -> cost 추가될때 tile Cost Text 업데이트
        GameManager.Instance.OnCostAdd += OnCostChanged;
    }

    // 타일 모양 데이터를 기반으로 오프셋 위치 초기화
    private void InitializeTileShape()
    {
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            tileOffsets = new List<Vector3Int>();
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                Vector2 tilePos = tile.f_TilePos.f_TilePos;
                Vector3Int tileOffset = new Vector3Int(
                    Mathf.RoundToInt(tilePos.x),
                    Mathf.RoundToInt(tilePos.y),
                    0
                );
                tileOffsets.Add(tileOffset);
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
        Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        pointerPosition.z = 0;



        // 타일 위치 계산
        Vector3Int tilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);

        Vector3 centerPosition = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);

        CreatePreviewInstances(centerPosition);
        UpdatePreviewInstancesPosition(tilePosition);  // 프리뷰 위치 즉시 업데이트
    }

    // 드래그 중: 프리뷰 위치 업데이트 및 배치 가능 여부 표시
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;


        hasDragged = true;  // 드래그가 발생했음을 표시

        Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        pointerPosition.z = 0;
        Vector3Int baseTilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);

        if (baseTilePosition != previousTilePosition)
        {
            //TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0.1f));
            canPlace = TileMapManager.Instance.CanPlaceObject(baseTilePosition, tileOffsets);

            //canPlace에 따라 배치 가능불가능 판정
            UpdatePreviewInstancesPosition(baseTilePosition);
            previousTilePosition = baseTilePosition;
        }
    }

    // 드래그 종료: 배치 가능 여부 확인 후 유닛 배치 또는 취소
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        //배치 타일 투명화
        //TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0));

        //배치 가능한지 체크
        canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, tileOffsets);

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
            GameObject previewInstance = PoolingManager.Instance.GetObject(unitBuildData.f_unitData.f_unitPrefabKey,pointerPosition);

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
    private void UpdatePreviewInstancesPosition(Vector3Int basePosition)
    {
        for (int i = 0; i < previewInstances.Count; i++)
        {
            Vector3Int previewPosition = basePosition + tileOffsets[i];
            previewInstances[i].transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(previewPosition);
            previewInstances[i].SetPreviewMaterial(canPlace);
            
        }
    }

    // 유닛 배치: 프리뷰를 실제 유닛으로 전환하고 게임 상태 업데이트
    private void PlaceUnits()
    {
        foreach (var unitInstance in previewInstances)
        {
            unitInstance.DestroyPreviewUnit();

            UnitManager.Instance.RegisterUnit(unitInstance);
           
        }

        //타일 배치 불가상태로 변경, 코스트 사용
        TileMapManager.Instance.OccupyTile(previousTilePosition, tileOffsets);
        GameManager.Instance.UseCost(cardCost);
        //enemy path 업데이트
        EnemyManager.Instance.UpdateEnemiesPath();

        // 사용된 카드 제거 -> 이벤트 통해서
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
                GameObject tempUnit = PoolingManager.Instance.GetObject(buildData.f_unitData.f_unitPrefabKey);

                UnitController unitController = tempUnit.GetComponent<UnitController>();

                if (unitController != null && unitController.unitSprite != null)
                {
                    tileImages[index].gameObject.SetActive(true);

                    if (buildData.f_unitData.f_SkillAttackType != SkillAttackType.None)
                    {
                        tileImages[index].sprite = unitController.unitSprite.sprite;
                        activeImageIndices.Add(index);  // 활성화된 인덱스 저장
                    }
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