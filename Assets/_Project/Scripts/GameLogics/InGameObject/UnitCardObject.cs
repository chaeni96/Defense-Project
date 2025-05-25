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
    [SerializeField] private PerObjectRTRenderer unitRTObject;
    [SerializeField] private GameObject unitTraitObject;
    [SerializeField] private GameObject costImageObject;
    [SerializeField] private List<Image> tileImages = new List<Image>();
    [SerializeField] private TMP_Text cardCostText;
    
    // 프리뷰 유닛
    private UnitController previewUnit;
    
    // 유닛 정보
    private D_UnitData unitData;
    
    // 카드 정보
    private string tileCardName;
    private int cardCost;
    
    // 드래그 관련 변수
    private bool isDragging = false;
    private bool hasDragged = false;
    private bool canPlace = false;
    private Vector2 previousTilePosition;
    private PerObjectRTSource rtSource;
    
    // 활성화된 이미지 인덱스
    private List<int> activeImageIndices = new List<int>();
    
    // 타겟 유닛 (합성 시)
    private UnitController targetUnit = null;
    private bool isShowingMergePreview = false;
    
    // 카드가 사용되었을 때 발생하는 이벤트
    public static event System.Action<GameObject> OnCardUsed;

    // 카드 초기화: 타일 정보, 코스트, 프리뷰 설정
    public void InitializeCardInform(D_TileCardData tileCardData)
    {
        CleanUp();

        tileCardName = tileCardData.f_name;
        cardCost = tileCardData.f_Cost;
        cardCostText.text = cardCost.ToString();
        
        // 첫 번째 유닛 데이터 저장
        unitData = tileCardData.f_unitBuildData[0].f_unitData;

        CreateTilePreview(tileCardData);
        UpdateCostTextColor();

        // RT 프리뷰 유닛 생성 (화면 밖에 위치)
        CreateRTPreviewUnit();
         
        // 이벤트 구독
        GameManager.Instance.OnCostAdd += OnCostChanged;
        StageManager.Instance.OnWaveFinish += CancelDragging;
    }
    
    // RT 렌더링용 프리뷰 유닛 생성
    private void CreateRTPreviewUnit()
    {
        // 화면 밖 위치에 프리뷰 유닛 생성
        Vector3 offscreenPos = new Vector3(3000f, 0f, 0f);
        GameObject previewInstance = PoolingManager.Instance.GetObject(
            unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey,
            offscreenPos,
            (int)ObjectLayer.Player
        );
        
        // 유닛 컨트롤러 설정
        previewUnit = previewInstance.GetComponent<UnitController>();
        if (previewUnit != null)
        {
            previewUnit.Initialize();
            previewUnit.InitializeUnitInfo(unitData);
            
            // RT 소스 설정
            rtSource = previewInstance.GetComponent<PerObjectRTSource>();
            if (rtSource == null)
            {
                rtSource = previewInstance.AddComponent<PerObjectRTSource>();
            }
            
            if (rtSource != null && unitRTObject != null)
            {
                unitRTObject.CalculateAutoRect();
                unitRTObject.source = rtSource;
                rtSource.CalculateAutoBounds();
            }
        }
    }

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

    // 코스트 변경 시 호출될 메서드
    private void OnCostChanged()
    {
        UpdateCostTextColor();
    }

    // 코스트 색상 업데이트
    public void UpdateCostTextColor()
    {
        cardCostText.color = GameManager.Instance.GetSystemStat(StatName.Cost) >= cardCost ? Color.white : Color.red;
    }

    // 드래그 시작
    public void OnPointerDown(PointerEventData eventData)
    {
        if (StageManager.Instance.IsBattleActive)
            return;

        if (GameManager.Instance.GetSystemStat(StatName.Cost) < cardCost) 
            return;

        isDragging = true;
        canPlace = false;
        hasDragged = false;
        isShowingMergePreview = false;
        targetUnit = null;

        GameManager.Instance.PrepareUseCost(cardCost);
        SetUIVisibility(false);

        // 마우스 커서의 월드 좌표 계산
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        worldPos.y += 0.2f; // 오프셋 조정
        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);
        
        // 프리뷰 유닛 위치 업데이트
        UpdatePreviewPosition(tilePos);
        previousTilePosition = tilePos;
    }

    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || previewUnit == null) return;

        hasDragged = true;
        
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        worldPos.y += 0.1f; // 오프셋 조정
        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        if (tilePos != previousTilePosition)
        {
            // 이전 타일 색상 초기화
            ResetTileColor(previousTilePosition);
            
            // 이전 합성 프리뷰 초기화
            ResetMergePreview();
            
            // 배치 가능 여부 확인
            canPlace = TileMapManager.Instance.CanPlaceObject(tilePos, previewUnit);
            
            // 프리뷰 위치 업데이트
            UpdatePreviewPosition(tilePos);
            
            previousTilePosition = tilePos;
        }
    }

    // 드래그 종료
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging || previewUnit == null) return;

        isDragging = false;

        if (hasDragged && canPlace && UnitManager.Instance.GetActiveUnitCount() < GameManager.Instance.GetSystemStat(StatName.UnitPlacementCount))
        {
            PlaceUnit();
        }
        else
        {
            // 합성 프리뷰 초기화
            ResetMergePreview();
            SetUIVisibility(true);
            HidePreviewUnit();
        }

        ResetTileColor(previousTilePosition);
        GameManager.Instance.CanclePrepareUseCost();
    }

    // 합성 프리뷰 초기화
    private void ResetMergePreview()
    {
        if (isShowingMergePreview && targetUnit != null)
        {
            // 타겟 유닛의 별 표시 복원
            foreach (var star in targetUnit.starObjects)
            {
                star.SetActive(true);
            }
            
            // 프리뷰 유닛의 별 표시 복원
            if (previewUnit != null)
            {
                previewUnit.UpdateStarDisplay();
            }
            
            isShowingMergePreview = false;
            targetUnit = null;
        }
    }

    // 이전 타일 색상 초기화
    private void ResetTileColor(Vector2 position)
    {
        TileMapManager.Instance.SetTileColor(position, Color.white);
    }

    // 프리뷰 위치 업데이트
    private void UpdatePreviewPosition(Vector2 tilePos)
    {
        if (previewUnit == null) return;
        
        TileData tileData = TileMapManager.Instance.GetTileData(tilePos);
        Vector3 newPosition = TileMapManager.Instance.GetTileToWorldPosition(tilePos);
        newPosition.z = -0.1f;

        // 합성 가능 여부 확인
        if (tileData?.placedUnit != null)
        {
            UnitController placedUnit = tileData.placedUnit;
            int previewLevel = (int)previewUnit.GetStat(StatName.UnitStarLevel);
            int placedLevel = (int)placedUnit.GetStat(StatName.UnitStarLevel);
            
            bool canMerge = (previewUnit.unitType == placedUnit.unitType) &&
                          (previewLevel == placedLevel) &&
                          (placedLevel < 5) &&
                          (!placedUnit.isMultiUnit);
                      
            if (canMerge)
            {
                // 합성 프리뷰 표시
                ShowMergePreview(placedUnit, newPosition);
                return;
            }
        }
        
        // 일반 배치
        previewUnit.transform.position = newPosition;
    }

    // 합성 프리뷰 표시
    private void ShowMergePreview(UnitController placedUnit, Vector3 position)
    {
        targetUnit = placedUnit;
        isShowingMergePreview = true;
        
        // 타겟 유닛의 별 비활성화
        foreach (var star in targetUnit.starObjects)
        {
            star.SetActive(false);
        }
        
        // 프리뷰 유닛 레벨 표시 업그레이드
        int currentLevel = (int)previewUnit.GetStat(StatName.UnitStarLevel);
        int newLevel = currentLevel + 1;
        previewUnit.UpdateStarDisplay(newLevel);
        //previewUnit.ApplyEffect(0.3f);
        // 위치 및 머티리얼 설정
        previewUnit.transform.position = position;
    }

    // 유닛 배치
    private void PlaceUnit()
    {
        // 합성 처리
        if (isShowingMergePreview && targetUnit != null)
        {
            PerformMerge();
            return;
        }
        
        // 새로운 유닛 생성 및 배치
        Vector3 unitPos = TileMapManager.Instance.GetTileToWorldPosition(previousTilePosition);
        GameObject unitInstance = PoolingManager.Instance.GetObject(
            unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey,
            unitPos,
            (int)ObjectLayer.Player
        );
        
        UnitController newUnit = unitInstance.GetComponent<UnitController>();
        if (newUnit != null)
        {
            // 유닛 초기화
            newUnit.Initialize();
            newUnit.InitializeUnitInfo(unitData);
            newUnit.tilePosition = previousTilePosition;
            
            // 유닛 매니저에 등록
            UnitManager.Instance.RegisterUnit(newUnit);
            
            // 타일 점유 처리
            TileData tileData = new TileData(previousTilePosition)
            {
                isAvailable = false,
                placedUnit = newUnit
            };
            TileMapManager.Instance.SetTileData(tileData);
            
            // 코스트 사용
            UseCardCost();
            
            PoolingManager.Instance.ReturnObject(rtSource.gameObject);
            
            // 카드 제거
            OnCardUsed?.Invoke(transform.parent.gameObject);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("Failed to create unit: UnitController component not found");
        }
    }

    // 합성 수행
    private void PerformMerge()
    {
        if (targetUnit == null) return;
        
        // 타겟 유닛 업그레이드
        int newLevel = (int)targetUnit.GetStat(StatName.UnitStarLevel) + 1;
        targetUnit.UpGradeUnitLevel(newLevel);
        
        // 코스트 사용
        UseCardCost();
        
        PoolingManager.Instance.ReturnObject(rtSource.gameObject);

        // 카드 제거
        OnCardUsed?.Invoke(transform.parent.gameObject);
        Destroy(gameObject);
    }

    // 코스트 사용
    private void UseCardCost()
    {
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = cardCost * -1,
            multiply = 1f
        });
    }

    // 프리뷰 유닛 숨기기
    private void HidePreviewUnit()
    {
        if (previewUnit != null)
        {
            // 화면 밖으로 이동
            previewUnit.transform.position = new Vector3(3000f, 0f, 0f);
            
            // 별 표시 원래대로 복원
            previewUnit.UpdateStarDisplay();
        }
    }

    // 드래깅 취소
    private void CancelDragging()
    {
        if (isDragging)
        {
            ResetMergePreview();
            SetUIVisibility(true);
            HidePreviewUnit();
            isDragging = false;
            
            GameManager.Instance.CanclePrepareUseCost();
        }
    }

    // 카드 UI에 타일 프리뷰 생성
    private void CreateTilePreview(D_TileCardData tileCardData)
    {
        // 기존 타일 이미지 모두 제거
        ClearTileImages();
        
        // 단일 타일만 표시
        GameObject tileImageObj = Instantiate(tileImagePrefab, tileImageLayout.transform);
        
        // UnitTileObject 컴포넌트 가져오기
        UnitTileObject tileObject = tileImageObj.GetComponent<UnitTileObject>();
        
        if (tileObject != null)
        {
            // 중앙에 배치
            tileObject.SetPosition(0, 0);
            
            // 이미지 리스트에 타일 이미지 컴포넌트 추가
            Image tileImage = tileObject.backgroundImage;
            if (tileImage != null)
            {
                tileImages.Add(tileImage);
                activeImageIndices.Add(tileImages.Count - 1);
            }
            // 프리뷰 초기화
            tileObject.InitTileImage(true);
        }
    }

    // 기존 타일 이미지 모두 제거
    private void ClearTileImages()
    {
        activeImageIndices.Clear();
        
        foreach (var image in tileImages)
        {
            if (image != null && image.gameObject != null)
            {
                Destroy(image.gameObject);
            }
        }
        
        tileImages.Clear();
    }

    // 정리
    public void CleanUp()
    {
        ClearTileImages();
        
        if (previewUnit != null)
        {
            PoolingManager.Instance.ReturnObject(previewUnit.gameObject);
            previewUnit = null;
        }
        
        activeImageIndices.Clear();
        
        // 이벤트 구독 해제
        GameManager.Instance.OnCostAdd -= OnCostChanged;
        StageManager.Instance.OnWaveFinish -= CancelDragging;
    }
}