using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;


public class HuntingOptionObject : MonoBehaviour
{

    public Image cardBackImage;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private Image enemyImage;
    private bool isSelectable;

    private AsyncOperationHandle<Sprite> imageLoadHandle;

    private D_HuntingOptionData optionData;
    private Action<D_HuntingOptionData> onOptionClicked;
    public Action onCardRevealed; // 카드가 뒤집혔을 때 호출될 이벤트


    public void Initialize(D_HuntingOptionData data, System.Action<D_HuntingOptionData> callback)
    {
        optionData = data;
        onOptionClicked = callback;
        enemyNameText.text = data.f_spawnEnemy.Name;

        // TODO: 프리뷰 이미지 로드 -> RT 렌더러로 가지고오기
        //LoadEnemyPreviewImage();

        UpdateUI();
    }

    private void LoadEnemyPreviewImage()
    {
        // 이전에 로드한 이미지가 있으면 해제
        if (imageLoadHandle.IsValid())
        {
            Addressables.Release(imageLoadHandle);
        }

        // 프리뷰 이미지 주소 키가 유효한지 확인
        if (!string.IsNullOrEmpty(optionData.f_spawnEnemy.f_ObjectPoolKey.f_PoolObjectAddressableKey))
        {
            // 비동기로 이미지 로드
            imageLoadHandle = Addressables.LoadAssetAsync<Sprite>(optionData.f_spawnEnemy.f_ObjectPoolKey.f_PoolObjectAddressableKey);
            imageLoadHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    enemyImage.sprite = handle.Result;
                }
                else
                {
                    Debug.LogWarning($"프리뷰 이미지 로드 실패: {optionData.f_spawnEnemy.f_ObjectPoolKey.f_PoolObjectAddressableKey}");
                }
            };
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
}
