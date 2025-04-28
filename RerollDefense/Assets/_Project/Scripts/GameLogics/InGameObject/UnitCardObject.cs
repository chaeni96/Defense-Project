using BGDatabaseEnum;
using CatDarkGame.PerObjectRTRenderForUGUI;
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

    //[SerializeField] private GameObject unitImageObject;
    [SerializeField] private PerObjectRTRenderer unitRTObject;
    [SerializeField] private GameObject unitTraitObject;
    [SerializeField] private GameObject costImageObject;
    [SerializeField] private List<Image> tileImages = new List<Image>();
    [SerializeField] private TMP_Text cardCostText;

    //프리뷰 유닛들을 관리, 합성 여부 판단, 드래그 도중 상태 복원 및 업데이트

    //TileData의 프리뷰 유닛
    private Dictionary<int, UnitController> originalPreviews = new Dictionary<int, UnitController>();

    //현재 화면에 보여지는 프리뷰 유닛, 합성 여부에 따라 기존 프리뷰 교체, 위치 변경, 머터리얼 업데이트
    private Dictionary<int, UnitController> currentPreviews = new Dictionary<int, UnitController>();

    // 확장 타일 프리뷰를 저장할 Dictionary 추가
    private Dictionary<Vector2, TileExtensionObject> extensionPreviews = new Dictionary<Vector2, TileExtensionObject>();


    // 기준점 (0,0)으로부터의 오프셋값들(다중타일을 위함)
    private List<Vector2> tileOffsets;
    private Vector2 previousTilePosition;
    private string tileCardName;
    private int cardCost;
    private bool isDragging = false;
    private bool hasDragged = false;  // 실제 드래그 여부를 체크하는 변수 추가
    private bool canPlace = false;
    private PerObjectRTSource rtSource;

    // 활성화되었던 이미지의 인덱스를 저장할 리스트 추가
    private List<int> activeImageIndices = new List<int>();

    // 카드가 사용되었을 때 발생하는 이벤트 (사용된 카드덱을 매개변수로 전달)
    public static event System.Action<GameObject> OnCardUsed;


    // 카드 초기화: 타일 정보, 코스트, 프리뷰 설정
    public void InitializeCardInform(D_TileCardData tileCardData)
    {
        CleanUp();

        tileCardName = tileCardData.f_name;
        cardCost = tileCardData.f_Cost;
        cardCostText.text = cardCost.ToString();

        InitializeTileShape();
        UpdateCostTextColor();
        CreateTilePreview(tileCardData);

        //cost 사용 이벤트 구독 -> cost 추가될때 tile Cost Text 업데이트
        GameManager.Instance.OnCostAdd += OnCostChanged;

        StageManager.Instance.OnWaveFinish += CancelDragging;

        rtSource = CreatePreviewInstances(new Vector3(3000f, 0f)).GetComponent<PerObjectRTSource>();

        if(rtSource != null && unitRTObject != null)
        {
            unitRTObject.source = rtSource;
            rtSource.CalculateAutoBounds();

        }

    }

    // 타일 모양 데이터를 기반으로 오프셋 위치 초기화
    private void InitializeTileShape()
    {
        var tileCardData = D_TileCardData.FindEntity(data => data.f_name == tileCardName);

        if (tileCardData != null)
        {
            tileOffsets = new List<Vector2>();

            foreach(var tile in tileCardData.f_unitBuildData)
            {
                //원본 좌표
                Vector2 originalPos = tile.f_TilePosData.f_TilePos;

                //2D 좌표계는 아래쪽으로 갈수록 Y값 증가하므로 Y축 반전
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
        unitRTObject.gameObject.SetActive(visible);
        unitTraitObject.SetActive(visible);
        costImageObject.SetActive(visible);
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

        GameManager.Instance.PrepareUseCost(cardCost);

        SetUIVisibility(false);  // UI 숨기기

        // 마우스 커서의 월드 좌표 계산
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        worldPos.y += 0.2f; // 오프셋 조정
        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);
        CreatePreviewInstances(tilePos);
        UpdatePreviewInstancesPosition(tilePos);
    }

    // 드래그 중: 프리뷰 위치 업데이트 및 배치 가능 여부 표시
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        hasDragged = true;
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        worldPos.y += 0.1f; // 오프셋 조정
        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        if (tilePos != previousTilePosition)
        {
            TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0.1f));

            // 멀티타일 유닛 확인
            UnitController multiTileUnit = null;
            foreach (var pair in originalPreviews)
            {
                if (pair.Value.isMultiUnit)
                {
                    multiTileUnit = pair.Value;
                    break;
                }
            }

            // 멀티타일 유닛인 경우
            if (multiTileUnit != null)
            {
                MultiTileUnitController multiController = multiTileUnit as MultiTileUnitController;
                if (multiController != null)
                {
                    // 멀티타일 유닛 합성 가능 여부 확인
                    TileData tileData = TileMapManager.Instance.GetTileData(tilePos);
                    if (tileData?.placedUnit != null && tileData.placedUnit is MultiTileUnitController)
                    {
                        MultiTileUnitController targetMultiUnit = tileData.placedUnit as MultiTileUnitController;

                        // 합성 가능 여부 확인 로직
                        bool canMerge = CheckMultiTileMergePossibility(multiController, targetMultiUnit, tilePos);

                        if (canMerge)
                        {
                            // 합성 프리뷰 표시
                            ShowMultiTileMergePreview(multiController, targetMultiUnit);
                            canPlace = true;
                            previousTilePosition = tilePos;
                            return;
                        }
                    }
                }

                // 합성이 불가능한 경우 일반 배치 가능 여부 확인
                canPlace = CheckMultiTilePlacement(tilePos, multiTileUnit);
            }
            else
            {
                // 일반 유닛 처리 (기존 코드)
                canPlace = TileMapManager.Instance.CanPlaceObject(tilePos, tileOffsets, originalPreviews);
            }

            UpdatePreviewInstancesPosition(tilePos);
            previousTilePosition = tilePos;
        }
    }


    // 대형 유닛의 배치 가능 여부 확인 메서드 추가
    private bool CheckMultiTilePlacement(Vector2 basePosition, UnitController multiTileUnit)
    {
        // tileOffsets 사용
        foreach (var offset in tileOffsets)
        {
            Vector2 tilePos = basePosition + offset;
            TileData tileData = TileMapManager.Instance.GetTileData(tilePos);

            // 타일이 없거나 사용 불가능한 경우
            if (tileData == null || !tileData.isAvailable)
            {
                // 이미 자신이 점유한 타일이 아니라면 배치 불가
                if (tileData?.placedUnit != multiTileUnit)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private bool CheckMultiTileMergePossibility(MultiTileUnitController previewUnit, MultiTileUnitController targetUnit, Vector2 basePosition)
    {
        // 유닛 타입 및 레벨 확인
        if (previewUnit.unitType != targetUnit.unitType ||
            previewUnit.GetStat(StatName.UnitStarLevel) != targetUnit.GetStat(StatName.UnitStarLevel) ||
            targetUnit.GetStat(StatName.UnitStarLevel) >= 5)
        {
            return false;
        }

        // 모든 타일이 겹치는지 확인
        HashSet<Vector2> previewTilePositions = new HashSet<Vector2>();
        HashSet<Vector2> targetTilePositions = new HashSet<Vector2>();

        // 프리뷰 유닛의 모든 타일 위치 계산
        foreach (var offset in previewUnit.multiTilesOffset)
        {
            previewTilePositions.Add(basePosition + offset);
        }

        // 타겟 유닛의 모든 타일 위치 계산
        foreach (var offset in targetUnit.multiTilesOffset)
        {
            targetTilePositions.Add(targetUnit.tilePosition + offset);
        }

        // 두 집합이 완전히 같은지 확인
        return previewTilePositions.SetEquals(targetTilePositions);
    }

    private void ShowMultiTileMergePreview(MultiTileUnitController previewUnit, MultiTileUnitController targetUnit)
    {
        // 타겟 유닛의 별 비활성화
        foreach (var star in targetUnit.starObjects)
        {
            star.SetActive(false);
        }

        // 프리뷰 유닛 레벨 업그레이드
        int currentLevel = (int)previewUnit.GetStat(StatName.UnitStarLevel);
        int newLevel = currentLevel + 1;
        previewUnit.UpdateStarDisplay(newLevel);

        // 프리뷰 유닛 위치 조정
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(targetUnit.tilePosition);
        targetPosition.z = -0.1f;
        previewUnit.transform.position = targetPosition;

        // 확장 타일 위치 업데이트 - 이벤트 직접 호출 대신 UpdateExtensionObjects 메서드 사용
        // 기존: previewUnit.OnPositionChanged?.Invoke(targetPosition);
        previewUnit.UpdateExtensionObjects();

        // 프리뷰 머테리얼 설정
        previewUnit.SetPreviewMaterial(true);

        // 시각적 효과
        //previewUnit.unitSprite.transform.DOKill();
        //previewUnit.unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
    }

    private void PerformMultiTileMerge(MultiTileUnitController previewUnit, MultiTileUnitController targetUnit)
    {
        // 타겟 유닛 업그레이드
        int newStarLevel = (int)previewUnit.GetStat(StatName.UnitStarLevel) + 1;
        targetUnit.UpGradeUnitLevel(newStarLevel);

        // 효과 적용
        targetUnit.ApplyEffect(1.0f);

        // 코스트 사용
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = cardCost * -1,
            multiply = 1f
        });

        // 사용된 카드 제거
        OnCardUsed?.Invoke(transform.parent.gameObject);

        // 프리뷰 객체 정리
        foreach (var instance in currentPreviews.Values)
        {
            PoolingManager.Instance.ReturnObject(instance.gameObject);
        }

        foreach (var extObj in extensionPreviews.Values)
        {
            if (extObj != null)
            {
                PoolingManager.Instance.ReturnObject(extObj.gameObject);
            }
        }

        originalPreviews.Clear();
        currentPreviews.Clear();
        extensionPreviews.Clear();

        Destroy(gameObject);
    }

    // 드래그 종료: 배치 가능 여부 확인 후 유닛 배치 또는 취소
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        //배치 타일 투명화
        TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0));

        // 멀티타일 유닛 확인
        UnitController multiTileUnit = null;
        foreach (var pair in originalPreviews)
        {
            if (pair.Value.isMultiUnit)
            {
                multiTileUnit = pair.Value;
                break;
            }
        }

        if (multiTileUnit != null)
        {
            MultiTileUnitController multiController = multiTileUnit as MultiTileUnitController;
            if (multiController != null)
            {
                // 합성 가능 여부 확인
                TileData tileData = TileMapManager.Instance.GetTileData(previousTilePosition);
                if (tileData?.placedUnit != null && tileData.placedUnit is MultiTileUnitController)
                {
                    MultiTileUnitController targetMultiUnit = tileData.placedUnit as MultiTileUnitController;

                    if (CheckMultiTileMergePossibility(multiController, targetMultiUnit, previousTilePosition))
                    {
                        // 합성 수행
                        PerformMultiTileMerge(multiController, targetMultiUnit);
                        isDragging = false;
                        return;
                    }
                }
            }

            // 일반 배치 처리
            canPlace = CheckMultiTilePlacement(previousTilePosition, multiTileUnit);
        }
        else
        {
            canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, tileOffsets, originalPreviews);
        }

        if (hasDragged && canPlace)
        {
            PlaceUnits();
        }
        else
        {
            SetUIVisibility(true);  // 배치 실패시 UI 다시 보이게
            UnitInstanceViewOut();
        }

        isDragging = false;

        GameManager.Instance.CanclePrepareUseCost();
    }

    // 프리뷰 인스턴스 생성: 각 타일 위치에 대한 프리뷰 유닛 생성
    private UnitController CreatePreviewInstances(Vector3 tilePos)
    {
        var tileCardData = D_TileCardData.GetEntity(tileCardName);
        bool isMultiUnit = tileCardData.f_isMultiTileUinit;

        // 기준 유닛 찾기 (TilePos 0,0)
        D_unitBuildData baseUnitBuildData = null;
        foreach (var buildData in tileCardData.f_unitBuildData)
        {
            if (buildData.f_TilePosData.f_TilePos == Vector2.zero)
            {
                baseUnitBuildData = buildData;
                break;
            }
        }

        if (isMultiUnit && baseUnitBuildData != null)
        {
            // 멀티 유닛 생성 (기준 유닛)
            var unitPoolingKey = baseUnitBuildData.f_unitData.f_UnitPoolingKey;
            Vector3 worldPos = TileMapManager.Instance.GetTileToWorldPosition(tilePos);

            GameObject previewInstance = PoolingManager.Instance.GetObject(
                unitPoolingKey.f_PoolObjectAddressableKey,
                worldPos,
                (int)ObjectLayer.Player
            );

            // UnitController를 MultiTileUnitController로 교체
            UnitController oldController = previewInstance.GetComponent<UnitController>();
            MultiTileUnitController multiController = null;

            if (oldController != null && !(oldController is MultiTileUnitController))
            {
                // TODO : 멀티타일용 유닛오브젝트 만들면 이부분 꼭 빼기!!!!!!! 지금은 유닛 프리팹에 UnitController만 들어있어서 이렇게 한거임
                // 기존 컴포넌트의 중요 데이터 보존
                //SpriteRenderer unitSprite = oldController.unitSprite;
                //SpriteRenderer unitBaseSprite = oldController.unitBaseSprite;
                GameObject unitStarObject = oldController.unitStarObject;
                Material enabledMaterial = oldController.enabledMaterial;
                Material disabledMaterial = oldController.disabledMaterial;
                Material deleteMaterial = oldController.deleteMaterial;
                Material originalMaterial = oldController.originalMaterial;
                LayerMask unitLayer = oldController.unitLayer;

                // 새 MultiTileUnitController 추가
                multiController = previewInstance.AddComponent<MultiTileUnitController>();

                // 기존 데이터 복사
                //multiController.unitSprite = unitSprite;
                //multiController.unitBaseSprite = unitBaseSprite;
                multiController.unitStarObject = unitStarObject;
                multiController.enabledMaterial = enabledMaterial;
                multiController.disabledMaterial = disabledMaterial;
                multiController.deleteMaterial = deleteMaterial;
                multiController.originalMaterial = originalMaterial;
                multiController.unitLayer = unitLayer;

                // 기존 컴포넌트 제거 (Initialize 전에 해야 함)
                Destroy(oldController);

                // 초기화 및 UnitData 설정
                multiController.Initialize();
                multiController.InitializeUnitInfo(baseUnitBuildData.f_unitData, tileCardData);

                // 점유 타일 정보 설정
                multiController.multiTilesOffset.Clear();
                foreach (var offset in tileOffsets)
                {
                    multiController.multiTilesOffset.Add(offset);
                }

                originalPreviews[0] = multiController;
                currentPreviews[0] = multiController;
            }
            else if (oldController is MultiTileUnitController)
            {
                // 이미 MultiTileUnitController인 경우
                multiController = (MultiTileUnitController)oldController;
                originalPreviews[0] = multiController;
                currentPreviews[0] = multiController;
            }

            // 다른 타일 위치에 확장 오브젝트 생성
            if (multiController != null)
            {
                foreach (var offset in tileOffsets)
                {
                    if (offset != Vector2.zero)
                    {
                        Vector2 extTilePos = (Vector2)tilePos + offset;
                        Vector3 extWorldPos = TileMapManager.Instance.GetTileToWorldPosition(extTilePos);

                        GameObject extPreview = PoolingManager.Instance.GetObject(
                            "TileExtensionObject",
                            extWorldPos,
                            (int)ObjectLayer.Player
                        );

                        TileExtensionObject extObject = extPreview.GetComponent<TileExtensionObject>();
                        if (extObject != null)
                        {
                            // 변경된 Initialize 메서드 호출
                            extObject.Initialize(multiController, offset);
                            extensionPreviews[offset] = extObject;

                            // 멀티타일 컨트롤러에 확장 오브젝트 추가
                            multiController.extensionObjects.Add(extObject);
                        }
                    }
                }
            }
        }
        else
        {
            // 기존 일반 유닛 생성 로직
            for (int i = 0; i < tileOffsets.Count; i++)
            {
                var unitBuildData = tileCardData.f_unitBuildData[i];
                var unitPoolingKey = unitBuildData.f_unitData.f_UnitPoolingKey;

                // 유닛 위치 계산
                Vector2 offsetPos = (Vector2)tilePos + tileOffsets[i];
                Vector3 unitPos = TileMapManager.Instance.GetTileToWorldPosition(offsetPos);

                GameObject previewInstance = PoolingManager.Instance.GetObject(
                    unitPoolingKey.f_PoolObjectAddressableKey,
                    unitPos,
                    (int)ObjectLayer.Player
                );

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

        return originalPreviews[0];
    }

    // 프리뷰 위치 업데이트: 현재 마우스 위치에 따라 프리뷰 위치 조정, 배치불가에 따라 머테리얼 변경
    private void UpdatePreviewInstancesPosition(Vector2 basePosition)
    {
        // 대형 유닛인지 확인
        UnitController multiTileUnit = null;
        foreach (var pair in originalPreviews)
        {
            if (pair.Value.isMultiUnit)
            {
                multiTileUnit = pair.Value;
                break;
            }
        }

        if (multiTileUnit != null)
        {
            // 여기에 복원 로직 추가
            MultiTileUnitController multiController = multiTileUnit as MultiTileUnitController;

            // 이전에 합성 프리뷰가 활성화되었던 경우 별 표시 원래대로 복원
            if (previousTilePosition != basePosition)
            {
                TileData previousTileData = TileMapManager.Instance.GetTileData(previousTilePosition);
                if (previousTileData?.placedUnit != null && previousTileData.placedUnit is MultiTileUnitController)
                {
                    MultiTileUnitController previousTarget = previousTileData.placedUnit as MultiTileUnitController;

                    // 타겟 유닛의 별 다시 활성화
                    foreach (var star in previousTarget.starObjects)
                    {
                        star.SetActive(true);
                    }

                    // 현재 프리뷰 유닛의 별 수준 원래대로 복원
                    int originalLevel = (int)multiController.GetStat(StatName.UnitStarLevel);
                    multiController.UpdateStarDisplay(originalLevel);
                }
            }

            // 대형 유닛 처리
            multiTileUnit.gameObject.SetActive(true);
            multiTileUnit.transform.position = TileMapManager.Instance.GetTileToWorldPosition(basePosition);
            multiTileUnit.SetPreviewMaterial(canPlace);

            // 확장 오브젝트 위치 및 상태 업데이트
            foreach (var offset in tileOffsets)
            {
                if (offset != Vector2.zero && extensionPreviews.ContainsKey(offset))
                {
                    TileExtensionObject extObj = extensionPreviews[offset];
                    if (extObj != null)
                    {
                        // 위치 업데이트
                        Vector2 extTilePos = basePosition + offset;
                        Vector3 extWorldPos = TileMapManager.Instance.GetTileToWorldPosition(extTilePos);
                        extObj.transform.position = extWorldPos;
                        //extObj.tileSprite.material = multiTileUnit.unitSprite.material;
                        extObj.tileSprite.sortingOrder = 100;
                    }
                }
            }
        }
        else
        {
            // 일반 유닛 처리 - 기존 코드
            // 여기서 Dictionary 키 확인 로직 추가 필요
            // 합성된 유닛이 있다면 원래 상태로 복원
            for (int i = 0; i < tileOffsets.Count; i++)
            {
                // 키 존재 여부 확인 추가
                if (currentPreviews.ContainsKey(i) && originalPreviews.ContainsKey(i) &&
                    currentPreviews[i] != originalPreviews[i])
                {
                    // 해당 위치의 기존 배치된 유닛의 별 다시 활성화
                    Vector2 position = previousTilePosition + tileOffsets[i];
                    var tileData = TileMapManager.Instance.GetTileData(position);

                    if (tileData?.placedUnit != null)
                    {
                        foreach (var star in tileData.placedUnit.starObjects)
                        {
                            star.SetActive(true);
                        }
                    }

                    PoolingManager.Instance.ReturnObject(currentPreviews[i].gameObject);
                    currentPreviews[i] = originalPreviews[i];
                    currentPreviews[i].gameObject.SetActive(true);
                }
            }

            // 나머지 일반 유닛 처리 코드 - 여기도 키 확인 로직 추가
            for (int i = 0; i < tileOffsets.Count; i++)
            {
                // 키 존재 여부 확인 추가
                if (!currentPreviews.ContainsKey(i) || !originalPreviews.ContainsKey(i))
                    continue;

                Vector2 previewPosition = basePosition + tileOffsets[i];
                var tileData = TileMapManager.Instance.GetTileData(previewPosition);
                var currentPreview = currentPreviews[i];

                // 합성 로직
                if (tileData?.placedUnit != null &&
                    currentPreview.unitType == tileData.placedUnit.unitType &&
                    currentPreview.GetStat(StatName.UnitStarLevel) == tileData.placedUnit.GetStat(StatName.UnitStarLevel) &&
                    tileData.placedUnit.GetStat(StatName.UnitStarLevel) < 5)
                {
                    // 합성 로직 코드...
                    foreach (var star in tileData.placedUnit.starObjects)
                    {
                        star.SetActive(false);
                    }

                    Vector3 mergedPosition = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);
                    currentPreview.gameObject.SetActive(false);

                    GameObject mergedPreview = PoolingManager.Instance.GetObject(currentPreview.unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey, mergedPosition, (int)ObjectLayer.Player);
                    var mergedUnit = mergedPreview.GetComponent<UnitController>();
                    mergedUnit.Initialize();
                    mergedUnit.InitializeUnitInfo(currentPreview.unitData);

                    int newStarLevel = (int)tileData.placedUnit.GetStat(StatName.UnitStarLevel) + 1;
                    mergedUnit.UpdateStarDisplay(newStarLevel);  // 여기에서 UpGradeUnitLevel 대신 UpdateStarDisplay 사용

                    mergedUnit.SetPreviewMaterial(canPlace);
                    //mergedUnit.unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
                    currentPreviews[i] = mergedUnit;
                }
                else
                {
                    // 일반 이동
                    currentPreview.gameObject.SetActive(true);
                    currentPreview.transform.position = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);
                    currentPreview.SetPreviewMaterial(canPlace);
                }
            }
        }
    }
    // 유닛 배치: 프리뷰를 실제 유닛으로 전환하고 게임 상태 업데이트
    private void PlaceUnits()
    {
        // 멀티타일 유닛 확인
        bool isMultiUnit = false;
        MultiTileUnitController multiTileUnit = null;

        foreach (var pair in currentPreviews)
        {
            multiTileUnit = pair.Value as MultiTileUnitController;
            if (multiTileUnit != null)
            {
                isMultiUnit = true;
                break;
            }
        }

        // 먼저 합성이 일어날 위치의 기존 유닛들만 제거
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            // 현재 프리뷰가 원본과 다르다면 합성이 일어난 위치
            if (currentPreviews.ContainsKey(i) && originalPreviews.ContainsKey(i) &&
                currentPreviews[i] != originalPreviews[i])
            {
                Vector2 position = previousTilePosition + tileOffsets[i];
                var tileData = TileMapManager.Instance.GetTileData(position);

                if (tileData?.placedUnit != null)
                {
                    UnitManager.Instance.UnregisterUnit(tileData.placedUnit);
                }
            }
        }

        // 멀티타일 유닛 배치
        if (isMultiUnit && multiTileUnit != null)
        {
            multiTileUnit.DestroyPreviewUnit();
            multiTileUnit.tilePosition = previousTilePosition;
            UnitManager.Instance.RegisterUnit(multiTileUnit);

            // 대형 유닛이 점유하는 모든 타일 설정
            foreach (var offset in tileOffsets)
            {
                Vector2 tilePos = previousTilePosition + offset;
                TileData tileData = TileMapManager.Instance.GetTileData(tilePos);

                if (tileData != null)
                {
                    tileData.isAvailable = false;
                    tileData.placedUnit = multiTileUnit; // 모든 타일이 같은 유닛 참조
                    TileMapManager.Instance.SetTileData(tileData);

                    // 메인 유닛 위치(0,0)가 아닌 경우에만 실제 TileExtensionObject 배치
                    if (offset != Vector2.zero)
                    {
                        // 프리뷰 오브젝트 제거
                        if (extensionPreviews.ContainsKey(offset))
                        {
                            PoolingManager.Instance.ReturnObject(extensionPreviews[offset].gameObject);
                        }

                        // 실제 확장 오브젝트 생성
                        Vector3 worldPos = TileMapManager.Instance.GetTileToWorldPosition(tilePos);
                        GameObject extensionObject = PoolingManager.Instance.GetObject(
                            "TileExtensionObject",
                            worldPos,
                            (int)ObjectLayer.Player
                        );

                        // Extension 오브젝트 초기화
                        TileExtensionObject extension = extensionObject.GetComponent<TileExtensionObject>();
                        if (extension != null)
                        {
                            // 확장 오브젝트와 대형 유닛 연결
                            extension.Initialize(multiTileUnit, offset);

                            // MultiTileUnitController에도 확장 오브젝트 추가
                            multiTileUnit.extensionObjects.Add(extension);
                        }
                    }
                }
            }
        }
        else
        {
            // 기존 단일 타일 로직
            foreach (var pair in currentPreviews)
            {
                var unitInstance = pair.Value;
                unitInstance.DestroyPreviewUnit();
                UnitManager.Instance.RegisterUnit(unitInstance);
            }



            TileMapManager.Instance.OccupyTiles(previousTilePosition, tileOffsets, currentPreviews);
        }

        // 코스트 사용
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = cardCost * -1, // 음수로 소모
            multiply = 1f
        });

        // enemy path 업데이트
        //EnemyManager.Instance.UpdateEnemiesPath();

        // 사용된 카드 제거 -> 이벤트 통해서
        OnCardUsed?.Invoke(transform.parent.gameObject);

        originalPreviews.Clear();
        currentPreviews.Clear();
        extensionPreviews.Clear();
        Destroy(gameObject);
    }
    private void UnitInstanceViewOut()
    {
        foreach (var instance in currentPreviews.Values)
        {
            instance.transform.position = new Vector3(3000f, 0f);
        }
    }

    // 배치 취소시 호출 메서드: 프리뷰 인스턴스 정리
    private void CancelPlacement()
    {
        // 기존 프리뷰 반환 코드 유지
        foreach (var instance in currentPreviews.Values)
        {
            instance.transform.position = new Vector3(3000f, 0f);
        }

        // 확장 오브젝트 프리뷰도 반환
        foreach (var extObj in extensionPreviews.Values)
        {
            if (extObj != null)
            {
                PoolingManager.Instance.ReturnObject(extObj.gameObject);
            }
        }

        originalPreviews.Clear();
        currentPreviews.Clear();
        extensionPreviews.Clear();

        GameManager.Instance.OnCostAdd -= OnCostChanged;
    }

    // 카드 UI에 타일 프리뷰 생성
    private void CreateTilePreview(D_TileCardData tileCardData)
    {
        // 기존 타일 이미지 모두 제거
        ClearTileImages();

        // 타일 데이터에서 최소/최대 x, y 좌표 찾기
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        // 유닛 빌드 데이터에서 좌표 범위 찾기
        foreach (var buildData in tileCardData.f_unitBuildData)
        {
            Vector2 tilePos = buildData.f_TilePosData.f_TilePos;
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
        float tileSize = 50f;
        float spacing = 1.5f; // 0.1 * tileSize
        layoutRect.sizeDelta = new Vector2(width * (tileSize + spacing) - spacing, height * (tileSize + spacing) - spacing);

        // 유닛 데이터를 그리드 위치에 매핑
        Dictionary<Vector2Int, D_unitBuildData> unitPositionMap = new Dictionary<Vector2Int, D_unitBuildData>();

        foreach (var buildData in tileCardData.f_unitBuildData)
        {
            Vector2Int normalizedPos = new Vector2Int(
                (int)buildData.f_TilePosData.f_TilePos.x - minX,
                (int)buildData.f_TilePosData.f_TilePos.y - minY
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
                    Image tileImage = tileObject.backgroundImage;

                    if (tileImage != null)
                    {
                        tileImages.Add(tileImage);
                        activeImageIndices.Add(tileImages.Count - 1);
                    }

                    // 해당 위치에 유닛 데이터가 있는지 확인
                    if (unitPositionMap.TryGetValue(gridPos, out var buildData))
                    {
                        // 유닛이 null이거나 SkillAttackType이 None이면 Base로 처리
                        bool isBase = (buildData.f_unitData == null || buildData.f_unitData.f_SkillAttackType == SkillAttackType.None);

                        if (!isBase)
                        {
                            // 유닛 스프라이트 가져오기
                            GameObject tempUnit = PoolingManager.Instance.GetObject(buildData.f_unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey);
                            UnitController unitController = tempUnit.GetComponent<UnitController>();

                            //if (unitController != null && unitController.unitSprite != null)
                            //{
                            //    Sprite unitSprite = unitController.unitSprite.sprite;
                            //    tileObject.InitUnitImage(unitSprite, false);
                            //}

                            PoolingManager.Instance.ReturnObject(tempUnit);
                        }
                        else
                        {
                            // Base로 표시
                            tileObject.InitUnitImage(null, true);
                        }
                    }
                    else
                    {
                        // 빈 타일 초기화
                        tileObject.InitUnitImage(null, false);
                    }
                }
            }
        }
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
            UnitInstanceViewOut();
            isDragging = false;

            GameManager.Instance.CanclePrepareUseCost();
        }
    }

    private void CleanUp()
    {
        ClearTileImages(); // 타일 이미지 제거
        CancelPlacement();

        originalPreviews.Clear();
        currentPreviews.Clear();
        activeImageIndices.Clear();
        extensionPreviews.Clear();

        // 이벤트 구독 해제
        GameManager.Instance.OnCostAdd -= OnCostChanged;
        StageManager.Instance.OnWaveFinish -= CancelDragging;

    }


}