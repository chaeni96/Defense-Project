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
    public Action onCardRevealed; // ī�尡 �������� �� ȣ��� �̺�Ʈ


    public void Initialize(D_HuntingOptionData data, System.Action<D_HuntingOptionData> callback)
    {
        optionData = data;
        onOptionClicked = callback;
        enemyNameText.text = data.f_spawnEnemy.Name;

        // TODO: ������ �̹��� �ε� -> RT �������� ���������
        //LoadEnemyPreviewImage();

        UpdateUI();
    }

    private void LoadEnemyPreviewImage()
    {
        // ������ �ε��� �̹����� ������ ����
        if (imageLoadHandle.IsValid())
        {
            Addressables.Release(imageLoadHandle);
        }

        // ������ �̹��� �ּ� Ű�� ��ȿ���� Ȯ��
        if (!string.IsNullOrEmpty(optionData.f_spawnEnemy.f_ObjectPoolKey.f_PoolObjectAddressableKey))
        {
            // �񵿱�� �̹��� �ε�
            imageLoadHandle = Addressables.LoadAssetAsync<Sprite>(optionData.f_spawnEnemy.f_ObjectPoolKey.f_PoolObjectAddressableKey);
            imageLoadHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    enemyImage.sprite = handle.Result;
                }
                else
                {
                    Debug.LogWarning($"������ �̹��� �ε� ����: {optionData.f_spawnEnemy.f_ObjectPoolKey.f_PoolObjectAddressableKey}");
                }
            };
        }
       
    }

    private void UpdateUI()
    {
        titleText.text = optionData.f_title;
        descriptionText.text = optionData.f_description;
    }

    // ī�带 ���� ���� ���·� ����
    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
    }

    // ī�� �޸��� ��Ȱ��ȭ�Ǿ��� �� ȣ��� �޼ҵ�
    public void NotifyCardRevealed()
    {
        onCardRevealed?.Invoke();
    }

    public void OnClickSelectOption()
    {
        if (isSelectable)
        {
            // Ŭ�� �̺�Ʈ �ݹ� ����
            onOptionClicked?.Invoke(optionData);
        }
    }
}
