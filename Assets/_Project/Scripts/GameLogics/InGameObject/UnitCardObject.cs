using BGDatabaseEnum;
using CatDarkGame.PerObjectRTRenderForUGUI;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ���� Ÿ�� UI ��Ҹ� �����ϰ� �巡�� �� ������� �ʿ� ������ ��ġ�ϴ� Ŭ����
public class UnitCardObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private GameObject tileImageLayout; // TileImage_Layout ���ӿ�����Ʈ
    [SerializeField] private GameObject tileImagePrefab; // Tile_Image ������
    [SerializeField] private PerObjectRTRenderer unitRTObject;
    [SerializeField] private GameObject unitTraitObject;
    [SerializeField] private GameObject costImageObject;
    [SerializeField] private List<Image> tileImages = new List<Image>();
    [SerializeField] private TMP_Text cardCostText;
    
    // ������ ����
    private UnitController previewUnit;
    
    // ���� ����
    private D_UnitData unitData;
    
    // ī�� ����
    private string tileCardName;
    private int cardCost;
    
    // �巡�� ���� ����
    private bool isDragging = false;
    private bool hasDragged = false;
    private bool canPlace = false;
    private Vector2 previousTilePosition;
    private PerObjectRTSource rtSource;
    
    // Ȱ��ȭ�� �̹��� �ε���
    private List<int> activeImageIndices = new List<int>();
    
    // Ÿ�� ���� (�ռ� ��)
    private UnitController targetUnit = null;
    private bool isShowingMergePreview = false;
    
    // ī�尡 ���Ǿ��� �� �߻��ϴ� �̺�Ʈ
    public static event System.Action<GameObject> OnCardUsed;

    // ī�� �ʱ�ȭ: Ÿ�� ����, �ڽ�Ʈ, ������ ����
    public void InitializeCardInform(D_TileCardData tileCardData)
    {
        CleanUp();

        tileCardName = tileCardData.f_name;
        cardCost = tileCardData.f_Cost;
        cardCostText.text = cardCost.ToString();
        
        // ù ��° ���� ������ ����
        unitData = tileCardData.f_unitBuildData[0].f_unitData;

        CreateTilePreview(tileCardData);
        UpdateCostTextColor();

        // RT ������ ���� ���� (ȭ�� �ۿ� ��ġ)
        CreateRTPreviewUnit();
         
        // �̺�Ʈ ����
        GameManager.Instance.OnCostAdd += OnCostChanged;
        StageManager.Instance.OnWaveFinish += CancelDragging;
    }
    
    // RT �������� ������ ���� ����
    private void CreateRTPreviewUnit()
    {
        // ȭ�� �� ��ġ�� ������ ���� ����
        Vector3 offscreenPos = new Vector3(3000f, 0f, 0f);
        GameObject previewInstance = PoolingManager.Instance.GetObject(
            unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey,
            offscreenPos,
            (int)ObjectLayer.Player
        );
        
        // ���� ��Ʈ�ѷ� ����
        previewUnit = previewInstance.GetComponent<UnitController>();
        if (previewUnit != null)
        {
            previewUnit.Initialize();
            previewUnit.InitializeUnitInfo(unitData);
            
            // RT �ҽ� ����
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

    // �ڽ�Ʈ ���� �� ȣ��� �޼���
    private void OnCostChanged()
    {
        UpdateCostTextColor();
    }

    // �ڽ�Ʈ ���� ������Ʈ
    public void UpdateCostTextColor()
    {
        cardCostText.color = GameManager.Instance.GetSystemStat(StatName.Cost) >= cardCost ? Color.white : Color.red;
    }

    // �巡�� ����
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

        // ���콺 Ŀ���� ���� ��ǥ ���
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        worldPos.y += 0.2f; // ������ ����
        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);
        
        // ������ ���� ��ġ ������Ʈ
        UpdatePreviewPosition(tilePos);
        previousTilePosition = tilePos;
    }

    // �巡�� ��
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || previewUnit == null) return;

        hasDragged = true;
        
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        worldPos.y += 0.1f; // ������ ����
        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        if (tilePos != previousTilePosition)
        {
            // ���� Ÿ�� ���� �ʱ�ȭ
            ResetTileColor(previousTilePosition);
            
            // ���� �ռ� ������ �ʱ�ȭ
            ResetMergePreview();
            
            // ��ġ ���� ���� Ȯ��
            canPlace = TileMapManager.Instance.CanPlaceObject(tilePos, previewUnit);
            
            // ������ ��ġ ������Ʈ
            UpdatePreviewPosition(tilePos);
            
            previousTilePosition = tilePos;
        }
    }

    // �巡�� ����
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
            // �ռ� ������ �ʱ�ȭ
            ResetMergePreview();
            SetUIVisibility(true);
            HidePreviewUnit();
        }

        ResetTileColor(previousTilePosition);
        GameManager.Instance.CanclePrepareUseCost();
    }

    // �ռ� ������ �ʱ�ȭ
    private void ResetMergePreview()
    {
        if (isShowingMergePreview && targetUnit != null)
        {
            // Ÿ�� ������ �� ǥ�� ����
            foreach (var star in targetUnit.starObjects)
            {
                star.SetActive(true);
            }
            
            // ������ ������ �� ǥ�� ����
            if (previewUnit != null)
            {
                previewUnit.UpdateStarDisplay();
            }
            
            isShowingMergePreview = false;
            targetUnit = null;
        }
    }

    // ���� Ÿ�� ���� �ʱ�ȭ
    private void ResetTileColor(Vector2 position)
    {
        TileMapManager.Instance.SetTileColor(position, Color.white);
    }

    // ������ ��ġ ������Ʈ
    private void UpdatePreviewPosition(Vector2 tilePos)
    {
        if (previewUnit == null) return;
        
        TileData tileData = TileMapManager.Instance.GetTileData(tilePos);
        Vector3 newPosition = TileMapManager.Instance.GetTileToWorldPosition(tilePos);
        newPosition.z = -0.1f;

        // �ռ� ���� ���� Ȯ��
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
                // �ռ� ������ ǥ��
                ShowMergePreview(placedUnit, newPosition);
                return;
            }
        }
        
        // �Ϲ� ��ġ
        previewUnit.transform.position = newPosition;
    }

    // �ռ� ������ ǥ��
    private void ShowMergePreview(UnitController placedUnit, Vector3 position)
    {
        targetUnit = placedUnit;
        isShowingMergePreview = true;
        
        // Ÿ�� ������ �� ��Ȱ��ȭ
        foreach (var star in targetUnit.starObjects)
        {
            star.SetActive(false);
        }
        
        // ������ ���� ���� ǥ�� ���׷��̵�
        int currentLevel = (int)previewUnit.GetStat(StatName.UnitStarLevel);
        int newLevel = currentLevel + 1;
        previewUnit.UpdateStarDisplay(newLevel);
        //previewUnit.ApplyEffect(0.3f);
        // ��ġ �� ��Ƽ���� ����
        previewUnit.transform.position = position;
    }

    // ���� ��ġ
    private void PlaceUnit()
    {
        // �ռ� ó��
        if (isShowingMergePreview && targetUnit != null)
        {
            PerformMerge();
            return;
        }
        
        // ���ο� ���� ���� �� ��ġ
        Vector3 unitPos = TileMapManager.Instance.GetTileToWorldPosition(previousTilePosition);
        GameObject unitInstance = PoolingManager.Instance.GetObject(
            unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey,
            unitPos,
            (int)ObjectLayer.Player
        );
        
        UnitController newUnit = unitInstance.GetComponent<UnitController>();
        if (newUnit != null)
        {
            // ���� �ʱ�ȭ
            newUnit.Initialize();
            newUnit.InitializeUnitInfo(unitData);
            newUnit.tilePosition = previousTilePosition;
            
            // ���� �Ŵ����� ���
            UnitManager.Instance.RegisterUnit(newUnit);
            
            // Ÿ�� ���� ó��
            TileData tileData = new TileData(previousTilePosition)
            {
                isAvailable = false,
                placedUnit = newUnit
            };
            TileMapManager.Instance.SetTileData(tileData);
            
            // �ڽ�Ʈ ���
            UseCardCost();
            
            PoolingManager.Instance.ReturnObject(rtSource.gameObject);
            
            // ī�� ����
            OnCardUsed?.Invoke(transform.parent.gameObject);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("Failed to create unit: UnitController component not found");
        }
    }

    // �ռ� ����
    private void PerformMerge()
    {
        if (targetUnit == null) return;
        
        // Ÿ�� ���� ���׷��̵�
        int newLevel = (int)targetUnit.GetStat(StatName.UnitStarLevel) + 1;
        targetUnit.UpGradeUnitLevel(newLevel);
        
        // �ڽ�Ʈ ���
        UseCardCost();
        
        PoolingManager.Instance.ReturnObject(rtSource.gameObject);

        // ī�� ����
        OnCardUsed?.Invoke(transform.parent.gameObject);
        Destroy(gameObject);
    }

    // �ڽ�Ʈ ���
    private void UseCardCost()
    {
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = cardCost * -1,
            multiply = 1f
        });
    }

    // ������ ���� �����
    private void HidePreviewUnit()
    {
        if (previewUnit != null)
        {
            // ȭ�� ������ �̵�
            previewUnit.transform.position = new Vector3(3000f, 0f, 0f);
            
            // �� ǥ�� ������� ����
            previewUnit.UpdateStarDisplay();
        }
    }

    // �巡�� ���
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

    // ī�� UI�� Ÿ�� ������ ����
    private void CreateTilePreview(D_TileCardData tileCardData)
    {
        // ���� Ÿ�� �̹��� ��� ����
        ClearTileImages();
        
        // ���� Ÿ�ϸ� ǥ��
        GameObject tileImageObj = Instantiate(tileImagePrefab, tileImageLayout.transform);
        
        // UnitTileObject ������Ʈ ��������
        UnitTileObject tileObject = tileImageObj.GetComponent<UnitTileObject>();
        
        if (tileObject != null)
        {
            // �߾ӿ� ��ġ
            tileObject.SetPosition(0, 0);
            
            // �̹��� ����Ʈ�� Ÿ�� �̹��� ������Ʈ �߰�
            Image tileImage = tileObject.backgroundImage;
            if (tileImage != null)
            {
                tileImages.Add(tileImage);
                activeImageIndices.Add(tileImages.Count - 1);
            }
            // ������ �ʱ�ȭ
            tileObject.InitTileImage(true);
        }
    }

    // ���� Ÿ�� �̹��� ��� ����
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

    // ����
    public void CleanUp()
    {
        ClearTileImages();
        
        if (previewUnit != null)
        {
            PoolingManager.Instance.ReturnObject(previewUnit.gameObject);
            previewUnit = null;
        }
        
        activeImageIndices.Clear();
        
        // �̺�Ʈ ���� ����
        GameManager.Instance.OnCostAdd -= OnCostChanged;
        StageManager.Instance.OnWaveFinish -= CancelDragging;
    }
}