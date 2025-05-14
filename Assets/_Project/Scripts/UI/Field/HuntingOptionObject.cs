using System;
using System.Collections;
using System.Collections.Generic;
using CatDarkGame.PerObjectRTRenderForUGUI;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;


public class HuntingOptionObject : MonoBehaviour
{

    public Image cardBackImage;

    [SerializeField] private PerObjectRTRenderer unitRTObject;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text enemyNameText;

    private bool isSelectable;
    private GameObject enemyObj;

    private D_HuntingOptionData optionData;
    private Action<D_HuntingOptionData> onOptionClicked;


    public Action onCardRevealed; // 카드가 뒤집혔을 때 호출될 이벤트

    public void Initialize(D_HuntingOptionData data, System.Action<D_HuntingOptionData> callback)
    {
        optionData = data;
        onOptionClicked = callback;
        enemyNameText.text = data.f_f_bossEnemy.Name;

        // TODO: 프리뷰 이미지 로드 -> RT 렌더러로 가지고오기
        LoadEnemyPreviewImage();

        UpdateUI();
    }

    private void LoadEnemyPreviewImage()
    {
        enemyObj = PoolingManager.Instance.GetObject(optionData.f_f_bossEnemy.f_ObjectPoolKey.f_PoolObjectAddressableKey);

        // 유닛 렌더링 설정
        if (unitRTObject != null)
        {
            // PerObjectRTSource 컴포넌트를 가진 게임 오브젝트 생성
            PerObjectRTSource rtSource = enemyObj.GetComponent<PerObjectRTSource>();

            // 필요한 경우 자식 오브젝트도 복사
            // 여기서는 간단히 소스만 설정
            unitRTObject.source = rtSource;
        }
    }

    private void UpdateUI()
    {
        titleText.text = optionData.f_title;
        descriptionText.text = optionData.f_description;
    }

    // 카드를 선택 가능 상태로 설정
    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
    }

    // 카드 뒷면이 비활성화되었을 때 호출될 메소드
    public void NotifyCardRevealed()
    {
        onCardRevealed?.Invoke();
    }

    public void OnClickSelectOption()
    {
        if (isSelectable)
        {
            // 클릭 이벤트 콜백 실행
            onOptionClicked?.Invoke(optionData);
        }
    }

    public void DestroyRTObject()
    {
        PoolingManager.Instance.ReturnObject(enemyObj.gameObject);

    }
}
