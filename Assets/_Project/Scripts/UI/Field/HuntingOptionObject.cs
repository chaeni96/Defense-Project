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


    public Action onCardRevealed; // ī�尡 �������� �� ȣ��� �̺�Ʈ

    public void Initialize(D_HuntingOptionData data, System.Action<D_HuntingOptionData> callback)
    {
        optionData = data;
        onOptionClicked = callback;
        enemyNameText.text = data.f_f_bossEnemy.Name;

        // TODO: ������ �̹��� �ε� -> RT �������� ���������
        LoadEnemyPreviewImage();

        UpdateUI();
    }

    private void LoadEnemyPreviewImage()
    {
        enemyObj = PoolingManager.Instance.GetObject(optionData.f_f_bossEnemy.f_ObjectPoolKey.f_PoolObjectAddressableKey);

        // ���� ������ ����
        if (unitRTObject != null)
        {
            // PerObjectRTSource ������Ʈ�� ���� ���� ������Ʈ ����
            PerObjectRTSource rtSource = enemyObj.GetComponent<PerObjectRTSource>();

            // �ʿ��� ��� �ڽ� ������Ʈ�� ����
            // ���⼭�� ������ �ҽ��� ����
            unitRTObject.source = rtSource;
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

    public void DestroyRTObject()
    {
        PoolingManager.Instance.ReturnObject(enemyObj.gameObject);

    }
}
